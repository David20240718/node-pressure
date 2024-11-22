using System;
using AElf.Client.Dto;
using AElf.Client.Proto;
using AElf.Contracts.MultiToken;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using Address = AElf.Types.Address;
using Hash = AElf.Types.Hash;

namespace AElfChain.Common.Contracts
{
    public enum TokenMethod
    {
        //Action
        Create,
        InitializeTokenContract,
        CreateNativeToken,
        Issue,
        IssueNativeToken,
        Transfer,
        CrossChainTransfer,
        CrossChainReceiveToken,
        Lock,
        Unlock,
        TransferFrom,
        Approve,
        UnApprove,
        BatchApprove,
        Burn,
        ChargeTransactionFees,
        ClaimTransactionFees,
        SetFeePoolAddress,
        RegisterCrossChainTokenContractAddress,
        CrossChainCreateToken,
        UpdateCoefficientFromContract,
        UpdateCoefficientFromSender,
        UpdateLinerAlgorithm,
        UpdatePowerAlgorithm,
        ChangeFeePieceKey,
        ValidateTokenInfoExists,
        AdvanceResourceToken,
        UpdateRental,
        UpdateRentedResources,
        ChangeTokenIssuer,
        SetSymbolsToPayTxSizeFee,
        SetMethodFee,
        SetMaxBatchApproveCount,
        SetSymbolAlias,
        //View
        GetTokenInfo,
        GetBalance,
        GetAllowance,
        GetAvailableAllowance,
        GetPrimaryTokenSymbol,
        IsInWhiteList,
        GetNativeTokenInfo,
        GetCrossChainTransferTokenContractAddress,
        GetMethodFee,
        GetOwningRental,
        GetLockedAmount,
        GetMethodFeeController,

        GetOwningRentalUnitValue,
        
        IsInCreateTokenWhiteList,
        GetVirtualAddressForLocking,
        GetMethodFeeFreeAllowancesConfig,
        GetMethodFeeFreeAllowances,

        SetTransactionFeeDelegations,
        GetTransactionFeeDelegationsOfADelegatee,
        RemoveTransactionFeeDelegator,
        RemoveTransactionFeeDelegatee,
        GetSymbolsToPayTxSizeFee,
        GetTransactionFeeFreeAllowancesConfig,
        GetCalculateFeeCoefficientsForSender,
        GetTokenAlias,
        GetSymbolByAlias,
        
        GetUserFeeController,
        UpdateCoefficientsForSender,
        SetTransactionFeeDelegateInfos,
        RemoveTransactionFeeDelegateeInfos,
        RemoveTransactionFeeDelegatorInfos,
        GetTransactionFeeDelegateeList,
        GetTransactionFeeDelegateInfosOfADelegatee,
        ConfigTransactionFeeFreeAllowances,
        GetTransactionFeeFreeAllowances,
        SetPrimaryTokenSymbol,
        GetTransactionFeeDelegatees,
        GetTransactionFeeDelegateInfo,
        GetMaxBatchApproveCount
    }

    public class TokenContract : BaseContract<TokenMethod>
    {
        public TokenContract(INodeManager nodeManager, string callAddress) :
            base(nodeManager, "AElf.Contracts.MultiToken", callAddress)
        {
            Logger = Log4NetHelper.GetLogger();
        }

        public TokenContract(INodeManager nodeManager, string callAddress, string contractAddress) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
            Logger = Log4NetHelper.GetLogger();
        }
        
        public TransactionResultDto CreateSEED0Token(string owner = "")
        {
            var symbol = "SEED-0";
            if (!GetTokenInfo(symbol).Equals(new TokenInfo())) 
                return new TransactionResultDto();
            var tokenOwner = owner == "" ? CallAddress : owner;
            var result = ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
            {
                Issuer = CallAccount,
                Owner = Address.FromBase58(tokenOwner),
                Symbol = symbol,
                Decimals = 0,
                IsBurnable = true,
                TokenName = $"{symbol} symbol",
                TotalSupply = 1,
                IssueChainId = 0,
                ExternalInfo = new ExternalInfo()
            });
            return result;
        }
        
        public TransactionResultDto CreateSEEDToken(string issuer, long total, string symbol, 
            bool isBurnable = true, int issueChainId = 0, string ownerSymbol = "", string expirationTime = "", int d = 0)
        {
            if (!GetTokenInfo(symbol).Equals(new TokenInfo())) 
                return new TransactionResultDto();
            var totalSupply = total;
            var externalInfo = new ExternalInfo();
            if (ownerSymbol != "" && expirationTime!="")
            {
                externalInfo = new ExternalInfo
                {
                    Value =
                    {
                        {
                            "__seed_owned_symbol", ownerSymbol
                        },
                        {
                            "__seed_exp_time", expirationTime
                        }
                    }
                };
            }
            var result = ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
            {
                Issuer = issuer.ConvertAddress(),
                Symbol = symbol,
                Decimals = d,
                IsBurnable = isBurnable,
                TokenName = $"{symbol} symbol",
                TotalSupply = totalSupply,
                IssueChainId = issueChainId,
                ExternalInfo = externalInfo
            });
            return result;
        }
        
                
        public TransactionResultDto CreateToken(string issuer, string owner, long total, string symbol, 
            int decimals, bool isBurnable = true, int issueChainId = 0, ExternalInfo externalInfo = null)
        {
            if (!GetTokenInfo(symbol).Equals(new TokenInfo())) 
                return new TransactionResultDto();
            var totalSupply = (long)Math.Pow(10, decimals) * total;
            var result = ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
            {
                Issuer = issuer.ConvertAddress(),
                Symbol = symbol,
                Decimals = decimals,
                IsBurnable = isBurnable,
                TokenName = $"{symbol} symbol",
                TotalSupply = totalSupply,
                IssueChainId = issueChainId,
                Owner = owner.ConvertAddress(),
                ExternalInfo = externalInfo
            });
            return result;
        }
        
        public TransactionResultDto CheckToken(string symbol, string issuer, string owner, int issueChainId = 0)
        {
            if (!GetTokenInfo(symbol).Equals(new TokenInfo())) return new TransactionResultDto();
            var item = 1;
            var seedSymbol = "SEED-" + $"{item}";
            while (!GetTokenInfo(seedSymbol).Equals(new TokenInfo()))
            {
                item++;
                seedSymbol = "SEED-" + $"{item}";
            }

            CreateSEEDToken(issuer, 1, seedSymbol, true, 0, 
                symbol, DateTime.UtcNow.Add(TimeSpan.FromDays(1)).ToTimestamp().Seconds.ToString());
            IssueBalance(issuer, issuer, 1, seedSymbol);
            ApproveToken(issuer, ContractAddress, 1, seedSymbol);
            var d = 0;
            if (!symbol.Contains("-"))
            {
                d = symbol.Equals("USDT") || symbol.Equals("USDC") ? 6 : 8;
            }
            var result = CreateToken(issuer, owner, 100000000, symbol, d, true, issueChainId);
            return result;
        }
        
        public TransactionResultDto CheckNFTCollectionToken(string symbol, string issuer, string owner, int issueChainId = 0, long total = 10000)
        {
            if (!GetTokenInfo(symbol).Equals(new TokenInfo())) return new TransactionResultDto();
            var item = 1;
            var seedSymbol = "SEED-" + $"{item}";
            while (!GetTokenInfo(seedSymbol).Equals(new TokenInfo()))
            {
                item++;
                seedSymbol = "SEED-" + $"{item}";
            }

            CreateSEEDToken(issuer, 1, seedSymbol, true, 0, 
                symbol, DateTime.UtcNow.Add(TimeSpan.FromDays(1)).ToTimestamp().Seconds.ToString());
            IssueBalance(issuer, issuer, 1, seedSymbol);
            ApproveToken(issuer, ContractAddress, 1, seedSymbol);
            var d = 0;
            var result = CreateToken(issuer, owner, total, symbol, d, true, issueChainId);
            return result;
        }
        
        public TransactionResultDto TransferBalance(string from, string to, long amount, string symbol = "")
        {
            // var tester = GetNewTester(from);
            SetAccount(from);
            var result = ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
            {
                Symbol = NodeOption.GetTokenSymbol(symbol),
                To = to.ConvertAddress(),
                Amount = amount
            });

            return result;
        }

        public TransactionResultDto IssueBalance(string from, string to, long amount, string symbol = "")
        {
            var tester = GetNewTester(from);
            tester.SetAccount(from);
            var result = tester.ExecuteMethodWithResult(TokenMethod.Issue, new IssueInput
            {
                Symbol = NodeOption.GetTokenSymbol(symbol),
                To = to.ConvertAddress(),
                Amount = amount,
                Memo = $"I-{Guid.NewGuid()}"
            });

            return result;
        }
        
        public TransactionResultDto Burn(string burner,  long amount, string symbol = "")
        {
            SetAccount(burner);
            var result = ExecuteMethodWithResult(TokenMethod.Burn, new BurnInput
            {
                Symbol = NodeOption.GetTokenSymbol(symbol),
                Amount = amount
            });

            return result;
        }


        public TransactionResultDto ApproveToken(string from, string to, long amount, string symbol = "")
        {
            SetAccount(from);
            var result = ExecuteMethodWithResult(TokenMethod.Approve, new ApproveInput
            {
                Symbol = NodeOption.GetTokenSymbol(symbol),
                Amount = amount,
                Spender = to.ConvertAddress()
            });

            return result;
        }
        
        public TransactionResultDto SetDelegation_old(string delegatee, string delegator, string symbol, long amount)
        {
            var delegations = new Dictionary<string, long>
            {
                [symbol] = amount
            };
            var input = new SetTransactionFeeDelegationsInput()
            {
                DelegatorAddress = delegator.ConvertAddress(),
                Delegations =
                {
                    delegations
                }
            };
            SetAccount(delegatee);
            var result = ExecuteMethodWithResult(TokenMethod.SetTransactionFeeDelegations, input);
            return result;
        }
        
        public TransactionResultDto SetDelegation_new(string delegatee, string delegator, string method, Address contract, string symbol, long amount)
        {
            var delegations = new Dictionary<string, long>
            {
                [symbol] = amount
            };
            var delegateInfo = new DelegateInfo
            {
                ContractAddress = contract,
                MethodName = method,
                Delegations =
                {
                    delegations
                },
                IsUnlimitedDelegate = false
            };
 
            SetAccount(delegatee);
            var executionResult = ExecuteMethodWithResult(TokenMethod.SetTransactionFeeDelegateInfos,
                new SetTransactionFeeDelegateInfosInput
                {
                    DelegatorAddress = delegator.ConvertAddress(),
                    DelegateInfoList = { delegateInfo }
                });
            return executionResult;
        }
        public TransactionResultDto CrossChainReceiveToken(string from, CrossChainReceiveTokenInput input)
        {
            var tester = GetNewTester(from);
            return tester.ExecuteMethodWithResult(TokenMethod.CrossChainReceiveToken, input);
        }

        public long GetUserBalance(string account, string symbol = "")
        {
            return CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
            {
                Owner = account.ConvertAddress(),
                Symbol = NodeOption.GetTokenSymbol(symbol)
            }).Balance;
        }

        public long GetLockedAmount(string account, Hash lockId, string symbol = "")
        {
            return CallViewMethod<GetLockedAmountOutput>(TokenMethod.GetLockedAmount, new GetLockedAmountInput
            {
                Address = account.ConvertAddress(),
                LockId = lockId,
                Symbol = NodeOption.GetTokenSymbol(symbol)
            }).Amount;
        }

        public long GetAllowance(string from, string to, string symbol = "")
        {
            return CallViewMethod<GetAllowanceOutput>(TokenMethod.GetAllowance,
                new GetAllowanceInput
                {
                    Owner = from.ConvertAddress(),
                    Spender = to.ConvertAddress(),
                    Symbol = NodeOption.GetTokenSymbol(symbol)
                }).Allowance;
        }
        //GetAvailableAllowance
        public long GetAvailableAllowance(string from, string to, string symbol = "")
        {
            return CallViewMethod<GetAllowanceOutput>(TokenMethod.GetAvailableAllowance,
                new GetAllowanceInput
                {
                    Owner = from.ConvertAddress(),
                    Spender = to.ConvertAddress(),
                    Symbol = NodeOption.GetTokenSymbol(symbol)
                }).Allowance;
        }

        public string GetPrimaryTokenSymbol()
        {
            return CallViewMethod<StringValue>(TokenMethod.GetPrimaryTokenSymbol, new Empty()).Value;
        }

        public string GetNativeTokenSymbol()
        {
            return CallViewMethod<TokenInfo>(TokenMethod.GetNativeTokenInfo, new Empty()).Symbol;
        }

        public TokenInfo GetTokenInfo(string symbol)
        {
            return CallViewMethod<TokenInfo>(TokenMethod.GetTokenInfo, new GetTokenInfoInput
            {
                Symbol = symbol
            });
        }

        public OwningRental GetOwningRental()
        {
            return CallViewMethod<OwningRental>(TokenMethod.GetOwningRental, new Empty());
        }

        public bool IsInCreateTokenWhiteList(string contract)
        {
            return CallViewMethod<BoolValue>(TokenMethod.IsInCreateTokenWhiteList, contract.ConvertAddress()).Value;
        }

        public Address GetVirtualAddressForLocking(Address address, Hash lockId)
        {
            return CallViewMethod<Address>(TokenMethod.GetVirtualAddressForLocking,
                new GetVirtualAddressForLockingInput
                {
                    Address = address,
                    LockId = lockId
                });
        }

        public MethodFeeFreeAllowancesConfig GetMethodFeeFreeAllowancesConfig()
        {
            return CallViewMethod<MethodFeeFreeAllowancesConfig>(TokenMethod.GetMethodFeeFreeAllowancesConfig,
                new Empty());
        }

        public GetTransactionFeeFreeAllowancesConfigOutput GetTransactionFeeFreeAllowancesConfig()
        {
            return CallViewMethod<GetTransactionFeeFreeAllowancesConfigOutput>(TokenMethod.GetTransactionFeeFreeAllowancesConfig,
                new Empty());
        }

        public MethodFeeFreeAllowances GetMethodFeeFreeAllowances(string address)
        {
            return CallViewMethod<MethodFeeFreeAllowances>(TokenMethod.GetMethodFeeFreeAllowances,
                address.ConvertAddress());
        }

        public TransactionFeeFreeAllowancesMap GetTransactionFeeFreeAllowances(string address)
        {
            return CallViewMethod<TransactionFeeFreeAllowancesMap>(TokenMethod.GetTransactionFeeFreeAllowances,
                address.ConvertAddress());
        }

        public TransactionFeeDelegations GetTransactionFeeDelegationsOfADelegatee(string delegator, string delegatee)
        {
            return CallViewMethod<TransactionFeeDelegations>(TokenMethod.GetTransactionFeeDelegationsOfADelegatee,
                new GetTransactionFeeDelegationsOfADelegateeInput
                {
                    DelegatorAddress = delegator.ConvertAddress(),
                    DelegateeAddress = delegatee.ConvertAddress()
                });
        }
        
        public GetTransactionFeeDelegateesOutput GetTransactionFeeDelegatees(string delegator)
        {
            return CallViewMethod<GetTransactionFeeDelegateesOutput>(TokenMethod.GetTransactionFeeDelegatees,
                new GetTransactionFeeDelegateesInput()
                {
                    DelegatorAddress = delegator.ConvertAddress()
                });;
        }


        public  TransactionFeeDelegations GetTransactionFeeDelegateInfo(Address contract, string delegator, string delegatee, string method)
        {
            return CallViewMethod<TransactionFeeDelegations>(TokenMethod.GetTransactionFeeDelegateInfo,
                new GetTransactionFeeDelegateInfoInput
                {
                    ContractAddress = contract,
                    DelegatorAddress = delegator.ConvertAddress(),
                    DelegateeAddress = delegatee.ConvertAddress(),
                    MethodName = method
                });
        }

        public OwningRentalUnitValue GetOwningRentalUnitValue()
        {
            return CallViewMethod<OwningRentalUnitValue>(TokenMethod.GetOwningRentalUnitValue, new Empty());
        }
        
        public SymbolListToPayTxSizeFee QueryAvailableTokenInfos()
        {
            var tokenInfos = CallViewMethod<SymbolListToPayTxSizeFee>(TokenMethod.GetSymbolsToPayTxSizeFee,new Empty());
            if (tokenInfos.Equals(new SymbolListToPayTxSizeFee()))
            {
                Logger.Info("GetAvailableTokenInfos: Null");
                return null;
            }

            foreach (var info in tokenInfos.SymbolsToPayTxSizeFee)
                Logger.Info(
                    $"Symbol: {info.TokenSymbol}, TokenWeight: {info.AddedTokenWeight}, BaseWeight: {info.BaseTokenWeight}");

            return tokenInfos;
        }

        public CalculateFeeCoefficients GetCalculateFeeCoefficientsForSender()
        {
          return CallViewMethod<CalculateFeeCoefficients>(TokenMethod.GetCalculateFeeCoefficientsForSender, new Empty());
        }

        public UserFeeController GetUserFeeController()
        {
            return CallViewMethod<UserFeeController>(TokenMethod.GetUserFeeController, new Empty());
        }

        public string GetTokenAlias(string aliasOrSymbol)
        {
            return CallViewMethod<StringValue>(TokenMethod.GetTokenAlias, new StringValue{Value = aliasOrSymbol}).Value;
        }
        
        //GetSymbolByAlias
        public string GetSymbolByAlias(string aliasOrSymbol)
        {
            return CallViewMethod<StringValue>(TokenMethod.GetSymbolByAlias, new StringValue{Value = aliasOrSymbol}).Value;
        }
    }
}