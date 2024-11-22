using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;

namespace CreateReceiptScript;

public class Service
{
    public readonly INodeManager NodeManager;

    public Service(string url, string callAddress, string password)
    {
        NodeManager = new NodeManager(url);
        CallAddress = callAddress;
        CallAccount = callAddress.ConvertAddress();

        NodeManager.UnlockAccount(CallAddress, password);
        GetContractServices();
    }

    public GenesisContract GenesisService { get; set; } = null!;
    public TokenContract TokenService { get; set; } = null!;

    public string CallAddress { get; set; }
    public Address CallAccount { get; set; }

    private void GetContractServices()
    {
        GenesisService = GenesisContract.GetGenesisContract(NodeManager, CallAddress);

        //Token contract
        TokenService = GenesisService.GetTokenContract();
    }
}