using AElfChain.Common.Helpers;
using Newtonsoft.Json;

namespace CreateReceiptScript;

public class ConfigInfo
{
    [JsonProperty("TransactionCount")] public int TransactionCount { get; set; }
    [JsonProperty("UserCount")] public int UserCount { get; set; }
    [JsonProperty("ServiceUrl")] public string ServiceUrl { get; set; }
    [JsonProperty("ConfigFile")] public string ConfigFile { get; set; }
    [JsonProperty("ChainType")] public ChainTypeOption ChainTypeOption { get; set; }
    [JsonProperty("BridgeInfos")] public Dictionary<string, BridgeInfo> BridgeInfos { get; set; }

    public static ConfigInfo ReadInformation => ConfigHelper<ConfigInfo>.GetConfigInfo("config.json", false);
}

public class ChainTypeOption
{
    [JsonProperty("is_side_chain")] public bool IsSideChain { get; set; }
    [JsonProperty("main_chain_url")] public string MainChainUrl { get; set; }
}

public class BridgeInfo
{
    [JsonProperty("BridgeAddress")] public string BridgeAddress { get; set; }
    [JsonProperty("TargetChainId")] public string  TargetChainId{ get; set; }
    [JsonProperty("TargetAddress")] public string TargetAddress { get; set; }
    [JsonProperty("SymbolList")] public List<string> SymbolList { get; set; }

}