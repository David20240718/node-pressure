using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Contracts
{
    public enum ConfigurationMethod
    {
        SetConfiguration,
        GetConfiguration,
        GetOwnerAddress,
        ChangeConfigurationController,
        GetConfigurationController,
        ChangeMethodFeeController,
        GetMethodFeeController
    }

    public enum ConfigurationNameProvider
    {
        BlockTransactionLimit,
        RequiredAcsInContracts,
        StateSizeLimit,
        ExecutionObserverThreshold,
        UserContractMethodFee,
        SecretSharingEnabled
    }

    public class ConfigurationContract : BaseContract<ConfigurationMethod>
    {
        public ConfigurationContract(INodeManager nodeManager, string callAddress, string contractAddress)
            : base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }

        public BytesValue GetBlockTransactionLimit()
        {
            return CallViewMethod<BytesValue>(ConfigurationMethod.GetConfiguration, new StringValue
                {Value = nameof(ConfigurationNameProvider.BlockTransactionLimit)});
        }
    }
}