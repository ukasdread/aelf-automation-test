using System.Threading;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken.Messages;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.SideChainTests
{
    [TestClass]
    public class SideChainVerify : SideChainTestBase
    {
        public static string SideARpcUrl { get; } = "http://192.168.197.16:8011";
        public static string SideBRpcUrl { get; } = "http://192.168.197.26:8011";
        public IApiHelper SideChainA { get; set; }
        public IApiHelper SideChainB { get; set; }
        public ContractTester TesterA;
        public ContractTester TesterB;
        public string sideChainAccount = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        public string sideChainBccount = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";

        [TestInitialize]
        public void InitializeNodeTests()
        {
            base.Initialize();
        }

        #region cross chain verify 

        [TestMethod]
        [DataRow("2QhTob7XyrbvByB9X1ymYKdTYM57rhHJ2w3rC3a3imWycAYBL9", 1000)]
        public void TransferOnMainChain(string toAddress, long amount)
        {
            var result = Tester.TransferToken(InitAccount, toAddress, amount, "ELF");
            var transferResult = result.InfoMsg as TransactionResultDto;
            var txIdInString = transferResult.TransactionId;
            var blockNumber = transferResult.BlockNumber;

            _logger.WriteInfo($"{txIdInString},{blockNumber}");
        }

        [TestMethod]
        [DataRow("ac50644b0fbd579f1458ec0c7347b9d5d66546a777bce3b10756e31dc57bfbeb","11490",1)]
        public void VerifyMainChainTransaction(string txIdInString,string blockNumber,string txid)
        {
            var merklePath = GetMerklePath(blockNumber, txid, Tester);

            var verificationInput = new VerifyTransactionInput
            {
                ParentChainHeight = long.Parse(blockNumber),
                TransactionId = Hash.LoadHex(txIdInString),
                VerifiedChainId = 9992731
            };
            verificationInput.Path.AddRange(merklePath.Path);

            // change to side chain a to verify
            SideChainA = ChangeRpc(SideARpcUrl);
            TesterA = ChangeToSideChain(SideChainA, sideChainAccount);

            Thread.Sleep(4000);

            var result = TesterA.VerifyTransaction(verificationInput, sideChainAccount);
            var verifyResult = result.InfoMsg as TransactionResultDto;
            verifyResult.ReadableReturnValue.ShouldBe("true");
        }

        [TestMethod]
        [DataRow("2QhTob7XyrbvByB9X1ymYKdTYM57rhHJ2w3rC3a3imWycAYBL9",1000)]
        public void TransferOnsideChain(string toAddress,long amount)
        {
            // change to side chain a to verify
            SideChainA = ChangeRpc(SideARpcUrl);
            TesterA = ChangeToSideChain(SideChainA,sideChainAccount);
            
            var result = TesterA.TransferToken(InitAccount, toAddress, amount, "ELF");
            var transferResult = result.InfoMsg as TransactionResultDto;
            var txIdInString = transferResult.TransactionId;
            var blockNumber = transferResult.BlockNumber;

            _logger.WriteInfo($"{txIdInString},{blockNumber}");
        }

        [TestMethod]
        [DataRow("8e8c7176bf37ef1ce8f8414fdff54a68d67e9b9da9124c659f46910593c292d1","12793",1)]
        public void VerifysideACHainTransaction(string txIdInString,string blockNumber,string txid)
        {
            SideChainA = ChangeRpc(SideARpcUrl);
            TesterA = ChangeToSideChain(SideChainA, sideChainAccount);
            ISA = new WebApiService(SideARpcUrl);

            var merklePath = GetMerklePath(blockNumber, txid, TesterA);
            var verificationInput = new VerifyTransactionInput
            {
                TransactionId = Hash.LoadHex(txIdInString),
                VerifiedChainId = 2750978
            };
            verificationInput.Path.AddRange(merklePath.Path);

            // verify side chain transaction
            var crossChainMerkleProofContext =
                TesterA.GetBoundParentChainHeightAndMerklePathByHeight(sideChainAccount, long.Parse(blockNumber));
            verificationInput.Path.AddRange(crossChainMerkleProofContext.MerklePathForParentChainRoot.Path);
            verificationInput.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;

            //verify in main chain            
            var result =
                Tester.VerifyTransaction(verificationInput, InitAccount);
            var verifyResult = result.InfoMsg as TransactionResultDto;
            verifyResult.ReadableReturnValue.ShouldBe("true");

//            //change to side chain B
//            SideChainB = ChangeRpc(SideBRpcUrl);
//            TesterB = ChangeToSideChain(SideChainB,sideChainBccount);
//
//            var result2 =
//                TesterB.VerifyTransaction(verificationInput, sideChainBccount);
//            var verifyResult2 = result2.InfoMsg as TransactionResultDto;
//            verifyResult2.ReadableReturnValue.ShouldBe("true");
        }

        #endregion

        #region cross chain transfer

        [TestMethod]
        [DataRow("2A1RKFfxeh2n7nZpcci6t8CcgbJMGz9a7WGpC94THpiTK3U7nG", 2750978)]
        public void MainChainTransferSideChainA(string accountA, int toChainId)
        {
            //get token info
            var tokenInfo = Tester.GetTokenInfo("ELF");
            //Transfer
            var result = Tester.CrossChainTransfer(InitAccount, accountA, tokenInfo, toChainId, 1000);
            var resultReturn = result.InfoMsg as TransactionResultDto;
            var blockNumber = resultReturn.BlockNumber;
            _logger.WriteInfo($"Block Number: {blockNumber}");
        }

        [TestMethod]
        [DataRow("2A1RKFfxeh2n7nZpcci6t8CcgbJMGz9a7WGpC94THpiTK3U7nG","122783","724046442d42fddc391204a3fbb75da4206dc141060fa5771838de775d2db628","0a220a200e0859791a72bc512b4b91edfd12116daddf7c9f8915d608e2313f4772e761df12220a2043a0f4a61fd597aee85d15e13bfa96e70b82a7071ca25e62c3176a80b8231ae21897bf072204f6e454cc2a1243726f7373436861696e5472616e73666572328a010a220a209825ea6ae8e17764b3d6561088d21134d8683419ef386257ae7e9404f47fd34212440a03454c461209656c6620746f6b656e1880a8d6b9072080a8d6b907280432220a20dd8eea50c31966e06e4a2662bebef7ed81d09a47b2eb1eb3729f2f0cc78129ae380118d00f22167472616e7366657220746f207369646520636861696e2882f4a70182f10441ee90268fda8a449de808141b2aa9d305522ea3f48764659761ec5356fc4b363032c28263d88e71a65867ec6457bf2effada1a1b715cd4c5a78eb57c520adcc2a00")]
        public void SideChainAReceive(string accountA,string blockNumber, string txid,string rawTx)
        {
            var merklePath = GetMerklePath(blockNumber, txid, Tester);

            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = 9992731,
                ParentChainHeight = long.Parse(blockNumber)
            };
            crossChainReceiveToken.MerklePath.AddRange(merklePath.Path);
            crossChainReceiveToken.TransferTransactionBytes =
                ByteString.CopyFrom(ByteArrayHelper.FromHexString(rawTx));

            SideChainA = ChangeRpc(SideARpcUrl);
            TesterA = ChangeToSideChain(SideChainA,sideChainAccount);
            TesterA.CrossChainReceive(InitAccount, crossChainReceiveToken);
            
            //verify
            var balance = TesterA.GetBalance(accountA, "ELF");
            _logger.WriteInfo($"balance: {balance}");

            var tokenInfo = TesterA.GetTokenInfo("ELF");
            _logger.WriteInfo($"Token: {tokenInfo}");
        }

        [TestMethod]
        [DataRow("", "")]
        public void SideChainATransferSideChainB(string accountA, string accountB, int toChainId)
        {
            SideChainA = ChangeRpc(SideARpcUrl);
            TesterA = ChangeToSideChain(SideChainA, sideChainAccount);
            ISA = new WebApiService(SideARpcUrl);
            //get tokenInfo
            var tokenInfo = TesterA.GetTokenInfo("ELF");
            //Transfer
            var result = Tester.CrossChainTransfer(accountA, accountB, tokenInfo, toChainId, 1000);
            var resultReturn = result.InfoMsg as TransactionResultDto;
            var blockNumber = resultReturn.BlockNumber;
            _logger.WriteInfo($"Block Number: {blockNumber}");
        }

        [TestMethod]
        [DataRow("", "", "")]
        public void SideChainBReceive(string accountB, string blockNumber, string txid, string rawTx)
        {
            SideChainA = ChangeRpc(SideARpcUrl);
            TesterA = ChangeToSideChain(SideChainA, sideChainAccount);
            ISA = new WebApiService(SideARpcUrl);

            var merklePath = GetMerklePath(blockNumber, txid, TesterA);

            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = 2750978,
            };
            crossChainReceiveToken.MerklePath.AddRange(merklePath.Path);

            // verify side chain transaction
            var crossChainMerkleProofContext =
                TesterA.GetBoundParentChainHeightAndMerklePathByHeight(sideChainAccount, long.Parse(blockNumber));
            crossChainReceiveToken.MerklePath.AddRange(crossChainMerkleProofContext.MerklePathForParentChainRoot.Path);
            crossChainReceiveToken.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
            crossChainReceiveToken.TransferTransactionBytes =
                ByteString.CopyFrom(ByteArrayHelper.FromHexString(rawTx));

            //receive in side chain B
            SideChainB = ChangeRpc(SideBRpcUrl);
            TesterB = ChangeToSideChain(SideChainB, sideChainBccount);
            ISB = new WebApiService(SideBRpcUrl);
            TesterB.CrossChainReceive(accountB, crossChainReceiveToken);

            //verify
            var balance = TesterA.GetBalance(accountB, "ELF");
            _logger.WriteInfo($"balance: {balance}");

            var tokenInfo = TesterA.GetTokenInfo("ELF");
            _logger.WriteInfo($"Token: {tokenInfo}");
        }

        [TestMethod]
        [DataRow("", "")]
        public void SideChainTransferMainChain(string accountB, string accountM, int toChainId)
        {
            SideChainA = ChangeRpc(SideARpcUrl);
            TesterA = ChangeToSideChain(SideChainA,sideChainAccount);
            ISA = new WebApiService(SideARpcUrl);
            //get ELF token info
            var tokenInfo = TesterB.GetTokenInfo("ELF");
            //Transfer
            var result = TesterB.CrossChainTransfer(accountB, accountM, tokenInfo, toChainId, 1000);
            var resultReturn = result.InfoMsg as TransactionResultDto;
            var blockNumber = resultReturn.BlockNumber;
            _logger.WriteInfo($"Block Number: {blockNumber}");
        }

        [TestMethod]
        [DataRow("2iimYTf2mn134pAsRqRT2a1kEadNVGkBZdNgE8na9y4RnwiPRU","2460",1,"0a220a20e26d40e0021a6dd128cda7d682d8cd9dba10feda55d8c689d1563fb0b313c8e812220a2080ee395414cb759fc3a997f2b1a3db506aa9779bebbdef653aec7121fd681da61893132204dfcc9d022a1243726f7373436861696e5472616e736665723288010a220a200640bf6b4c86d22ebf32cd8f96135c421990326febdc94256d3b0f5f1ac89ff712440a03454c461209656c6620746f6b656e18fca7d6b9072080a8d6b907280432220a20dd8eea50c31966e06e4a2662bebef7ed81d09a47b2eb1eb3729f2f0cc78129ae380118c801221463726f737320636861696e207472616e73666572289bf4e10482f104412bd970d3de35175f33e74e0563a82b1e87792096ce5c72859f29d23a425f905512bc028dbd27ecc68995f2934405d66ee55a6bc06d47cfdc0a639aa620e4f93a01")]
        public void MainChainReceive(string accountM,string blockNumber,string txid,string rawTx)
        {
            SideChainB = ChangeRpc(SideBRpcUrl);
            TesterB = ChangeToSideChain(SideChainB, sideChainBccount);
            ISB = new WebApiService(SideBRpcUrl);

            var merklePath = GetMerklePath(blockNumber, txid, TesterB);

            var crossChainReceiveToken = new CrossChainReceiveTokenInput
            {
                FromChainId = 2750978,
            };
            crossChainReceiveToken.MerklePath.AddRange(merklePath.Path);

            // verify side chain transaction
            var crossChainMerkleProofContext =
                TesterB.GetBoundParentChainHeightAndMerklePathByHeight(sideChainAccount, long.Parse(blockNumber));
            crossChainReceiveToken.MerklePath.AddRange(crossChainMerkleProofContext.MerklePathForParentChainRoot.Path);
            crossChainReceiveToken.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
            crossChainReceiveToken.TransferTransactionBytes = ByteString.CopyFrom(ByteArrayHelper.FromHexString(rawTx));
            
            Tester.UnlockAllAccounts(Tester.ContractServices,accountM);
            //receive in main chain
            Tester.CrossChainReceive(accountM, crossChainReceiveToken);

            //verify
            var balance = Tester.GetBalance(accountM, "ELF");
            _logger.WriteInfo($"balance: {balance}");
            
            var tokenInfo = Tester.GetTokenInfo("ELF");
            _logger.WriteInfo($"Token: {tokenInfo}");
        }

        #endregion
    }
}