using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;

namespace AElfTestContractScript;

public class Service
{
    public readonly INodeManager NodeManager;
    public readonly AuthorityManager AuthorityManager;

    public Service(string url, string callAddress, string password)
    {
        NodeManager = new NodeManager(url);
        AuthorityManager = new AuthorityManager(NodeManager, callAddress);
        CallAddress = callAddress;
        CallAccount = callAddress.ConvertAddress();

        NodeManager.UnlockAccount(CallAddress, password);
        GetContractServices();
    }

    private GenesisContract GenesisService { get; set; } = null!;
    public TokenContract TokenService { get; set; } = null!;
    public ParliamentContract ParliamentService { get; set; } = null!;
    public ConfigurationContract ConfigurationService { get; set; } = null!;


    public string CallAddress { get; set; }
    public Address CallAccount { get; set; }

    private void GetContractServices()
    {
        GenesisService = GenesisContract.GetGenesisContract(NodeManager, CallAddress);

        //Token contract
        TokenService = GenesisService.GetTokenContract();
        ParliamentService = GenesisService.GetParliamentContract();
        ConfigurationService = GenesisService.GetConfigurationContract();
    }
}