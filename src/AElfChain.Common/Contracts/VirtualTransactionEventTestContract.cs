using AElfChain.Common.Managers;
using JetBrains.Annotations;

namespace AElfChain.Common.Contracts;

public enum EventTestContractMethod
{
    FireVirtualTransactionEventTest,
    SendVirtualTransactionWithOutEvent
}

public class VirtualTransactionEventTestContract : BaseContract<EventTestContractMethod>
{
    public VirtualTransactionEventTestContract([NotNull] INodeManager nodeManager, [NotNull] string callAddress) 
        : base(nodeManager, ContractFileName, callAddress)
    {
    }

    public VirtualTransactionEventTestContract([NotNull] INodeManager nodeManager, string callAddress, [NotNull] string contractAddress)
        : base(nodeManager, contractAddress)
    {
        SetAccount(callAddress);
    }
    public static string ContractFileName => "AElf.Contracts.TestContract.VirtualTransactionEvent.dll";

}

