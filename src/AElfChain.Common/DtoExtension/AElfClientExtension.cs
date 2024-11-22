using AElf.Client.Service;
using AElf.Client.Service;

namespace AElfChain.Common.DtoExtension
{
    public static class AElfClientExtension
    {
        /// <summary>
        ///     get AElf client instance
        /// </summary>
        /// <param name="baseUrl"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static AElfClient GetClient(string baseUrl, string? userName = null , string? password = null)
        {
            var endpoint = FormatServiceUrl(baseUrl);
            const int timeout = 120; 
            return new AElfClient(endpoint, timeout, userName, password);
        }

        private static string FormatServiceUrl(string baseUrl)
        {
            if (baseUrl.Contains("http://") || baseUrl.Contains("https://"))
                return baseUrl;

            return $"http://{baseUrl}";
        }
    }
}