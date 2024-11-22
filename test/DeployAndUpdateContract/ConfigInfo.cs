using Newtonsoft.Json;

namespace DeployAndUpdateContract
{
    public class ConfigInfo
    {
        [JsonProperty("Environment")] public string Environment { get; set; }
        [JsonProperty("Type")] public string Type { get; set; }
        [JsonProperty("UpdateInfo")] public UpdateInfo UpdateInfo { get; set; }
        [JsonProperty("isApproval")] public bool isApproval { get; set; }
        [JsonProperty("ContractFileName")] public string ContractFileName { get; set; }
        [JsonProperty("Salt")] public string Salt { get; set; }
        [JsonProperty("AuthorInfo")] public AuthorInfo AuthorInfo { get; set; }

    }

    public class UpdateInfo
    {
        [JsonProperty("isSystemContract")] public bool isSystemContract { get; set; }
        [JsonProperty("ContractName")] public string ContractName { get; set; }
        [JsonProperty("ContractAddress")] public string ContractAddress { get; set; }
    }

    public class AuthorInfo
    {
        [JsonProperty("isProxyAddress")] public bool isProxyAddress { get; set; }
        [JsonProperty("Author")] public string Author { get; set; }
        [JsonProperty("Signer")] public string Signer { get; set; }
    }

    public static class ConfigHelper
    {
        private static ConfigInfo? _instance;
        private static string? _jsonContent;
        private static readonly object LockObj = new object();

        public static ConfigInfo? Config => GetConfigInfo();

        private static ConfigInfo? GetConfigInfo()
        {
            lock (LockObj)
            {
                if (_instance != null) return _instance;

                var configFile = Path.Combine(Directory.GetCurrentDirectory(), "config.json");
                _jsonContent = File.ReadAllText(configFile);
                _instance = JsonConvert.DeserializeObject<ConfigInfo>(_jsonContent);
            }

            return _instance;
        }
    }
}