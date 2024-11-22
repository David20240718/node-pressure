using AElfChain.Common.Helpers;
using Newtonsoft.Json;

namespace BeangoTownScript;

public class ConfigInfo
{
    [JsonProperty("TransactionCount")] public int TransactionCount { get; set; }
    [JsonProperty("UserCount")] public int UserCount { get; set; }
    [JsonProperty("ServiceUrl")] public string ServiceUrl { get; set; }
    [JsonProperty("ConfigFile")] public string ConfigFile { get; set; }
    [JsonProperty("main_chain_url")] public string mainChain_url { get; set; }
    [JsonProperty("side_chain_url")] public string sideChain_url { get; set; }
    [JsonProperty("BeangoTownServerUrl")] public string beangoTownServerUrl { get; set; }
    [JsonProperty("BeangoTownAddress")] public string BeangoTownAddress { get; set; }
    [JsonProperty("CaAddressMain")] public string CaAddressMain { get; set; }
    [JsonProperty("CaAddressSide")] public string CaAddressSide { get; set; }
    [JsonProperty("CreatorController")] public string creatorController { get; set; }
    [JsonProperty("privateKey")] public string privateKey { get; set; }

    public static ConfigInfo ReadInformation => ConfigHelper<ConfigInfo>.GetConfigInfo("config.json", false);
}

public class ChainTypeOption
{
    [JsonProperty("is_side_chain")] public bool IsSideChain { get; set; }
    [JsonProperty("main_chain_url")] public string MainChainUrl { get; set; }
}
