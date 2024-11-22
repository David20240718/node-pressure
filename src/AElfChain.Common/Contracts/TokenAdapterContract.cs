using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
{
    public enum TokenAdapterContractAddressMethod
    {
        //action
        CreateToken
    }

    public class TokenAdapterContract : BaseContract<TokenAdapterContractAddressMethod>
    {
        public TokenAdapterContract(INodeManager nodeManager, string callAddress) :
            base(nodeManager, ContractFileName, callAddress)
        {
        }

        public TokenAdapterContract(INodeManager nodeManager, string callAddress, string contractAddress) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }

        public static string ContractFileName => "";
    }
}