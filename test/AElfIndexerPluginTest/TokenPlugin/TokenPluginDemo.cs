using AElfChain.Common.Helpers;
using AElfIndexer.Plugin;
using AElfIndexer.Plugin.Dto;
using AElfIndexer.Plugin.PluginQuery;
using GraphQL.Client.Http;
using log4net;

namespace AElfIndexerPluginTest;

[TestClass]
public class TokenPluginTestDemo
{
    private static readonly ILog Logger = Log4NetHelper.GetLogger();
    private GraphQLHttpClient? _graphQlClient;
    private TokenQuery _token;
    

    [TestInitialize]
    public void Initialize()
    {
        var url = "http://192.168.66.123:8071/AElfIndexer_Kimi/TokenContractPluginSchema/graphql";
        _graphQlClient = GraphQLClient.GetGraphQlHttpClient(url);
        _token = new TokenQuery(url);
        Log4NetHelper.LogInit("TokenPlugin");
    }

    [TestMethod]
    public async Task TestTokenInfoQuery()
    {
        var data = await _token.GetTokenInfo("AELF", "", 0, 100);
        foreach (var tokenInfo in data)
        {
            Logger.Info(tokenInfo.Symbol);
        }
    }
    
    [TestMethod]
    public async Task TestAccountInfoQuery()
    {
        var data = await _token.GetAccountInfo("AELF", "");
        foreach (var accountInfo in data)
        {
            Logger.Info(accountInfo.Address);
        }
    }
    
    [TestMethod]
    public async Task TestAccountTokenQuery()
    {
        var data = await _token.GetAccountToken("AELF", "",  "");
        foreach (var accountTokenDto in data)
        {
            Logger.Info(accountTokenDto.Address);
        }
    }
    
    [TestMethod]
    public async Task TestTransferInfoQuery()
    {
        var data = await _token.GetTransferInfo("AELF", "ELF",  "","", 0,100);
        foreach (var transferInfo in data)
        {
            Logger.Info(transferInfo.TransactionId);
        }
    }
}