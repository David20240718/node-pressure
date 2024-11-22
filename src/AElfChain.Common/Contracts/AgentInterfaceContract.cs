using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts;

public enum InterfaceMethod
{
    Initialize,
    CreateToken
}

public class AgentInterfaceContract : BaseContract<InterfaceMethod>
{
    public AgentInterfaceContract(INodeManager nodeManager, string callAddress, string contractAddress) :
        base(nodeManager, contractAddress)
    {
        SetAccount(callAddress);
    }

    public AgentInterfaceContract(INodeManager nodeManager, string callAddress)
        : base(nodeManager, ContractFileName, callAddress)
    {
    }
    
    public static string ContractFileName => "AElf.Contracts.AgentInterface.dll";

}