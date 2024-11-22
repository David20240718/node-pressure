using AElf;
using AElf.Types;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using Portkey.Contracts.CA;

namespace AElfChain.Common.Contracts;

public enum CAMethod
{
    // Action
    Initialize,
    ManagerForwardCall,
    ManagerTransfer,
    ManagerTransferFrom,
    AddVerifierServerEndPoints,
    CreateCAHolder,
    SetTransferLimit,
    SetDefaultTokenTransferLimit,
    ManagerApprove,
    ManagerUnApprove,
    SetForbiddenForwardCallContractMethod,
    ChangeManagerApproveForbiddenEnabled,
    SetCAContractAddresses,
    AddCAServer,
    AddCreatorController,
    RemoveVerifierServerEndPoints,

    // View
    GetHolderInfo,
    GetVerifierServers,
    GetContractDelegationFee,
    GetCreatorControllers,
    GetTransferLimit,
    GetDefaultTokenTransferLimit,
    GetAdmin
}

public class CAContract : BaseContract<CAMethod>
{
    public CAContract(INodeManager nodeManager, string callAddress, string salt = "", bool isApprove = false) : base(nodeManager, "Portkey.Contracts.CA",
        callAddress, salt, isApprove)
    {
    }

    public CAContract(INodeManager nodeManager, string callAddress, string contractAddress) : base(nodeManager,
        contractAddress)
    {
        SetAccount(callAddress);
    }

    public GetHolderInfoOutput GetHolderInfo(Hash hash)
    {
        return CallViewMethod<GetHolderInfoOutput>(CAMethod.GetHolderInfo,
            new GetHolderInfoInput
            {
                LoginGuardianIdentifierHash = hash
            });
    }

    public GetHolderInfoOutput GetHolderInfoByGuardianType(string loginGuardianAccount)
    {
        return CallViewMethod<GetHolderInfoOutput>(CAMethod.GetHolderInfo,
            new GetHolderInfoInput
            {
                // CaHash = HashHelper.ComputeFrom(cahash),
                // LoginGuardianIdentifierHash = loginGuardianAccount
            });
    }

    public GetVerifierServersOutput GetVerifierServers()
    {
        return CallViewMethod<GetVerifierServersOutput>(CAMethod.GetVerifierServers, new Empty());
    }

    public GetContractDelegationFeeOutput GetContractDelegationFee()
    {
        return CallViewMethod<GetContractDelegationFeeOutput>(CAMethod.GetContractDelegationFee, new Empty());
    }

    public ControllerOutput GetCreatorControllers()
    {
        return CallViewMethod<ControllerOutput>(CAMethod.GetCreatorControllers, new Empty());
    }
}