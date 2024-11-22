using AElf.CSharp.Core;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;

namespace AElfTestContractScript;

internal class Program
{
    private static readonly ILog Logger = Log4NetHelper.GetLogger();

    private static async Task Main()
    {
        var fileName = "AElfTestContract";
        Log4NetHelper.LogInit(fileName);
        var config = ConfigInfo.ReadInformation;
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
            var testService = new TestContractService(serviceList.First(), Logger, config.TestContractAddress, 
                eoaTestAccount, toAccountList);
            testService.InitializeContract();
            var symbols = testService.InitTestContractTest(Constants.DefaultTokenCount, eoaTestAccount);
            var transactionService =
                new TransactionService(Logger, testService, testDuration, size, eoaTestAccount, serviceList, symbols);
            transactionService.ExecuteTransactionsWithoutResultTask(isParallel
                ? "TestTransfer"
                : "TransferWithoutParallel");
        }
    }
}