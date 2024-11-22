using AElf.Types;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using JetBrains.Annotations;

namespace AElfChain.Common.Contracts;

public enum TimeLockMethod
{
    Initialize,
    SetDelay,
    ChangeAdmin,
    QueueTransaction,
    ExecuteTransaction,
    CancelTransaction,
    
    GetDelay,
    GetAdmin,
    GetTransaction
}

public class TimeLockContract : BaseContract<TimeLockMethod>
{
    public TimeLockContract(INodeManager nodeManager, string callAddress) : base(nodeManager,
        ContractFileName, callAddress)
    {
    }

    public TimeLockContract(INodeManager nodeManager, string callAddress, string contractAddress) :
        base(nodeManager, contractAddress)
    {
        SetAccount(callAddress);
    }

    public ulong GetDelay()
    {
         var delay = CallViewMethod<UInt64Value>(TimeLockMethod.GetDelay, new Empty());
         return delay.Value;
    }
    
    public Address GetAdmin()
    {
        return CallViewMethod<Address>(TimeLockMethod.GetAdmin, new Empty());
    }
    
    public bool GetTransaction(Hash tx)
    {
        var transaction = CallViewMethod<BoolValue>(TimeLockMethod.GetTransaction, tx);
        return transaction.Value;
    }

    private static string ContractFileName => "AElf.Contracts.TimeLockContract.dll";
}