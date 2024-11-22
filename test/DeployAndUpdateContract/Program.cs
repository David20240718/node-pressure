using System.Text.Json;
using AElfChain.Common;
using AElfChain.Common.Helpers;
using log4net;

namespace DeployAndUpdateContract
{
    internal class Program
    {
        #region Private Properties

        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        #endregion

        private static void Main(string[] args)
        {
            #region Basic Preparation

            //Init Logger
            Log4NetHelper.LogInit("DeployAndUpdate");
            var testEnvironment = ConfigHelper.Config?.Environment;
            NodeInfoHelper.SetConfig(testEnvironment);
            var configFile = ConfigHelper.Config;
            var nodeInfo = NodeOption.AllNodes.First();
            var authorInfo = configFile.AuthorInfo;

            #endregion

            var basicService = new Service(nodeInfo.Endpoint, nodeInfo.Account, nodeInfo.Password);
            var service = new DeployAndUpdateService(basicService, Logger);
            Logger.Info($"InitAccount: {nodeInfo.Account}");
            Logger.Info("======== Check miners balance ========");
            service.CheckMinersAndInitAccountBalance();
            DeployAndUpdateService.ContractInfoDto contractInfoDto =
                new DeployAndUpdateService.ContractInfoDto("", 0, "", "");
            switch (configFile?.Type)
            {
                case "Deploy":
                    Logger.Info("======== Deploy =======");
                    Logger.Info($"======== Deploy File: {configFile.ContractFileName} ========");
                    contractInfoDto = service.DeployContracts(configFile.isApproval, configFile.ContractFileName,
                        authorInfo, configFile.Salt);
                    break;
                case "Update":
                    Logger.Info("========= Update =======");
                    Logger.Info($"======== Update File: {configFile.ContractFileName} ========");
                    var contract = configFile.UpdateInfo.ContractAddress.Equals("")
                        ? configFile.UpdateInfo.ContractName
                        : configFile.UpdateInfo.ContractAddress;
                    Logger.Info($"======== Update Contract: {contract} ========");
                    contractInfoDto = service.UpdateContracts(configFile.isApproval, configFile.UpdateInfo,
                        configFile.ContractFileName, authorInfo, configFile.Salt);
                    break;
            }

            var json = JsonSerializer.Serialize(contractInfoDto);

            var path = CommonHelper.MapPath($"logs/");
            File.WriteAllText($"{path}/info.json", json);
        }
    }
}