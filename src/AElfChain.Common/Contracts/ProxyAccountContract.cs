using AElf.Client.Dto;
using AElf.Contracts.ProxyAccountContract;
using AElf.Types;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Contracts;

public enum ProxyMethod
{
    Initialize,
    SetProxyAccountContracts,
    SetAdmin,
    SetMaxManagementAddressCount,
    Create,
    AddManagementAddress,
    RemoveManagementAddress,
    ResetManagementAddress,
    ForwardCall,
    ValidateProxyAccountExists,
    CrossChainSyncProxyAccount,

    GetAdmin,
    GetMaxManagementAddressCount,
    GetCurrentVirtualHash,
    GetProxyAccountAddress,
    GetProxyAccountByHash,
    GetProxyAccountByProxyAccountAddress,
    GetCurrentCounter,
    GetVirtualHashByCounter
}

public class ProxyAccountContract : BaseContract<ProxyMethod>
{
    public ProxyAccountContract(INodeManager nodeManager, string callAddress, string contractAddress) :
        base(nodeManager, contractAddress)
    {
        SetAccount(callAddress);
    }

    public ProxyAccountContract(INodeManager nodeManager, string callAddress, string salt = "", bool isApprove = true)
        : base(nodeManager, ContractFileName, callAddress, salt, isApprove)
    {
    }
    public static string ContractFileName => "AElf.Contracts.ProxyAccountContract";

    public Address GetAdmin()
    {
        return CallViewMethod<Address>(ProxyMethod.GetAdmin, new Empty());
    }

    public Address GetProxyAccountAddress(int chainId, Hash virtualHash)
    {
        return  CallViewMethod<Address>(ProxyMethod.GetProxyAccountAddress, new GetProxyAccountAddressInput
        {
            ChainId = chainId,
            ProxyAccountHash = virtualHash
        });
    }

    public ProxyAccount GetProxyAccountByHash(Hash virtualHash)
    {
        return  CallViewMethod<ProxyAccount>(ProxyMethod.GetProxyAccountByHash, virtualHash);
    }
    
    public ProxyAccount GetProxyAccountByProxyAccountAddress(Address virtualAddress)
    {
        return  CallViewMethod<ProxyAccount>(ProxyMethod.GetProxyAccountByProxyAccountAddress, virtualAddress);
    }
    
    public Int32Value GetMaxManagementAddressCount()
    {
        return  CallViewMethod<Int32Value>(ProxyMethod.GetMaxManagementAddressCount, new Empty());
    }

    public TransactionResultDto ForwardCall(IMessage input, Address contractAddress, string method, ProxyAccount proxyAccountInfo, int managerIndex)
    {
        var inputForwardCall = new ForwardCallInput
        {
            ContractAddress = contractAddress,
            MethodName = method,
            ProxyAccountHash = proxyAccountInfo.ProxyAccountHash,
            Args = input.ToByteString()
        };
        SetAccount(proxyAccountInfo.ManagementAddresses[managerIndex].Address.ToBase58());
        return ExecuteMethodWithResult(ProxyMethod.ForwardCall, inputForwardCall);
    }

    public Int64Value GetCurrentCounter()
    {
        return  CallViewMethod<Int64Value>(ProxyMethod.GetCurrentCounter, new Empty());
    }

    public Hash GetVirtualHashByCounter(long counter)
    {
        return CallViewMethod<Hash>(ProxyMethod.GetVirtualHashByCounter, new Int64Value{ Value = counter });
    }
}