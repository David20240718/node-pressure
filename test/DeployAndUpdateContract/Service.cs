using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;

namespace DeployAndUpdateContract;

public class Service
{
    public readonly INodeManager _nodeManager;

    public Service(string url, string callAddress, string password)
    {
        _nodeManager = new NodeManager(url);
        CallAddress = callAddress;
        CallAccount = callAddress.ConvertAddress();

        _nodeManager.UnlockAccount(CallAddress, password);
        GetContractServices();
    }

    public GenesisContract GenesisService { get; set; } = null!;
    public TokenContract TokenService { get; set; } = null!;
    public ConsensusContract ConsensusService { get; set; } = null!;
    public ParliamentContract ParliamentService { get; set; } = null!;

    public string CallAddress { get; set; }
    public Address CallAccount { get; set; }

    private void GetContractServices()
    {
        GenesisService = GenesisContract.GetGenesisContract(_nodeManager, CallAddress);

        //Token contract
        TokenService = GenesisService.GetTokenContract();

        //Consensus contract
        ConsensusService = GenesisService.GetConsensusContract();

        //Parliament contract
        ParliamentService = GenesisService.GetParliamentContract();
    }
}