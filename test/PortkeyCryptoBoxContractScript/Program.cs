using AElf.CSharp.Core;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;

namespace PortkeyCryptoBoxContractScript;

internal class Program
{
    private static readonly ILog Logger = Log4NetHelper.GetLogger();

    private static async Task Main()
    {
        var fileName = "PortkeyCryptoBoxContractScript";
        Log4NetHelper.LogInit(fileName);
        var config = ConfigInfo.ReadInformation;
        var cryptoBoxInfo = config.CryptoBoxInfo;
        var boxCount = cryptoBoxInfo.BoxCount;
        var transferCryptoCount = cryptoBoxInfo.TransferCryptoCount;
        var cryptoBoxInfoTestContractAddress = cryptoBoxInfo.TestContractAddress;
        
        var txInfo = config.TransactionInfo;
        var senderCount = txInfo.SenderCount;
        var size = txInfo.TransactionSize;
        var feeAmount = config.InitFeeAmount;
        var testDuration = config.TestDuration;
        var isParallel = config.IsParallel;
        var isProxy = config.IsProxyAccount;
        var urlList = config.ServiceUrlList;
        var serviceList = urlList.Select(url => new Service(url, config.InitAccount, config.Password)).ToList();

        var exceptLimit = config.TransactionInfo.TransactionLimit;
        var accountService = new AccountService(serviceList.First(), Logger);
        var prepareService = new PrepareService(serviceList.First(), Logger);
        prepareService.SetFreeAllowance();
        prepareService.SetTransactionLimit(exceptLimit);
        
        var eoaTestAccount = accountService.InitSymbolAndAccount(senderCount, feeAmount);

        if (isProxy)
        {
            var proxyService = new ProxyService(serviceList.First(), accountService, Logger,
                config.ProxyAccountContract);
            var proxyInfo = proxyService.InitProxyAccount(config.IsNeedDelegatee, eoaTestAccount);
            var transactionService =
                new TransactionService(Logger, proxyService, testDuration, size, eoaTestAccount, serviceList);
            transactionService.ExecuteContinuousRoundsTransactionsWithProxyTask(proxyInfo);
        }
        else
        {
            var toAccountList = accountService.GenerateTesterPair(eoaTestAccount);
            var testTokenService = new TestContractService(serviceList.First(), Logger, config.TestContractAddress, 
                eoaTestAccount, toAccountList);
            
            testTokenService.InitializeContract();
            
            var symbols = testTokenService.InitTestContractTest(Constants.DefaultTokenCount, eoaTestAccount);
            
            var TestCryptoBoxContract = new TestCryptoBoxContractService(serviceList.First(), Logger, cryptoBoxInfoTestContractAddress, 
                eoaTestAccount, eoaTestAccount[0],toAccountList,AElfKeyStore.GetKeyStore(""),boxCount,transferCryptoCount);

            // symbols.Clear();
            // symbols.Add("ELF");
            
            TestCryptoBoxContract.InitializeContract(config.TestContractAddress);
            TestCryptoBoxContract.InitTestContractTest(symbols);

            
            var tryptoBoxService =
                new TransactionService(Logger, TestCryptoBoxContract, testDuration, size, eoaTestAccount, serviceList, symbols);
            tryptoBoxService.ExecuteTransactionsWithoutResultTask("TransferCryptoBoxes");
        }
    }
}