using AElfChain.Common;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using BeangoTownScript;
using log4net;

internal class Program
{
    #region Private Properties

    private static readonly ILog Logger = Log4NetHelper.GetLogger();

    #endregion

    private static async Task Main(string[] args)
    {
        #region Basic Preparation

        //Init Logger
        Log4NetHelper.LogInit("Beangotown");
        var config = ConfigInfo.ReadInformation;
        NodeInfoHelper.SetConfig(config.ConfigFile);
        var nodeInfo = NodeOption.AllNodes.First();
        // var transactionCount = config.TransactionCount;
        var beangoTownAddress = config.BeangoTownAddress;
        var caAddressMain = config.CaAddressMain;
        var caAddressSide = config.CaAddressSide;
        var creatorController = config.creatorController;

        #endregion

        var mainServer = new Service(config.mainChain_url, nodeInfo.Account, nodeInfo.Password);
        var sideServer = new Service(config.sideChain_url, nodeInfo.Account, nodeInfo.Password);
        var beangoTownServerUrl = config.beangoTownServerUrl;

        var beangoService = new BeangoTownService(mainServer, sideServer, beangoTownServerUrl, Logger,
            beangoTownAddress, caAddressMain, caAddressSide, creatorController);

        var count = 170;
        Logger.Info($"UserCountNum:{count}");
        Task[] tasks = new Task[count];
        for (int i = 0; i < count; i++)
        {
            tasks[i] = Task.Run(() => beangoService.BeangoTownGoOnePerson());
            // tasks[i] = await Task.Run(async () =>  beangoService.BeangoTownGoOnePerson());
        }

        // for (int i = 0; i < count; i++)
        // {
        //     tasks[i] = Task.Run(() => beangoService.GetCaAccount1());
        // }


        await Task.WhenAll(tasks);

        Console.WriteLine("All tasks completed");
    }
}