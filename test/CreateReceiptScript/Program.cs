// See https://aka.ms/new-console-template for more information

using AElfChain.Common;
using AElfChain.Common.Helpers;
using CreateReceiptScript;
using log4net;

internal class Program
{
    #region Private Properties

    private static readonly ILog Logger = Log4NetHelper.GetLogger();

    #endregion

    private static void Main(string[] args)
    {
        #region Basic Preparation

        //Init Logger
        Log4NetHelper.LogInit("CreateReceipt");
        var config = ConfigInfo.ReadInformation;
        NodeInfoHelper.SetConfig(config.ConfigFile);
        var nodeInfo = NodeOption.AllNodes.First();
        var transactionCount = config.TransactionCount;
        var bridgeInfos = config.BridgeInfos;
        #endregion

        var userCount = config.UserCount;
        var service = new Service(config.ServiceUrl, nodeInfo.Account, nodeInfo.Password);
        var initService = new InitService(service, Logger, config.ChainTypeOption.IsSideChain,
            config.ChainTypeOption.MainChainUrl);
        var testList = initService.GetTestAccounts(userCount);
        foreach (var token in config.BridgeInfos.SelectMany(bridgeInfo => bridgeInfo.Value.SymbolList))
        {
            if (!token.Equals(NodeOption.NativeTokenSymbol))
                initService.IssueBalanceToInitAccount(token);
            
            initService.TransferAccount(testList, token);
            if (config.ChainTypeOption.IsSideChain)
            {
                initService.CrossTransferToInitAccount(token);
            }
        }

        foreach (var bridgeInfo in bridgeInfos)
        {
            var bridgeService = new BridgeService(service, testList, Logger, bridgeInfo.Value.BridgeAddress);
            foreach (var symbol in bridgeInfo.Value.SymbolList)
            {
                bridgeService.ApproveAccount(symbol);
            }
        }

        for (var r = 1; r < 2; r++) //continuous running
        {
            try
            {
                foreach (var bridgeInfo in bridgeInfos)
                {
                    var bridgeService = new BridgeService(service, testList, Logger, bridgeInfo.Value.BridgeAddress);

                    var txsTasks = new List<Task>();
                    foreach (var symbol in bridgeInfo.Value.SymbolList)
                    {
                        txsTasks.Add(Task.Run(() =>
                            bridgeService.CreateReceipt(symbol, bridgeInfo.Value.TargetChainId,
                                bridgeInfo.Value.TargetAddress, transactionCount)));
                    }

                    Task.WaitAll(txsTasks.ToArray<Task>());
                }
                Thread.Sleep(20000);
            }
            catch (AggregateException exception)
            {
                Logger.Error(
                    $"Request to {service.NodeManager.GetApiUrl()} got exception, {exception.Message}");
            }
            catch (Exception e)
            {
                var message = "Execute continuous transaction got exception." +
                              $"\r\nMessage: {e.Message}" +
                              $"\r\nStackTrace: {e.StackTrace}";
                Logger.Error(message);
            }
        }
    }
}
