using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Acs1;
using Acs3;
using Acs7;
using AElf.Client.Dto;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Automation.SideChainTests
{
    [TestClass]
    public class SideChainVerify : SideChainTestBase
    {
        [TestInitialize]
        public void InitializeNodeTests()
        {
            Initialize();
        }

        [TestMethod]
        public void GetResourceTokenOnSideChain()
        {
            var symbols = new[] {"CPU", "NET", "DISK", "RAM", "READ", "WRITE", "STORAGE", "TRAFFIC"};
            foreach (var sideService in SideServices)
            foreach (var symbol in symbols)
            {
                var infoOnMain = MainServices.TokenService.GetTokenInfo(symbol);
                var info = sideService.TokenService.GetTokenInfo(symbol);
                Logger.Info($"{symbol}:");
                info.Symbol.ShouldBe(infoOnMain.Symbol);
                info.Burned.ShouldBe(infoOnMain.Burned);
                info.Decimals.ShouldBe(infoOnMain.Decimals);
                info.IssueChainId.ShouldBe(infoOnMain.IssueChainId);
                info.Issuer.ShouldBe(infoOnMain.Issuer);
//                    info.Supply.ShouldBe(infoOnMain.Supply);
                info.TotalSupply.ShouldBe(infoOnMain.TotalSupply);
                info.IsBurnable.ShouldBe(infoOnMain.IsBurnable);
                info.IsProfitable.ShouldBe(infoOnMain.IsProfitable);
            }
        }

        [TestMethod]
        public void GetSideChainBalance()
        {
            var balance1 =
                MainServices.CrossChainService.GetSideChainBalance(ChainHelper.ConvertBase58ToChainId("tDVV"));
            var balance2 =
                MainServices.CrossChainService.GetSideChainBalance(ChainHelper.ConvertBase58ToChainId("tDVW"));
            Logger.Info($"chain tDVV {balance1}\n chain tDVW {balance2}");
        }

        [TestMethod]
        public void GetSideChainIndex()
        {
            var index1 =
                MainServices.CrossChainService.GetSideChainHeight(ChainHelper.ConvertBase58ToChainId("tDVV"));
            var index2 =
                MainServices.CrossChainService.GetSideChainHeight(ChainHelper.ConvertBase58ToChainId("tDVW"));
            Logger.Info($"chain tDVV {index1}\n chain tDVW {index2}");

            var index3 =
                SideAServices.CrossChainService.GetParentChainHeight();
            var index4 =
                SideBServices.CrossChainService.GetParentChainHeight();
            Logger.Info($"chain tDVV index main chain {index3}\n chain tDVW index main chain {index4}");
        }

        #region register
        
        [TestMethod]
        public async Task MainChainRegisterSideChain1()
        {
            var rawTx = ValidateTokenAddress(SideAServices);
            var txId = ExecuteMethodWithTxId(SideAServices, rawTx);
            var txResult = SideAServices.NodeManager.CheckTransactionResult(txId);

            if (txResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
                Assert.IsTrue(false,
                    $"Validate chain {SideAServices.ChainId} token contract failed");
            Logger.Info($"Validate Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId}");
            Logger.Info(
                $"Validate chain {SideAServices.ChainId} token address {SideAServices.TokenService.ContractAddress}");

            await MainChainCheckSideChainBlockIndex(SideAServices, txResult.BlockNumber);
            var merklePath = GetMerklePath(txResult.BlockNumber, txId, SideAServices, out var root);
            if (merklePath == null)
                Assert.IsTrue(false, "Can't get the merkle path.");
            var crossChainMerkleProofContext =
                GetBoundParentChainHeightAndMerklePathByHeight(SideAServices, InitAccount, txResult.BlockNumber);
            var registerInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = SideAServices.ChainId,
                ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight,
                TokenContractAddress =
                    AddressHelper.Base58StringToAddress(SideAServices.TokenService.ContractAddress),
                TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx)),
                MerklePath = merklePath
            };
            registerInput.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext.MerklePathFromParentChain
                .MerklePathNodes);
            Proposal(MainServices, registerInput);
            Logger.Info(
                $"Main chain register chain {SideAServices.ChainId} token address {SideAServices.TokenService.ContractAddress}");
        }

        [TestMethod]
        public void SideChain1RegisterMainChain()
        {
            var rawTx = ValidateTokenAddress(MainServices);
            var txId = ExecuteMethodWithTxId(MainServices, rawTx);
            var txResult = MainServices.NodeManager.CheckTransactionResult(txId);

            if (txResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
                Assert.IsTrue(false,
                    $"Validate chain {MainServices.ChainId} token contract failed");
            Logger.Info($"Validate Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId}");
            Logger.Info(
                $"Validate chain {MainServices.ChainId} token address {MainServices.TokenService.ContractAddress}");

            while (txResult.BlockNumber > GetIndexParentHeight(SideAServices))
            {
                Logger.Info("Block is not recorded ");
                Thread.Sleep(10000);
            }

            //register main chain token address
            var merklePath = GetMerklePath(txResult.BlockNumber, txId, MainServices, out var root);
            if (merklePath == null)
                Assert.IsTrue(false, "Can't get the merkle path.");
            var registerInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = MainServices.ChainId,
                ParentChainHeight = txResult.BlockNumber,
                TokenContractAddress =
                    AddressHelper.Base58StringToAddress(MainServices.TokenService.ContractAddress),
                TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx)),
                MerklePath = merklePath
            };

            Proposal(SideAServices, registerInput);
            Logger.Info(
                $"Chain {SideAServices.ChainId} register Main chain token address {MainServices.TokenService.ContractAddress}");
        }

        [TestMethod]
        public async Task ChangeCrossChainTokenContractRegistrationController()
        {
            var tokenImplStub = SideAServices.TokenImplContractStub;
            var controller =
                await tokenImplStub.GetCrossChainTokenContractRegistrationController.CallAsync(new Empty());
            var newController = SideAuthorityManager.CreateAssociationOrganization(Members);
            var changeInput = new AuthorityInfo
            {
                ContractAddress = SideAServices.AssociationService.Contract,
                OwnerAddress = newController
            };
            var authorityManager = new AuthorityManager(SideAServices.NodeManager);
            var changeProposal = authorityManager.ExecuteTransactionWithAuthority(
                SideAServices.TokenService.ContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub
                    .ChangeCrossChainTokenContractRegistrationController), changeInput, InitAccount,
                controller.OwnerAddress);
            changeProposal.Status.ShouldBe(TransactionResultStatus.Mined);
            var updateController =
                await tokenImplStub.GetCrossChainTokenContractRegistrationController.CallAsync(new Empty());
            updateController.ContractAddress.ShouldBe(SideAServices.AssociationService.Contract);
            updateController.OwnerAddress.ShouldBe(newController);

            //register 
            var rawTx = ValidateTokenAddress(MainServices);
            var txId = ExecuteMethodWithTxId(MainServices, rawTx);
            var txResult = MainServices.NodeManager.CheckTransactionResult(txId);

            if (txResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
                Assert.IsTrue(false,
                    $"Validate chain {MainServices.ChainId} token contract failed");
            Logger.Info($"Validate Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId}");
            Logger.Info(
                $"Validate chain {MainServices.ChainId} token address {MainServices.TokenService.ContractAddress}");

            while (txResult.BlockNumber > GetIndexParentHeight(SideAServices))
            {
                Logger.Info("Block is not recorded ");
                Thread.Sleep(10000);
            }
            var merklePath = GetMerklePath(txResult.BlockNumber, txId, MainServices, out var root);
            if (merklePath == null)
                Assert.IsTrue(false, "Can't get the merkle path.");
            var registerInput = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = MainServices.ChainId,
                ParentChainHeight = txResult.BlockNumber,
                TokenContractAddress =
                    AddressHelper.Base58StringToAddress(MainServices.TokenService.ContractAddress),
                TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx)),
                MerklePath = merklePath
            }; 

            //create proposal
            var proposer = SideAServices.AssociationService.GetOrganization(newController).ProposerWhiteList.Proposers
                .First();
            SideAServices.TokenService.TransferBalance(InitAccount, proposer.GetFormatted(), 5000_000000000);
            var createProposalResult =
                SideAServices.AssociationService.CreateProposal(SideAServices.TokenService.ContractAddress,
                    nameof(TokenMethod.RegisterCrossChainTokenContractAddress), registerInput, newController, proposer.GetFormatted());
            SideAServices.AssociationService.ApproveWithAssociation(createProposalResult,newController);
            var release = SideAServices.AssociationService.ReleaseProposal(createProposalResult, proposer.GetFormatted());
            release.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            Logger.Info(
                $"Main chain register chain {SideAServices.ChainId} token address {SideAServices.TokenService.ContractAddress}");
            
            //recover
            var recoverInput = new AuthorityInfo
            {
                ContractAddress = controller.ContractAddress,
                OwnerAddress = controller.OwnerAddress
            };
            var recoverProposalResult =
                SideAServices.AssociationService.CreateProposal(SideAServices.TokenService.ContractAddress,
                    nameof(TokenContractImplContainer.TokenContractImplStub
                        .ChangeCrossChainTokenContractRegistrationController), recoverInput, newController, proposer.GetFormatted());
            SideAServices.AssociationService.ApproveWithAssociation(recoverProposalResult,newController);
            var recoverRelease = SideAServices.AssociationService.ReleaseProposal(recoverProposalResult, proposer.GetFormatted());
            recoverRelease.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var recoverController = await tokenImplStub.GetCrossChainTokenContractRegistrationController.CallAsync(new Empty());
            recoverController.ContractAddress.ShouldBe(controller.ContractAddress);
            recoverController.OwnerAddress.ShouldBe(controller.OwnerAddress);
        }

        [TestMethod]
        public async Task SideChain1RegisterSideChain2()
        {
            var rawTx = ValidateTokenAddress(SideAServices);
            var txId = ExecuteMethodWithTxId(SideAServices, rawTx);
            var txResult = SideAServices.NodeManager.CheckTransactionResult(txId);

            if (txResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
                Assert.IsTrue(false,
                    $"Validate chain {SideAServices.ChainId} token contract failed");
            Logger.Info($"Validate Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId}");
            Logger.Info(
                $"Validate chain {SideAServices.ChainId} token address {SideAServices.TokenService.ContractAddress}");

            var mainHeight = await MainChainCheckSideChainBlockIndex(SideAServices, txResult.BlockNumber);
            var crossChainMerkleProofContextA =
                GetBoundParentChainHeightAndMerklePathByHeight(SideAServices, InitAccount, txResult.BlockNumber);
            var sideChainMerklePathA =
                GetMerklePath(txResult.BlockNumber, txId, SideAServices, out var root);
            if (sideChainMerklePathA == null)
                Assert.IsTrue(false, "Can't get the merkle path.");
            var sideChainRegisterInputA = new RegisterCrossChainTokenContractAddressInput
            {
                FromChainId = SideAServices.ChainId,
                ParentChainHeight = crossChainMerkleProofContextA.BoundParentChainHeight,
                TokenContractAddress =
                    AddressHelper.Base58StringToAddress(SideAServices.TokenService.ContractAddress),
                TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx)),
                MerklePath = sideChainMerklePathA
            };
            sideChainRegisterInputA.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContextA
                .MerklePathFromParentChain
                .MerklePathNodes);

            while (mainHeight > GetIndexParentHeight(SideBServices))
            {
                Logger.Info("Block is not recorded ");
                await Task.Delay(10000);
            }

            Proposal(SideBServices, sideChainRegisterInputA);
            Logger.Info(
                $"Chain {SideBServices.ChainId} register chain {SideAServices.ChainId} token address {SideAServices.TokenService.ContractAddress}");
        }

        #endregion

        #region cross transfer

        [TestMethod]
        public void MainChainCrossChainTransferSideChainResourceToken()
        {
            foreach (var service in SideServices) MainChainCrossChainTransferSideChain(service);
        }

        public void MainChainCrossChainTransferSideChain(ContractServices sideService)
        {
            var symbols = new[] {"CPU", "NET", "DISK", "RAM", "READ", "WRITE", "STORAGE", "TRAFFIC"};
            var txInfos = new Dictionary<TransactionResultDto, string>();
            foreach (var symbol in symbols)
            {
                var crossChainTransferInput = new CrossChainTransferInput
                {
                    Symbol = symbol,
                    IssueChainId = MainServices.ChainId,
                    Amount = 100_00000000,
                    Memo = "cross chain transfer",
                    To = AddressHelper.Base58StringToAddress(InitAccount),
                    ToChainId = sideService.ChainId
                };
                // execute cross chain transfer
                var rawTx = MainServices.NodeManager.GenerateRawTransaction(InitAccount,
                    MainServices.TokenService.ContractAddress, TokenMethod.CrossChainTransfer.ToString(),
                    crossChainTransferInput);
                Logger.Info($"Transaction rawTx is: {rawTx}");
                var txId = ExecuteMethodWithTxId(MainServices, rawTx);
                var txResult = MainServices.NodeManager.CheckTransactionResult(txId);
                // get transaction info            
                var status = txResult.Status.ConvertTransactionResultStatus();
                status.ShouldBe(TransactionResultStatus.Mined);
                Logger.Info(
                    $"Cross chain Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId} to chain {sideService.ChainId}");
                txInfos.Add(txResult, rawTx);
            }

            while (txInfos.Last().Key.BlockNumber > GetIndexParentHeight(sideService))
            {
                Logger.Info("Block is not recorded ");
                Thread.Sleep(10000);
            }

            foreach (var txInfo in txInfos)
            {
                var merklePath = GetMerklePath(txInfo.Key.BlockNumber, txInfo.Key.TransactionId, MainServices,
                    out var root);
                var crossChainReceiveToken = new CrossChainReceiveTokenInput
                {
                    FromChainId = MainServices.ChainId,
                    ParentChainHeight = txInfo.Key.BlockNumber,
                    MerklePath = merklePath
                };
                crossChainReceiveToken.TransferTransactionBytes =
                    ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(txInfo.Value));

                var result = CrossChainReceive(sideService, InitAccount, crossChainReceiveToken);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }

            foreach (var symbol in symbols)
            {
                //verify
                var balance = GetBalance(sideService, InitAccount, symbol);
                Logger.Info($"balance: {balance}");
            }
        }

        [TestMethod]
        public async Task AllSideChainTransferToMainChain()
        {
            foreach (var service in SideServices) await SideChainCrossChainTransferMainChain(service, 10000_00000000);
        }

        public async Task SideChainCrossChainTransferMainChain(ContractServices services, long amount)
        {
            var symbol = services.TokenService.GetPrimaryTokenSymbol();
            var crossChainTransferInput = new CrossChainTransferInput
            {
                Symbol = symbol,
                IssueChainId = services.ChainId,
                Amount = amount,
                Memo = "cross chain transfer",
                To = AddressHelper.Base58StringToAddress(InitAccount),
                ToChainId = MainServices.ChainId
            };

            var sideChainTokenContracts = services.TokenService.ContractAddress;
            Logger.Info($"{sideChainTokenContracts}");

            // execute cross chain transfer
            var rawTx = services.NodeManager.GenerateRawTransaction(InitAccount,
                sideChainTokenContracts, nameof(TokenMethod.CrossChainTransfer),
                crossChainTransferInput);
            Logger.Info($"Transaction rawTx is: {rawTx}");
            var txId = services.NodeManager.SendTransaction(rawTx);
            var txResult = services.NodeManager.CheckTransactionResult(txId);
            // get transaction info            
            var status = txResult.Status.ConvertTransactionResultStatus();
            status.ShouldBe(TransactionResultStatus.Mined);
            Logger.Info(
                $"Cross chain Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId} to chain {MainServices.ChainId}");

            await MainChainCheckSideChainBlockIndex(services, txResult.BlockNumber);
            var merklePath = GetMerklePath(txResult.BlockNumber, txId, services, out var root);
            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = services.ChainId,
                MerklePath = merklePath
            };
            // verify side chain transaction
            var crossChainMerkleProofContext = services.CrossChainService.GetCrossChainMerkleProofContext(txResult.BlockNumber);
            crossChainReceiveToken.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext
                .MerklePathFromParentChain.MerklePathNodes);
            crossChainReceiveToken.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
            crossChainReceiveToken.TransferTransactionBytes =
                ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx));

            var result = CrossChainReceive(MainServices, InitAccount, crossChainReceiveToken);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //verify
            var balance = GetBalance(MainServices, InitAccount, symbol);
            Logger.Info($"account {InitAccount}, {symbol} balance: {balance}");
        }

        [TestMethod]
        public async Task SideChainACrossChainTransferSideChainB()
        {
            //Transfer
            var crossChainTransferInput = new CrossChainTransferInput
            {
                Symbol = "STA",
                IssueChainId = SideAServices.ChainId,
                Amount = 1000,
                Memo = "cross chain transfer",
                To = AddressHelper.Base58StringToAddress(InitAccount),
                ToChainId = SideBServices.ChainId
            };
            // execute cross chain transfer
            var rawTx = SideAServices.NodeManager.GenerateRawTransaction(InitAccount,
                SideAServices.TokenService.ContractAddress,
                TokenMethod.CrossChainTransfer.ToString(),
                crossChainTransferInput);
            Logger.Info($"Transaction rawTx is: {rawTx}");
            var txId = ExecuteMethodWithTxId(SideAServices, rawTx);
            var txResult = SideAServices.NodeManager.CheckTransactionResult(txId);
            // get transaction info            
            var status = txResult.Status.ConvertTransactionResultStatus();
            status.ShouldBe(TransactionResultStatus.Mined);

            Logger.Info(
                $"Cross chain Transaction block: {txResult.BlockNumber}, rawTx: {rawTx}, txId:{txId} to chain {SideAServices.ChainId}");

            var mainHeight = await MainChainCheckSideChainBlockIndex(SideAServices, txResult.BlockNumber);
            var merklePath = GetMerklePath(txResult.BlockNumber, txId, SideAServices,
                out var root);

            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = SideAServices.ChainId,
                MerklePath = merklePath
            };
            var crossChainMerkleProofContext = SideAServices.CrossChainService.GetCrossChainMerkleProofContext(txResult.BlockNumber);
            crossChainReceiveToken.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext
                .MerklePathFromParentChain.MerklePathNodes);
            crossChainReceiveToken.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
            crossChainReceiveToken.TransferTransactionBytes =
                ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx));

            while (mainHeight > GetIndexParentHeight(SideBServices))
            {
                Logger.Info("Block is not recorded ");
                await Task.Delay(10000);
            }

            var result = CrossChainReceive(SideBServices, InitAccount, crossChainReceiveToken);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //verify
            var balance = GetBalance(SideBServices, InitAccount, "STA");
            Logger.Info($"balance: {balance}");
        }

        #endregion

        #region cross create token

        [TestMethod]
        public async Task ValidaAllToken()
        {
            foreach (var sideServices in SideServices)
            foreach (var validate in SideServices)
            {
                if (validate.ChainId.Equals(sideServices.ChainId)) continue;
                await ValidToken(sideServices, validate);
            }
        }

        public async Task ValidToken(ContractServices services, ContractServices validateServices)
        {
            var stub = services.TokenImplContractStub;
            var tokenInfo = await stub.GetTokenInfo.CallAsync(new GetTokenInfoInput
                {Symbol = services.TokenService.GetPrimaryTokenSymbol()});
            var validateStub = validateServices.TokenContractStub;
            var validateTokenInfo = await validateStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
                {Symbol = services.TokenService.GetPrimaryTokenSymbol()});
            if (!validateTokenInfo.Equals(new TokenInfo())) return;
            var result = await stub.ValidateTokenInfoExists.SendAsync(new ValidateTokenInfoExistsInput
            {
                Decimals = tokenInfo.Decimals,
                Issuer = tokenInfo.Issuer,
                IsBurnable = tokenInfo.IsBurnable,
                IssueChainId = tokenInfo.IssueChainId,
                IsProfitable = tokenInfo.IsProfitable,
                Symbol = tokenInfo.Symbol,
                TokenName = tokenInfo.TokenName,
                TotalSupply = tokenInfo.TotalSupply
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"{result.TransactionResult.BlockNumber},{result.TransactionResult.TransactionId}");

            var mainHeight = await MainChainCheckSideChainBlockIndex(services, result.TransactionResult.BlockNumber);

            while (mainHeight > GetIndexParentHeight(services))
            {
                Logger.Info("Block is not recorded ");
                await Task.Delay(10000);
            }

            var merklePath = GetMerklePath(result.TransactionResult.BlockNumber,
                result.TransactionResult.TransactionId.ToHex(), services, out var root);
            var crossChainCrossToken = new CrossChainCreateTokenInput
            {
                FromChainId = services.ChainId,
                MerklePath = merklePath,
                TransactionBytes = result.Transaction.ToByteString()
            };
            var crossChainMerkleProofContext =services.CrossChainService.
                GetCrossChainMerkleProofContext(result.TransactionResult.BlockNumber);
            crossChainCrossToken.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext
                .MerklePathFromParentChain.MerklePathNodes);
            crossChainCrossToken.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;

            var createResult =
                await validateStub.CrossChainCreateToken.SendAsync(crossChainCrossToken);
            createResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            validateTokenInfo = await validateStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
                {Symbol = services.TokenService.GetPrimaryTokenSymbol()});
            validateTokenInfo.TokenName.ShouldBe(tokenInfo.TokenName);
        }

        [TestMethod]
        public async Task ValidatePrimaryTokenSymbol()
        {
            var rawTx = await ValidateTokenSymbol(SideAServices, SideAServices.TokenService
                .GetPrimaryTokenSymbol());
            var txId = ExecuteMethodWithTxId(SideAServices, rawTx);
            var txResult = SideAServices.NodeManager.CheckTransactionResult(txId);
            if (txResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
                Assert.IsTrue(false,
                    $"Validate chain {SideAServices.ChainId} token symbol failed");
            var mainHeight = await MainChainCheckSideChainBlockIndex(SideAServices, txResult.BlockNumber);

            var merklePath = GetMerklePath(txResult.BlockNumber, txResult.TransactionId, SideAServices, out var root);
            if (merklePath == null)
                Assert.IsTrue(false, "Can't get the merkle path.");
            var createInput = new CrossChainCreateTokenInput
            {
                FromChainId = SideAServices.ChainId,
                MerklePath = merklePath,
                TransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx))
            };

            var crossChainMerkleProofContext = SideAServices.CrossChainService.GetCrossChainMerkleProofContext(txResult.BlockNumber);
            createInput.MerklePath.MerklePathNodes.AddRange(crossChainMerkleProofContext
                .MerklePathFromParentChain.MerklePathNodes);
            createInput.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;

            while (mainHeight > GetIndexParentHeight(SideBServices))
            {
                Logger.Info("Block is not recorded ");
                await Task.Delay(10000);
            }

            var createResult =
                SideBServices.TokenService.ExecuteMethodWithResult(TokenMethod.CrossChainCreateToken,
                    createInput);
            createResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var sideBTokenInfo = SideBServices.TokenService.CallViewMethod<TokenInfo>(TokenMethod.GetTokenInfo,
                new GetTokenInfoInput {Symbol = SideAServices.TokenService.GetPrimaryTokenSymbol()});
            sideBTokenInfo.Symbol.ShouldBe(SideAServices.TokenService.GetPrimaryTokenSymbol());
        }

        [TestMethod]
        public async Task ValidShareToken()
        {
            var stub = MainServices.TokenImplContractStub;
            var tokenInfo = await stub.GetTokenInfo.CallAsync(new GetTokenInfoInput
                {Symbol = "SHARE"});
            var validateService = new List<ContractServices>();
            foreach (var side in SideServices)
            {
                var validateStub = side.TokenContractStub;
                var validateTokenInfo = await validateStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
                    {Symbol = "SHARE"});
                if (validateTokenInfo.Equals(new TokenInfo()))
                    validateService.Add(side);
            }

            var result = await stub.ValidateTokenInfoExists.SendAsync(new ValidateTokenInfoExistsInput
            {
                Decimals = tokenInfo.Decimals,
                Issuer = tokenInfo.Issuer,
                IsBurnable = tokenInfo.IsBurnable,
                IssueChainId = tokenInfo.IssueChainId,
                IsProfitable = tokenInfo.IsProfitable,
                Symbol = tokenInfo.Symbol,
                TokenName = tokenInfo.TokenName,
                TotalSupply = tokenInfo.TotalSupply
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"{result.TransactionResult.BlockNumber},{result.TransactionResult.TransactionId}");

            var merklePath = GetMerklePath(result.TransactionResult.BlockNumber,
                result.TransactionResult.TransactionId.ToHex(), MainServices, out var root);
            var crossChainCrossToken = new CrossChainCreateTokenInput
            {
                FromChainId = MainServices.ChainId,
                MerklePath = merklePath,
                TransactionBytes = result.Transaction.ToByteString(),
                ParentChainHeight = result.TransactionResult.BlockNumber
            };

            foreach (var validate in validateService)
            {
                while (result.TransactionResult.BlockNumber > GetIndexParentHeight(validate))
                {
                    Logger.Info("Block is not recorded ");
                    Thread.Sleep(10000);
                }

                var validateStub = validate.TokenContractStub;

                var createResult =
                    await validateStub.CrossChainCreateToken.SendAsync(crossChainCrossToken);
                createResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var validateTokenInfo = await validateStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
                    {Symbol = "SHARE"});
                validateTokenInfo.Symbol.ShouldBe("SHARE");
            }
        }

        #endregion

        [TestMethod]
        public void SideChainVerifyMainChain()
        {
            var block = AsyncHelper.RunSync(() => MainServices.NodeManager.ApiClient.GetBlockByHeightAsync(100));
            var transaction = AsyncHelper.RunSync(() =>
                MainServices.NodeManager.ApiClient.GetTransactionResultsAsync(block.BlockHash)).First();
            var verifyInput =
                GetMainChainTransactionVerificationInput(transaction.BlockNumber, transaction.TransactionId);
            var result = SideAServices.CrossChainService.CallViewMethod<BoolValue>(
                CrossChainContractMethod.VerifyTransaction, verifyInput).Value;
            result.ShouldBeTrue();
        }

        #region private
        
        private async Task<long> MainChainCheckSideChainBlockIndex(ContractServices servicesFrom,
            long txHeight)
        {
            var mainHeight = long.MaxValue;
            var checkResult = false;

            while (!checkResult)
            {
                var indexSideChainBlock = GetIndexSideHeight(servicesFrom);

                if (indexSideChainBlock < txHeight)
                {
                    Logger.Info("Block is not recorded ");
                    await Task.Delay(10000);
                    continue;
                }

                mainHeight = mainHeight == long.MaxValue
                    ? await MainServices.NodeManager.ApiClient.GetBlockHeightAsync()
                    : mainHeight;
                var indexParentBlock = GetIndexParentHeight(servicesFrom);
                checkResult = indexParentBlock > mainHeight;
            }

            return mainHeight;
        }

        private long GetIndexParentHeight(ContractServices services)
        {
            return services.CrossChainService.CallViewMethod<Int64Value>(
                CrossChainContractMethod.GetParentChainHeight, new Empty()).Value;
        }

        private long GetIndexSideHeight(ContractServices services)
        {
            return MainServices.CrossChainService.CallViewMethod<Int64Value>(
                CrossChainContractMethod.GetSideChainHeight, new Int32Value {Value = services.ChainId}).Value;
        }

        private void Proposal(ContractServices services, IMessage input)
        {
            //get default organization
            var organizationAddress =
                services.ParliamentService.CallViewMethod<Address>(ParliamentMethod.GetDefaultOrganizationAddress,
                    new Empty());
            //create proposal
            var createProposalInput = new CreateProposalInput
            {
                OrganizationAddress = organizationAddress,
                ToAddress = AddressHelper.Base58StringToAddress(services.TokenService.ContractAddress),
                ContractMethodName = TokenMethod.RegisterCrossChainTokenContractAddress.ToString(),
                ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
                Params = input.ToByteString()
            };
            var createProposalResult =
                services.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.CreateProposal,
                    createProposalInput);
            if (createProposalResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined) return;
            var proposalId =
                Hash.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(createProposalResult.ReturnValue));

            //approve
            var miners = GetMiners(services);
            foreach (var miner in miners)
            {
                services.ParliamentService.SetAccount(miner.GetFormatted());
                var approveResult =
                    services.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Approve, proposalId);
                if (approveResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined) return;
            }

            services.ParliamentService.SetAccount(InitAccount);
            var releaseResult
                = services.ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Release, proposalId);
            if (releaseResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
                Assert.IsTrue(false,
                    $"Release proposal failed, token address can't register on chain {services.ChainId}");
        }

        // get miners
        private IEnumerable<Address> GetMiners(ContractServices services)
        {
            var minerList = new List<Address>();
            var miners =
                services.ConsensusService.CallViewMethod<MinerList>(ConsensusMethod.GetCurrentMinerList, new Empty());
            foreach (var publicKey in miners.Pubkeys)
            {
                var address = Address.FromPublicKey(publicKey.ToByteArray());
                minerList.Add(address);
            }

            return minerList;
        }

        private VerifyTransactionInput GetMainChainTransactionVerificationInput(long blockHeight, string txId)
        {
            var merklePath = GetMerklePath(blockHeight, txId, MainServices, out var hash);
            if (merklePath == null) return null;

            var verificationInput = new VerifyTransactionInput
            {
                ParentChainHeight = blockHeight,
                TransactionId = HashHelper.HexStringToHash(txId),
                VerifiedChainId = MainServices.ChainId,
                Path = merklePath
            };
            return verificationInput;
        }

        #endregion
    }
}