using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts;

public enum BeangoTownMethod
{
    // action method
    Initialize,
    Play,
    Bingo,
    
    // view method
    GetBoutInformation,
    GetPlayerInformation
}

public class BeangoTownContract : BaseContract<BeangoTownMethod>
{
    public BeangoTownContract(INodeManager nodeManager, string callAddress, string contractAddress) :
        base(nodeManager, contractAddress)
    {
        SetAccount(callAddress);
    }

    public BeangoTownContract(INodeManager nodeManager, string callAddress, string salt = "", bool isApprove = true)
        : base(nodeManager, ContractFileName, callAddress, salt, isApprove)
    {
    }

    private static string ContractFileName = "Contracts.BeangoTownContract";
}