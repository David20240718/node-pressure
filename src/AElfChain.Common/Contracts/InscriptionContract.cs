using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts;
public enum InscriptionMethod
{
    Initialize,
    DeployInscription,
    IssueInscription,
    Inscribe,
    
    GetInscribedLimit,
    GetDistributorList
}
public class InscriptionContract : BaseContract<InscriptionMethod>
{
    public InscriptionContract(INodeManager nodeManager, string callAddress, string contractAddress) :
        base(nodeManager, contractAddress)
    {
        SetAccount(callAddress);
    }

    public InscriptionContract(INodeManager nodeManager, string callAddress, string salt = "", bool isApprove = true)
        : base(nodeManager, ContractFileName, callAddress, salt, isApprove)
    {
    }
    public static string ContractFileName => "Forest.Inscription";
}