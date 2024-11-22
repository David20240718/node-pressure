using System.Linq;
using AElf.Contracts.Configuration;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Shouldly;

namespace PortkeyCryptoBoxContractScript;

public class PrepareService 
{
    public PrepareService(Service service, ILog logger)
    {
        _service = service;
        _logger = logger;
        _nodeManager = service.NodeManager;
    }

    public void SetFreeAllowance()
    {
        _logger.Info($"Set Transaction Fee Free Allowances:" +
                     $"\nThreshold Symbol: {Constants.ThresholdSymbol}" +
                     $"\nThreshold : {Constants.FreeAllowanceThreshold}" +
                     $"\nFreeSymbol: {Constants.FreeSymbol}" +
                     $"\nFreeAmount: {Constants.FreeAmount}");
        var getFreeAllowancesConfig = _service.TokenService.GetTransactionFeeFreeAllowancesConfig();
        if (getFreeAllowancesConfig.Value.Any())
            return;
        var organization = _service.ParliamentService.GetGenesisOwnerAddress();
        var input = new ConfigTransactionFeeFreeAllowancesInput
        {
            Value =
            {
                new ConfigTransactionFeeFreeAllowance
                {
                    Symbol = Constants.ThresholdSymbol,
                    TransactionFeeFreeAllowances = new TransactionFeeFreeAllowances
                    {
                        Value =
                        {
                            new TransactionFeeFreeAllowance
                            {
                                Amount = Constants.FreeAmount,
                                Symbol = Constants.FreeSymbol
                            }
                        }
                    },
                    RefreshSeconds = Constants.RefreshSeconds,
                    Threshold = Constants.FreeAllowanceThreshold,
                }
            }
        };
 
        var result = _service.AuthorityManager.ExecuteTransactionWithAuthority(_service.TokenService.ContractAddress,
            "ConfigTransactionFeeFreeAllowances", input,
            _service.CallAddress, organization);
        result.Status.ShouldBe(TransactionResultStatus.Mined);
    }

    public void SetTransactionLimit(int count)
    {
        var txLimit = _service.ConfigurationService.GetBlockTransactionLimit();
        var value = Int32Value.Parser.ParseFrom(txLimit.Value).Value;
        if (value.Equals(count))
        {
            return;
        }
        var organization = _service.ParliamentService.GetGenesisOwnerAddress();
        var input = new SetConfigurationInput
        {
            Key = nameof(ConfigurationNameProvider.BlockTransactionLimit),
            Value = new Int32Value { Value = count }.ToByteString()
        };
        var result = _service.AuthorityManager.ExecuteTransactionWithAuthority(_service.ConfigurationService.ContractAddress,
            nameof(ConfigurationMethod.SetConfiguration), input,
            _service.CallAddress, organization);
        result.Status.ShouldBe(TransactionResultStatus.Mined);
        txLimit = _service.ConfigurationService.GetBlockTransactionLimit();
        value = Int32Value.Parser.ParseFrom(txLimit.Value).Value;
        _logger.Info($"Set Transaction Limit: {value}");
    }

    public void SetSecondaryDelegate(Dictionary<string, string> eoaAccountList)
    {
        foreach (var eoa in eoaAccountList)
        {
            var delegator = eoa.Key;
            var delegatee = eoa.Value;
            var delegations = new Dictionary<string, long>
            {
                [Constants.FreeSymbol] = Constants.FreeAmount
            };
            var input = new SetTransactionFeeDelegationsInput()
            {
                DelegatorAddress = delegator.ConvertAddress(),
                Delegations =
                {
                    delegations
                }
            };
            _service.TokenService.SetAccount(delegatee);
            var result =  _service.TokenService.ExecuteMethodWithResult(TokenMethod.SetTransactionFeeDelegations, input);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var originDelegations = _service.TokenService.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
            _logger.Info(originDelegations);
        }
    }
    
    public void SetSecondaryDelegate(List<string> accountList , string delegatee)
    {
        var txIds = new List<string>();
        foreach (var account in accountList)
        {
            var delegations = new Dictionary<string, long>
            {
                [Constants.FreeSymbol] = Constants.FreeAmount
            };
            var input = new SetTransactionFeeDelegationsInput()
            {
                DelegatorAddress = Address.FromBase58(account),
                Delegations =
                {
                    delegations
                }
            };
            
            _service.TokenService.SetAccount(delegatee);
            var txId = _service.TokenService.ExecuteMethodWithTxId(TokenMethod.SetTransactionFeeDelegations, input);
            txIds.Add(txId);
        }

        _nodeManager.CheckTransactionListResult(txIds);
    }

    private readonly Service _service;
    private readonly ILog _logger;
    private readonly INodeManager _nodeManager;
}


