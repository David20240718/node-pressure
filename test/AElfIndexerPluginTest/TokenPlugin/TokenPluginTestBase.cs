using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElfIndexer.Plugin;
using AElfIndexer.Plugin.PluginQuery;
using GraphQL.Client.Http;
using log4net;

namespace AElfIndexerPluginTest;

public class TokenPluginTestBase
{
    protected static readonly ILog Logger = Log4NetHelper.GetLogger();
    protected TokenQuery _token;

    protected INodeManager NodeManager { get; set; }
    protected List<INodeManager> SideNodeManagers { get; } = new();
    
    protected GenesisContract _genesisContract;
    protected TokenContract _tokenContract;
    protected AuthorityManager _authorityManager;

    protected List<GenesisContract> _sideGenesisContracts { get; } = new();
    protected List<TokenContract> _sideTokenContracts { get; } = new();
    protected List<AuthorityManager> _sideAuthorityManagers { get; } = new();


    protected List<CrossChainManager> _mainToSideManagers { get; } = new();
    protected List<CrossChainManager> _sideToMainManagers { get; } = new();

    protected string InitAccount { get; } = "23GxsoW9TRpLqX1Z5tjrmcRMMSn5bhtLAf4HtPj8JX9BerqTqp";
    // protected string InitAccount { get; } = "2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd";

    // private static string RpcUrl { get; } = "127.0.0.1:8000";
    private static string RpcUrl { get; } = "192.168.71.11:8000";

    
    //"192.168.67.153:8000",
    private static readonly List<string> SideRpcUrl = new() { "192.168.71.28:8000" };
    private bool isNeedSide = true;
    private GraphQLHttpClient? _graphQlClient;

    protected void Initialize()
    {
        Log4NetHelper.LogInit("TokenPlugin");
        //http://192.168.67.23:8070/AElfIndexer_Dapp_Shaw/TokenContractPluginSchema/ui/playground
        // var url = "http://192.168.66.123:8071/AElfIndexer_Kimi/TokenContractPluginSchema/graphql";
        // _graphQlClient = GraphQLClient.GetGraphQlHttpClient(url);
        // _token = new TokenQuery(url);

        NodeInfoHelper.SetConfig("pressure3-main");

        NodeManager = new NodeManager(RpcUrl);
        _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
        _tokenContract = _genesisContract.GetTokenContract(InitAccount);
        _authorityManager = new AuthorityManager(NodeManager, InitAccount);
        
        if (!isNeedSide) return;
        foreach (var s in SideRpcUrl)
        {
            var sideNodeManager = new NodeManager(s);
            var sideAuthorityManager = new AuthorityManager(sideNodeManager, InitAccount);
            var sideGenesisContract = GenesisContract.GetGenesisContract(sideNodeManager, InitAccount);
            var sideTokenContract = sideGenesisContract.GetTokenContract(InitAccount);
            var mainToSideManager = new CrossChainManager(NodeManager, sideNodeManager, InitAccount);
            var sideToMainManager = new CrossChainManager(sideNodeManager, NodeManager, InitAccount);

            SideNodeManagers.Add(sideNodeManager);
            _sideGenesisContracts.Add(sideGenesisContract);
            _sideTokenContracts.Add(sideTokenContract);
            _sideAuthorityManagers.Add(sideAuthorityManager);
            _mainToSideManagers.Add(mainToSideManager);
            _sideToMainManagers.Add(sideToMainManager);
        }
    }
}