using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Contracts.Association;
using AElf.Contracts.Configuration;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.Election;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.Profit;
using AElf.Contracts.Referendum;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.TokenHolder;
using AElf.Contracts.Treasury;
using AElf.Contracts.Vote;
using AElf.CSharp.Core.Utils;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using log4net;

namespace AElfChain.Common.Managers
{
    public class ContractManager
    {
        private Dictionary<string, string> _systemContracts;
        private AuthorityManager _authorityManager;
        
        public ILog Logger = Log4NetHelper.GetLogger();
        public int ChainId { get; set; }
        public string ChainIdName { get; set; }
        public INodeManager NodeManager { get; set; }
        public string CallAddress { get; set; }
        public Address CallAccount { get; set; }
        public AuthorityManager Authority => GetAuthority();
        public Dictionary<string, string> SystemContracts => GetSystemContracts();

        public ContractManager(INodeManager nodeManager, string callAddress)
        {
            NodeManager = nodeManager;
            CallAddress = callAddress;
            CallAccount = callAddress.ConvertAddress();
            ChainIdName = NodeManager.GetChainId();
            ChainId = ChainHelper.ConvertBase58ToChainId(ChainIdName);
            Genesis = GenesisContract.GetGenesisContract(NodeManager, CallAddress);
            GenesisStub = Genesis.GetGensisStub(CallAddress);
        }

        public ContractManager(string endpoint, string callAddress)
        {
            NodeManager = new NodeManager(endpoint);
            CallAddress = callAddress;
            CallAccount = callAddress.ConvertAddress();
            ChainIdName = NodeManager.GetChainId();
            ChainId = ChainHelper.ConvertBase58ToChainId(ChainIdName);
            Genesis = GenesisContract.GetGenesisContract(NodeManager, CallAddress);
            GenesisStub = Genesis.GetGensisStub(CallAddress);
        }
        
        #region Contracts services and stub
        public GenesisContract Genesis { get; set; }
        public BasicContractZeroContainer.BasicContractZeroStub GenesisStub { get; set; }
        public TokenContract Token => Genesis.GetTokenContract();
        public TokenContractContainer.TokenContractStub TokenStub => Genesis.GetTokenStub();
        public TokenHolderContract TokenHolder => Genesis.GetTokenHolderContract();
        public TokenHolderContractContainer.TokenHolderContractStub TokenHolderStub => Genesis.GetTokenHolderStub();
        public TokenConverterContract TokenConverter => Genesis.GetTokenConverterContract();
        public TokenConverterContractContainer.TokenConverterContractStub TokenconverterStub =>
            Genesis.GetTokenConverterStub();
        public ConfigurationContract Configuration => Genesis.GetConfigurationContract();
        public ConfigurationContainer.ConfigurationStub ConfigurationStub => Genesis.GetConfigurationStub();
        public ConsensusContract Consensus => Genesis.GetConsensusContract();
        public AEDPoSContractContainer.AEDPoSContractStub ConsensusStub => Genesis.GetConsensusStub();
        public CrossChainContract CrossChain => Genesis.GetCrossChainContract();
        public CrossChainContractContainer.CrossChainContractStub CrossChainStub => Genesis.GetCrossChainStub();
        public ParliamentAuthContract ParliamentAuth => Genesis.GetParliamentAuthContract();
        public ParliamentContractContainer.ParliamentContractStub ParliamentAuthStub =>
            Genesis.GetParliamentAuthStub();
        public AssociationAuthContract Association => Genesis.GetAssociationAuthContract();
        public AssociationContractContainer.AssociationContractStub AssociationStub => Genesis.GetAssociationAuthStub();
        public ReferendumAuthContract Referendum => Genesis.GetReferendumAuthContract();
        public ReferendumContractContainer.ReferendumContractStub ReferendumStub =>
            Genesis.GetReferendumAuthStub();
        public ElectionContract Election => Genesis.GetElectionContract();
        public ElectionContractContainer.ElectionContractStub ElectionStub => Genesis.GetElectionStub();
        public VoteContract Vote => Genesis.GetVoteContract();
        public VoteContractContainer.VoteContractStub VoteStub => Genesis.GetVoteStub();
        public ProfitContract Profit => Genesis.GetProfitContract();
        public ProfitContractContainer.ProfitContractStub ProfitStub => Genesis.GetProfitStub();
        public TreasuryContract Treasury => Genesis.GetTreasuryContract();
        public TreasuryContractContainer.TreasuryContractStub TreasuryStub => Genesis.GetTreasuryStub();
        #endregion
        
        public async Task<long> CheckSideChainBlockIndex(long txHeight, int sideChainId)
        {
            var stopwatch = Stopwatch.StartNew();
            while (true)
            {
                var indexSideHeight = CrossChain.GetSideChainHeight(sideChainId);
                if (indexSideHeight < txHeight)
                {
                    System.Console.Write($"\r[Main->Side]Current index height: {indexSideHeight}, target index height: {txHeight}. Time using: {CommonHelper.ConvertMileSeconds(stopwatch.ElapsedMilliseconds)}"); 
                    await Task.Delay(2000);
                    continue;
                }
                System.Console.Write($"\r[Main->Side]Current index height: {indexSideHeight}, target index height: {txHeight}. Time using: {CommonHelper.ConvertMileSeconds(stopwatch.ElapsedMilliseconds)}"); 
                System.Console.WriteLine();
                stopwatch.Stop();
                var mainHeight = await NodeManager.ApiClient.GetBlockHeightAsync();
                return mainHeight;
            }
        }
        
        public async Task CheckParentChainBlockIndex(long blockHeight)
        {
            var stopwatch = Stopwatch.StartNew();
            while (true)
            {
                var indexHeight = CrossChain.GetParentChainHeight();
                if (blockHeight > indexHeight)
                {
                    System.Console.Write($"\r[Side->Main]Current index height: {indexHeight}, target index height: {blockHeight}. Time using: {CommonHelper.ConvertMileSeconds(stopwatch.ElapsedMilliseconds)}");
                    await Task.Delay(2000);
                    continue;
                }
                System.Console.Write($"\r[Side->Main]Current index height: {indexHeight}, target index height: {blockHeight}. Time using: {CommonHelper.ConvertMileSeconds(stopwatch.ElapsedMilliseconds)}");
                System.Console.WriteLine();
                stopwatch.Stop();
                break;
            }
        }
        
        public async Task<MerklePath> GetMerklePath(long blockNumber, string txId)
        {
            var index = 0;
            var blockInfoResult =
                await NodeManager.ApiClient.GetBlockByHeightAsync(blockNumber, true);
            var transactionIds = blockInfoResult.Body.Transactions;
            var transactionStatus = new List<string>();

            foreach (var transactionId in transactionIds)
            {
                var txResult = await NodeManager.ApiClient.GetTransactionResultAsync(transactionId);
                var resultStatus = txResult.Status.ConvertTransactionResultStatus();
                transactionStatus.Add(resultStatus.ToString());
            }

            var txIdsWithStatus = new List<Hash>();
            for (var num = 0; num < transactionIds.Count; num++)
            {
                var transactionId = HashHelper.HexStringToHash(transactionIds[num]);
                var txRes = transactionStatus[num];
                var rawBytes = transactionId.ToByteArray().Concat(EncodingHelper.GetBytesFromUtf8String(txRes))
                    .ToArray();
                var txIdWithStatus = Hash.FromRawBytes(rawBytes);
                txIdsWithStatus.Add(txIdWithStatus);
                if (!transactionIds[num].Equals(txId)) continue;
                index = num;
            }

            var bmt = BinaryMerkleTree.FromLeafNodes(txIdsWithStatus);
            var merklePath = new MerklePath();
            merklePath.MerklePathNodes.AddRange(bmt.GenerateMerklePath(index).MerklePathNodes);
            
            return merklePath;
        }
        
        public string GetContractAddress(string name)
        {
            if (SystemContracts.ContainsKey(name))
                return SystemContracts[name];

            return null;
        }
        
        private AuthorityManager GetAuthority()
        {
            if (_authorityManager == null)
                _authorityManager = new AuthorityManager(NodeManager, Genesis.CallAddress);

            return _authorityManager;
        }
        private Dictionary<string, string> GetSystemContracts()
        {
            if (_systemContracts == null)
            {
                var contracts = Genesis.GetAllSystemContracts();
                _systemContracts = new Dictionary<string, string>();
                foreach (var key in contracts.Keys)
                {
                    if (contracts[key].Equals(new Address())) continue;
                    _systemContracts.Add(key.ToString(), contracts[key].GetFormatted());
                }
            }

            return _systemContracts;
        }
    }
}