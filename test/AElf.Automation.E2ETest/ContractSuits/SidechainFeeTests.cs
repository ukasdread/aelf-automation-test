using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs3;
using Acs7;
using AElf.Contracts.Association;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.E2ETest.ContractSuits
{
    [TestClass]
    public class SidechainFeeTests
    {
        public INodeManager MainNode { get; set; }
        public ContractManager MainManager { get; set; }
        public INodeManager SideNode { get; set; }
        public ContractManager SideManager { get; set; }
        public ILog Logger { get; set; }
        
        public SidechainFeeTests()
        {
            Log4NetHelper.LogInit("SideChainTest");
            Logger = Log4NetHelper.GetLogger();
            
            NodeInfoHelper.SetConfig(ContractTestBase.MainConfig);
            var mainNode = NodeInfoHelper.Config.Nodes.First();
            MainNode = new NodeManager(mainNode.Endpoint);
            MainManager = new ContractManager(MainNode, mainNode.Account);
            
            NodeInfoHelper.SetConfig(ContractTestBase.SideConfig);
            var sideNode = NodeInfoHelper.Config.Nodes.First();
            SideNode = new NodeManager(sideNode.Endpoint);
            SideManager = new ContractManager(SideNode, sideNode.Account);    
        }

        [TestMethod]
        public async Task AdoptSideChain_IndexFee_Test()
        {
            var chainId = ChainHelper.ConvertBase58ToChainId("tDVV");
            var proposer = NodeInfoHelper.Config.Nodes.First().Account;
            var association = MainManager.CrossChain.GetSideChainIndexingFeeController(chainId).AuthorityInfo
                .OwnerAddress;
            var adjustIndexingFeeInput = new AdjustIndexingFeeInput
            {
                IndexingFee = 10,
                SideChainId = chainId
            };
            var proposalId = MainManager.Association.CreateProposal(
                MainManager.CrossChain.ContractAddress,
                nameof(CrossChainContractMethod.AdjustIndexingFeePrice), adjustIndexingFeeInput, association,
                proposer);
            MainManager.Association.SetAccount(proposer);
            MainManager.Association.ApproveProposal(proposalId, proposer);
            var defaultOrganization =
                (await MainManager.CrossChainStub.GetSideChainLifetimeController.CallAsync(new Empty()))
                .OwnerAddress;
            var approveProposalId = MainManager.ParliamentAuth.CreateProposal(
                MainManager.Association.ContractAddress, nameof(AssociationMethod.Approve), proposalId,
                defaultOrganization, proposer);
            var currentMiners = MainManager.Authority.GetCurrentMiners();
            foreach (var miner in currentMiners)
            {
                MainManager.ParliamentAuth.ApproveProposal(approveProposalId, miner);
            }

            MainManager.ParliamentAuth.ReleaseProposal(approveProposalId, proposer);
            MainManager.Association.ReleaseProposal(proposalId, proposer);

            var afterCheckPrice = MainManager.CrossChain.GetSideChainIndexingFeePrice(chainId);
            afterCheckPrice.ShouldBe(10);
        }

        [TestMethod]
        public async Task AdoptSideChain_RentalFee_Test()
        {
            var proposer = SideManager.CallAddress;
            var defaultOrganization = SideManager.ParliamentAuth.GetGenesisOwnerAddress();
            var association = await CreateAssociationOrganization(defaultOrganization, SideManager.CallAccount);
            var updateRentalInput = new UpdateRentalInput
            {
                Rental =
                {
                    {"CPU", 1000},
                    {"RAM", 500},
                    {"DISK", 4},
                    {"NET", 2}
                }
            };
            var proposalId = SideManager.Association.CreateProposal(
                SideManager.Token.ContractAddress,
                nameof(TokenMethod.UpdateRental), updateRentalInput, association,
                proposer);
            SideManager.Association.SetAccount(proposer);
            SideManager.Association.ApproveProposal(proposalId, proposer);
            var approveProposalId = SideManager.ParliamentAuth.CreateProposal(
                SideManager.Association.ContractAddress, nameof(AssociationMethod.Approve), proposalId,
                defaultOrganization, proposer);
            var currentMiners = SideManager.Authority.GetCurrentMiners();
            foreach (var miner in currentMiners)
            {
                SideManager.ParliamentAuth.ApproveProposal(approveProposalId, miner);
            }

            var parliamentResult = SideManager.ParliamentAuth.ReleaseProposal(approveProposalId, proposer);
            parliamentResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var associationResult = SideManager.Association.ReleaseProposal(proposalId, proposer);
            associationResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }
        
        private async Task<Address> CreateAssociationOrganization(Address parliamentOrgAddress, Address sideCreator)
        {
            var minimalApproveThreshold = 2;
            var minimalVoteThreshold = 2;
            var maximalAbstentionThreshold = 0;
            var maximalRejectionThreshold = 0;
            var list = new List<Address> {parliamentOrgAddress,sideCreator};
            var createOrganizationInput = new CreateOrganizationInput
            {
                OrganizationMemberList = new OrganizationMemberList
                {
                    OrganizationMembers = {list}
                },
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MinimalApprovalThreshold = minimalApproveThreshold,
                    MinimalVoteThreshold = minimalVoteThreshold,
                    MaximalAbstentionThreshold = maximalAbstentionThreshold,
                    MaximalRejectionThreshold = maximalRejectionThreshold
                },
                ProposerWhiteList = new ProposerWhiteList
                {
                    Proposers = {list}
                }
            };
            var transactionResult =
                await SideManager.AssociationStub.CreateOrganization.SendAsync(createOrganizationInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var organization = transactionResult.Output;
            Logger.Info($"Organization address: {organization.GetFormatted()}");

            return organization;
        }
    }
}