using System.Collections.Generic;
using System.Threading;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.SDK.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.SideChain.Verification.CrossChainTransfer
{
    public class CrossChainTransferPrepare : CrossChainBase
    {
        public CrossChainTransferPrepare()
        {
            MainChainService = InitMainChainServices();
            SideChainServices = InitSideChainServices();
        }

        public void DoCrossChainTransferPrepare()
        {
            Logger.Info($"Main chain transfer {NativeToken} to other side chain InitAccount");
            CrossChainTransferToInitAccount(NativeToken);

            Logger.Info($"Init account transfer {NativeToken} to other account");
            InitCrossChainTransfer(NativeToken);

            if (TokenSymbol.Count.Equals(0)) return;
            foreach (var symbol in TokenSymbol)
            {
                Logger.Info($"Main chain transfer {symbol} to other side chain InitAccount");
                CrossChainTransferToInitAccount(symbol);

                Logger.Info($"Init account transfer {symbol} to other account");
                InitOtherTokenCrossChainTransfer(symbol);
            }
        }

        private void CrossChainTransferToInitAccount(string symbol)
        {
            //Main Chain Transfer to SideChain
            //Get all side chain id;
            Logger.Info("Main chan transfer to side chain InitAccount ");
            var initRawTxInfos = new Dictionary<int, CrossChainTransactionInfo>();
            foreach (var sideChainService in SideChainServices)
            {
                CrossChainTransactionInfo rawTxInfo = null;
                var transferTimes = 3;
                while (rawTxInfo == null && transferTimes > 0)
                {
                    transferTimes--;
                    rawTxInfo = CrossChainTransferWithResult(MainChainService, symbol, InitAccount, InitAccount,
                        sideChainService.ChainId, 200000);
                }

                Assert.IsTrue(transferTimes > 0 || rawTxInfo != null,
                    "The first cross chain transfer failed, please start over.");

                initRawTxInfos.Add(sideChainService.ChainId, rawTxInfo);
                Logger.Info(
                    $"the transactions block is:{rawTxInfo.BlockHeight},transaction id is: {rawTxInfo.TxId}");
            }

            Logger.Info("Waiting for the index");
            Thread.Sleep(200000);

            foreach (var sideChainService in SideChainServices)
            {
                Logger.Info($"Side chain {sideChainService.ChainId} received token");
                Logger.Info(
                    $"Receive CrossTransfer Transaction id is : {initRawTxInfos[sideChainService.ChainId].TxId}");

                CommandInfo result = null;
                var transferTimes = 5;
                while (result == null && transferTimes > 0)
                {
                    transferTimes--;
                    var input = ReceiveFromMainChainInput(initRawTxInfos[sideChainService.ChainId]);
                    result = sideChainService.TokenService.ExecuteMethodWithResult(TokenMethod.CrossChainReceiveToken,
                        input);
                }

                if (result == null)
                {
                    Logger.Error($"Chain {sideChainService.ChainId} receive transaction is failed.");
                    Assert.IsTrue(false, "The first receive transfer failed, please start over.");
                }

                var resultReturn = result.InfoMsg as TransactionResultDto;
                var status = resultReturn.Status.ConvertTransactionResultStatus();
                if (status == TransactionResultStatus.NotExisted || status == TransactionResultStatus.Failed)
                {
                    Thread.Sleep(1000);
                    var checkTime = 3;
                    while (checkTime > 0)
                    {
                        Logger.Info($"Receive {4 - checkTime} time");
                        checkTime--;
                        var input = ReceiveFromMainChainInput(initRawTxInfos[sideChainService.ChainId]);
                        var reResult = sideChainService.TokenService.ExecuteMethodWithResult(
                            TokenMethod.CrossChainReceiveToken,
                            input);
                        if (reResult == null) continue;
                        var reResultReturn = reResult.InfoMsg as TransactionResultDto;
                        var reStatus = reResultReturn.Status.ConvertTransactionResultStatus();
                        if (reStatus == TransactionResultStatus.Mined)
                            goto GetBalance;
                        if (reResultReturn.Error.Contains("Token already claimed"))
                            goto GetBalance;
                        Thread.Sleep(2000);
                    }

                    Logger.Error($"Chain {sideChainService.ChainId} receive transaction is failed.");
                    Assert.IsTrue(false, "The first receive transfer failed, please start over.");
                }

                GetBalance:
                Logger.Info($"check the balance on the side chain {sideChainService.ChainId}");
                var accountBalance = GetBalance(sideChainService, InitAccount, symbol);

                Logger.Info(
                    $"On side chain {sideChainService.ChainId}, InitAccount:{InitAccount}, {symbol} balance is {accountBalance}");
            }
        }

        private void InitCrossChainTransfer(string symbol)
        {
            AccountList = new Dictionary<int, List<string>>();
            Logger.Info("Create account on main chain:");
            var mainAccounts = NewAccount(MainChainService, 10);
            AccountList.Add(MainChainService.ChainId, mainAccounts);

            Logger.Info("Create account on each side chain:");
            foreach (var sideChainService in SideChainServices)
            {
                Logger.Info($"Create account on chain {sideChainService.ChainId} :");
                var accounts = NewAccount(sideChainService, 10);
                AccountList.Add(sideChainService.ChainId, accounts);
            }

            // Unlock account on main chain 
            foreach (var accountList in AccountList)
            {
                UnlockAccounts(MainChainService, accountList.Value.Count, accountList.Value);
                UnlockAccounts(MainChainService, mainAccounts.Count, mainAccounts);
            }

            // Unlock account on side chain
            foreach (var sideChainService in SideChainServices)
            {
                UnlockAccounts(sideChainService, mainAccounts.Count, mainAccounts);

                foreach (var sideChain in SideChainServices)
                {
                    UnlockAccounts(sideChainService, AccountList[sideChain.ChainId].Count,
                        AccountList[sideChainService.ChainId]);
                }
            }

            Logger.Info("Transfer token to each account :");

            var mainTransferTxIds = new List<CrossChainTransactionInfo>();
            foreach (var mainChainAccount in mainAccounts)
            {
                var mainTxId = Transfer(MainChainService, InitAccount, mainChainAccount, 10000, symbol);
                var mainTxInfo = new CrossChainTransactionInfo(mainTxId, mainChainAccount);
                mainTransferTxIds.Add(mainTxInfo);
            }

            var mainResult = CheckoutTransferResult(MainChainService, mainTransferTxIds);

            if (mainResult[TransactionResultStatus.Failed].Count != 0)
            {
                foreach (var mainTxInfo in mainResult[TransactionResultStatus.Failed])
                {
                    var account = mainTxInfo.ReceiveAccount;
                    Logger.Info($"Transfer on main chain again: account {account}");
                    Transfer(MainChainService, InitAccount, account, 10000, symbol);
                }
            }

            foreach (var sideChainService in SideChainServices)
            {
                var transferTxIds = new List<CrossChainTransactionInfo>();
                foreach (var sideAccount in AccountList[sideChainService.ChainId])
                {
                    var sideTxId = Transfer(sideChainService, InitAccount, sideAccount, 10000, symbol);
                    var sideTxInfo = new CrossChainTransactionInfo(sideTxId, sideAccount);
                    transferTxIds.Add(sideTxInfo);
                }

                var sideResult = CheckoutTransferResult(sideChainService, transferTxIds);
                if (sideResult[TransactionResultStatus.Failed].Count != 0)
                {
                    foreach (var sideTxInfo in sideResult[TransactionResultStatus.Failed])
                    {
                        var account = sideTxInfo.ReceiveAccount;
                        Logger.Info($"Transfer on side chain {sideChainService.ChainId} again: account {account}");
                        Transfer(sideChainService, InitAccount, account, 10000, symbol);
                    }
                }
            }

            Logger.Info("Show the main chain account balance: ");
            foreach (var mainAccount in AccountList[MainChainService.ChainId])
            {
                var accountBalance = GetBalance(MainChainService, mainAccount, symbol);
                Logger.Info($"Account:{mainAccount}, {symbol} balance is:{accountBalance}");
            }

            Logger.Info("show the side chain account balance: ");
            foreach (var sideChainService in SideChainServices)
            {
                foreach (var sideAccount in AccountList[sideChainService.ChainId])
                {
                    var accountBalance = GetBalance(sideChainService, sideAccount, symbol);
                    Logger.Info($"Account:{sideAccount}, {symbol} balance is: {accountBalance}");
                }
            }
        }


        private void InitOtherTokenCrossChainTransfer(string symbol)
        {
            Logger.Info("Transfer token to each account :");

            var mainTransferTxIds = new List<CrossChainTransactionInfo>();
            foreach (var mainChainAccount in AccountList[MainChainService.ChainId])
            {
                var mainTxId = Transfer(MainChainService, InitAccount, mainChainAccount, 10000, symbol);
                var mainTxInfo = new CrossChainTransactionInfo(mainTxId, mainChainAccount);
                mainTransferTxIds.Add(mainTxInfo);
            }

            var mainResult = CheckoutTransferResult(MainChainService, mainTransferTxIds);

            if (mainResult[TransactionResultStatus.Failed].Count != 0)
            {
                foreach (var mainTxInfo in mainResult[TransactionResultStatus.Failed])
                {
                    var account = mainTxInfo.ReceiveAccount;
                    Logger.Info($"Transfer on main chain again: account {account}");
                    Transfer(MainChainService, InitAccount, account, 10000, symbol);
                }
            }

            foreach (var sideChainService in SideChainServices)
            {
                var transferTxIds = new List<CrossChainTransactionInfo>();
                foreach (var sideAccount in AccountList[sideChainService.ChainId])
                {
                    var sideTxId = Transfer(sideChainService, InitAccount, sideAccount, 10000, symbol);
                    var sideTxInfo = new CrossChainTransactionInfo(sideTxId, sideAccount);
                    transferTxIds.Add(sideTxInfo);
                }

                var sideResult = CheckoutTransferResult(sideChainService, transferTxIds);
                if (sideResult[TransactionResultStatus.Failed].Count != 0)
                {
                    foreach (var sideTxInfo in sideResult[TransactionResultStatus.Failed])
                    {
                        var account = sideTxInfo.ReceiveAccount;
                        Logger.Info($"Transfer on side chain {sideChainService.ChainId} again: account {account}");
                        Transfer(sideChainService, InitAccount, account, 10000, symbol);
                    }
                }
            }

            Logger.Info("Show the main chain account balance: ");
            foreach (var mainAccount in AccountList[MainChainService.ChainId])
            {
                var accountBalance = GetBalance(MainChainService, mainAccount, symbol);
                Logger.Info($"Account:{mainAccount}, {symbol} balance is:{accountBalance}");
            }

            Logger.Info("show the side chain account balance: ");
            foreach (var sideChainService in SideChainServices)
            {
                foreach (var sideAccount in AccountList[sideChainService.ChainId])
                {
                    var accountBalance = GetBalance(sideChainService, sideAccount, symbol);
                    Logger.Info($"Account:{sideAccount}, {symbol} balance is: {accountBalance}");
                }
            }
        }

        private string Transfer(ContractServices services, string initAccount, string toAddress, long amount,
            string symbol)
        {
            services.TokenService.SetAccount(initAccount);
            var transferId =
                services.TokenService.ExecuteMethodWithTxId(
                    TokenMethod.Transfer, new TransferInput
                    {
                        Symbol = symbol,
                        Amount = amount,
                        To = AddressHelper.Base58StringToAddress(toAddress),
                        Memo = "Transfer init amount"
                    });
            return transferId;
        }
    }
}