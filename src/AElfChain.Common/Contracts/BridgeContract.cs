using AElfChain.Common.Managers;
using JetBrains.Annotations;

namespace AElfChain.Common.Contracts;

public enum BridgeMethod
{
    CreateReceipt
}

public class BridgeContract : BaseContract<BridgeMethod>
{
    public BridgeContract(INodeManager nodeManager, string callAddress, string contractAddress) :
        base(nodeManager, contractAddress)
    {
        SetAccount(callAddress);
    }

    public BridgeContract(INodeManager nodeManager, string callAddress)
        : base(nodeManager, ContractFileName, callAddress)
    {
    }

    private static string ContractFileName = "EBridge.Contracts.Bridge";
}