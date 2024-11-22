using AElfChain.Common.Helpers;
using Newtonsoft.Json;

namespace AElfTestContractScript;

public class ConfigInfo
{
    [JsonProperty("ServiceUrlList")] public List<string> ServiceUrlList { get; set; }
    [JsonProperty("InitAccount")] public string InitAccount { get; set; }
    [JsonProperty("Password")] public string Password { get; set; }
    [JsonProperty("IsNeedDelegatee")] public bool IsNeedDelegatee { get; set; }
    [JsonProperty("IsProxyAccount")] public bool IsProxyAccount { get; set; }
    [JsonProperty("IsParallel")] public bool IsParallel { get; set; }
    [JsonProperty("InitFeeAmount")] public long InitFeeAmount { get; set; }
    [JsonProperty("TestContractAddress")] public string TestContractAddress { get; set; }
    [JsonProperty("ProxyAccountContract")] public string ProxyAccountContract { get; set; }
    [JsonProperty("TransactionInfo")] public TransactionInfo TransactionInfo { get; set; }
    [JsonProperty("TestDuration")] public int TestDuration { get; set; }

    public static ConfigInfo ReadInformation => ConfigHelper<ConfigInfo>.GetConfigInfo("test-config.json", false);
}

public class TransactionInfo
{
    [JsonProperty("SenderCount")] public int SenderCount { get; set; }
    [JsonProperty("TransactionCount")] public int TransactionCount { get; set; }
    [JsonProperty("TransactionSize")] public long TransactionSize { get; set; }
    [JsonProperty("GroupDuration")] public int GroupDuration { get; set; }
    [JsonProperty("SentTxLimit")] public int SentTxLimit { get; set; }
    [JsonProperty("TransactionLimit")] public int TransactionLimit { get; set; }
}