using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf;
using AElf.Standards.ACS1;
using AElf.Standards.ACS3;
using AElf.Contracts.Association;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.Contracts.TestContract.TransactionFees;
using AElf.Contracts.TokenConverter;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Shouldly;
using Volo.Abp.Threading;
using TokenInfo = AElf.Contracts.MultiToken.TokenInfo;
using TransferInput = AElf.Contracts.MultiToken.TransferInput;

namespace SystemContractTest
{
    [TestClass]
    public class SecondaryDelegateTest
    {
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private INodeManager SideNodeManager { get; set; }
        private AuthorityManager AuthorityManager { get; set; }
        private AuthorityManager SideAuthority { get; set; }
        private ContractManager ContractManager { get; set; }
        private ContractManager SideContractManager { get; set; }

        private TokenContract _tokenContract;
        private TokenConverterContract _tokenConverter;
        private ParliamentContract _parliament;
        private GenesisContract _genesisContract;
        private TreasuryContract _treasury;
        private ProfitContract _profit;

        private TransactionFeesContract _acs8ContractA;
        private TransactionFeesContract _acs8ContractB;
        private TransactionFeesContractContainer.TransactionFeesContractStub _acs8SubA;
        private TransactionFeesContractContainer.TransactionFeesContractStub _acs8SubB;
        private TokenConverterContractContainer.TokenConverterContractStub _tokenConverterSub;
        private TokenContractImplContainer.TokenContractImplStub _tokenContractImpl;
        private TokenContractImplContainer.TokenContractImplStub _sideTokenContractImpl;

        //aFm1FWZRLt7V6wCBUGVmqxaDcJGv9HvYPDUVxF95C9L7sTwXp
        //NUddzDNy8PBMUgPCAcFW7jkaGmofDTEmr5DUoddXDpdR6E85X
        private Dictionary<SchemeType, Scheme> Schemes { get; set; }
        private string InitAccount { get; } = "Du9KDALrVrboq51NHDaFgrfPWUJbur2PMCjVgGt7iw2qdMuBk";
        private string transferAccount { get; } = "zkWrJiNT8B4af6auBzn3WuhNrd3zHtmercyQ4sar7GxM8Xwy9";
        private string chargeAccount { get; } = "NCt7dRM9m7vFfbXcrhhN1cLZL8R4Yveur2uS8C7T8Pim2Umhk";
        private string Delegatee1 { get; } = "2gbaA3AGouPusXDjKgRXforaS7ayFMpXVYi2xZGhZhdFdbCoJ8";
        private string Delegatee2 { get; } = "396FeK4RAhqb82yBbkkUhY4VYwubRAFmpjUPRHMUmcNtokMLX";
        // private string Delegatee3 { get; } = "2bs2uYMECtHWjB57RqgqQ3X2LrxgptWHtzCqGEU11y45aWimh4";
        // private string Delegatee4 { get; } = "2HxX36oXZS89Jvz7kCeUyuWWDXLTiNRkAzfx3EuXq4KSSkH62W";
        //
        // private string TestAccount { get; } = "zptx91dhHVJjJRxf5Wg5KAoMrDrWX6i1H2FAyKAiv2q8VZfbg";

        private string Delegator1 { get; } = "2Hg4ZfAz1ktiw5yqsevvQ7Wr14e2rottW4GG91DmoYaeWqLf5z";
        // private string Delegator2 { get; } = "aFm1FWZRLt7V6wCBUGVmqxaDcJGv9HvYPDUVxF95C9L7sTwXp";
        // private string Delegator3 { get; } = "2pmw7ZpB8yxL4ifKU8uH233b6bwE3wnhGb6BXxvFWSuiFd7v1G";
        // private string Delegator4 { get; } = "2EyLTpDMvfkcBga6EavVZ5mcbCiWR3PtxSPpFrnWWxJ4SwEeAY";

        // private static string RpcUrl { get; } = "";
        private static string RpcUrl { get; } = "https://aelf-public-node.aelf.io";
        private static string SideRpcUrl { get; } = "";

        private string Symbol { get; } = "ELF";
        private long SymbolFee = 13_00000000;
        private bool isNeedSide = false;
        private List<string> _accountList = new List<string>();
        private List<string> _resourceSymbol = new List<string>
            { "READ", "WRITE", "STORAGE", "TRAFFIC" };
        
        private const string BasicFeeSymbol = "BASIC";
        private const string SizeFeeSymbol = "SIZE";
        private const string NativeToken = "ELF";
        private const string USDTSymbol = "USDT";

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("SecondaryDelegateTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes");

            if (isNeedSide)
            {
                SideNodeManager = new NodeManager(SideRpcUrl);
                SideContractManager = new ContractManager(SideNodeManager, InitAccount);
                SideAuthority = new AuthorityManager(SideNodeManager, InitAccount);
                _sideTokenContractImpl = SideContractManager.TokenImplStub;
                _acs8ContractB = new TransactionFeesContract(NodeManager, InitAccount);
            }

            NodeManager = new NodeManager(RpcUrl);
            ContractManager = new ContractManager(NodeManager, InitAccount);
            AuthorityManager = new AuthorityManager(NodeManager, InitAccount);

            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
            _tokenConverter = _genesisContract.GetTokenConverterContract(InitAccount);
            _parliament = _genesisContract.GetParliamentContract(InitAccount);
            _tokenContractImpl = _genesisContract.GetTokenImplStub();

            _treasury = _genesisContract.GetTreasuryContract(InitAccount);
            _profit = _genesisContract.GetProfitContract(InitAccount);
            _profit.GetTreasurySchemes(_treasury.ContractAddress);
            Schemes = ProfitContract.Schemes;

            _tokenConverterSub = _genesisContract.GetTokenConverterStub(InitAccount);
            //_acs8ContractA = new TransactionFeesContract(SideNodeManager, InitAccount);
            //_sideTokenContractImpl = SideContractManager.TokenImplStub;
//            _acs8ContractB = new TransactionFeesContract(NodeManager, InitAccount,
//                "Xg6cJsRnCuznxHC1JAyB8XSmxfDnCKTQeJN9fP4ca938MBYgU");
//           TransferFewResource();
            //InitializeFeesContract(_acs8ContractA);
//           InitializeFeesContract(_acs8ContractB);
            //_acs8SubA =
                //_acs8ContractA.GetTestStub<TransactionFeesContractContainer.TransactionFeesContractStub>(InitAccount);
//            _acs8SubB =
//                _acs8ContractB.GetTestStub<TransactionFeesContractContainer.TransactionFeesContractStub>(InitAccount);
            /*CreateAndIssueToken(100000_00000000, Symbol);
            CreateAndIssueToken(100000_00000000, "ABC");*/
            // CreateAndIssueToken(100000_00000000, "DDD");
            // CreateAndIssueToken(100000_00000000, USDTSymbol);
            
            // if (_tokenContract.GetTokenInfo(BasicFeeSymbol).Equals(new TokenInfo()))
            // {
            //     // CreateToken(BasicFeeSymbol, 8, InitAccount.ConvertAddress(), 100000000_00000000);
            //     IssueBalance(BasicFeeSymbol, 100000000_00000000, InitAccount.ConvertAddress());
            // }
            // if (_tokenContract.GetTokenInfo(SizeFeeSymbol).Equals(new TokenInfo()))
            // {
            //     // CreateToken(SizeFeeSymbol, 8, InitAccount.ConvertAddress(), 100000000_00000000);
            //     IssueBalance(SizeFeeSymbol, 100000000_00000000, InitAccount.ConvertAddress());
            // }

        }
        
        private void CreateAndIssueToken(long amount, string symbol)
        {
            if (!_tokenContract.GetTokenInfo(symbol).Equals(new TokenInfo())) return;

            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
            {
                Issuer = InitAccount.ConvertAddress(),
                Symbol = symbol,
                Decimals = 8,
                IsBurnable = true,
                TokenName = $"{symbol} symbol",
                TotalSupply = 100000000_00000000
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var balance = _tokenContract.GetUserBalance(InitAccount, symbol);
            var issueResult = _tokenContract.IssueBalance(InitAccount, InitAccount, amount, symbol);
            issueResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var afterBalance = _tokenContract.GetUserBalance(InitAccount, symbol);
            afterBalance.ShouldBe(balance + amount);
        }

        #region Set Delegator

        //SetTransactionFeeDelegations
        //GetTransactionFeeDelegationsOfADelegatee
        //RemoveTransactionFeeDelegator
        //RemoveTransactionFeeDelegatee

        public void SetTransactionFeeDelegations_Add(string delegator, string delegatee, long balance, string token)
        {
            var symbol = token;
            var amount = balance;
            // _tokenContract.TransferBalance(InitAccount, delegatee, amount, symbol);
            var originDelegations = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
            Logger.Info(originDelegations);
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
            _tokenContract.SetAccount(delegatee);
            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.SetTransactionFeeDelegations, input);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var height = result.BlockNumber;
            var getDelegations = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
            Logger.Info(getDelegations);
            getDelegations.BlockHeight.ShouldBe(!originDelegations.Equals(new TransactionFeeDelegations())
                ? originDelegations.BlockHeight
                : height);

            if (originDelegations.Equals(new TransactionFeeDelegations()))
            {
                var logs = result.Logs.First(l => l.Name.Equals("TransactionFeeDelegationAdded")).Indexed;
                var logDelegator = TransactionFeeDelegationCancelled.Parser.ParseFrom(ByteString.FromBase64(logs[0]))
                    .Delegator;
                logDelegator.ShouldBe(delegator.ConvertAddress());
                var logDelegatee = TransactionFeeDelegationCancelled.Parser.ParseFrom(ByteString.FromBase64(logs[1]))
                    .Delegatee;
                logDelegatee.ShouldBe(delegatee.ConvertAddress());
                var caller = TransactionFeeDelegationCancelled.Parser.ParseFrom(ByteString.FromBase64(logs[2])).Caller;
                caller.ShouldBe(delegatee.ConvertAddress());
                caller.ShouldBe(result.Transaction.From.ConvertAddress());
            }
            else
            {
                var logs = result.Logs.Any(l => l.Name.Equals("TransactionFeeDelegationAdded"));
                logs.ShouldBe(false);
                getDelegations.Delegations.Count.ShouldBe(originDelegations.Delegations.Keys.Contains(symbol)
                    ? originDelegations.Delegations.Count
                    : originDelegations.Delegations.Count.Add(1));
            }

            getDelegations.Delegations[symbol].ShouldBe(amount);
            BoolValue.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue)).Value.ShouldBeTrue();
        }

        [TestMethod]
        public void SetTransactionFeeDelegations_Add()
        {
            SetTransactionFeeDelegations_Add(chargeAccount, InitAccount, 10_00000000, "ELF");
        }

        [TestMethod]
        public void SetTransactionFeeDelegateInfos_Add(string delegator, string delegatee, long balance)
        {
            // SetMethodOrSizeFee(BasicFeeSymbol, SizeFeeSymbol, 1);
            //set delegate
            _tokenContract.TransferBalance(InitAccount, delegatee, balance);
            _tokenContract.TransferBalance(InitAccount, delegatee, 15_00000000);
            var delegations1 = new Dictionary<string, long>
            {
                [NativeToken] = balance,
                /*[BasicFeeSymbol] = 500,
                [SizeFeeSymbol] = 100*/
            };
            var delegateInfo1 = new DelegateInfo()
            {
                ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
                MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
                Delegations =
                {
                    delegations1
                },
                IsUnlimitedDelegate = false
            };
            _tokenContract.SetAccount(delegatee);
            var result =  _tokenContract.ExecuteMethodWithResult(TokenMethod.SetTransactionFeeDelegateInfos, new SetTransactionFeeDelegateInfosInput
            {
                DelegatorAddress = delegator.ConvertAddress(),
                DelegateInfoList = { delegateInfo1 }
            });
            
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info(result.Transaction.RefBlockNumber);
            
            var delegateeAddress = _tokenContract.CallViewMethod<GetTransactionFeeDelegateeListOutput>(TokenMethod.GetTransactionFeeDelegateeList,
                new GetTransactionFeeDelegateeListInput
                {
                    ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
                    DelegatorAddress = delegator.ConvertAddress(),
                    MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer)
                });
            delegateeAddress.DelegateeAddresses.Count.ShouldBe(1);

            var delegation = _tokenContract.CallViewMethod<TransactionFeeDelegations>(TokenMethod.GetTransactionFeeDelegateInfo,
                new GetTransactionFeeDelegateInfoInput
                {
                    DelegateeAddress = delegatee.ConvertAddress(),
                    DelegatorAddress = delegator.ConvertAddress(),
                    ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
                    MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer)
                });
            Logger.Info($"delegator({delegator})");
            Logger.Info($"delegatee({delegatee})");
            Logger.Info($"secondary delegatee({delegation})");
            Logger.Info($"secondary delegatee({delegateeAddress.DelegateeAddresses[0]})");
            /*delegation.Delegations[BasicFeeSymbol].ShouldBe(500);
            delegation.Delegations[SizeFeeSymbol].ShouldBe(100);
            delegation.Delegations[NativeToken].ShouldBe(balance);*/
        }

        [TestMethod]
        public void SetTransactionFeeDelegateInfos_Add()
        {
            SetTransactionFeeDelegateInfos_Add(Delegator1, Delegatee1, 1000);
        }

        [TestMethod]
        public void GetDelegationsInfo()
        {
            var delegator = chargeAccount;
            var delegatee = InitAccount;
            var olddelegateeAddress = _tokenContract.CallViewMethod<GetTransactionFeeDelegateesOutput>(TokenMethod.GetTransactionFeeDelegatees,
                new GetTransactionFeeDelegateesInput()
                {
                    DelegatorAddress = delegator.ConvertAddress()
                });
            Logger.Info(olddelegateeAddress);
            var newdelegateeAddress = _tokenContract.CallViewMethod<GetTransactionFeeDelegateeListOutput>(TokenMethod.GetTransactionFeeDelegateeList,
                new GetTransactionFeeDelegateeListInput
                {
                    ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
                    DelegatorAddress = delegator.ConvertAddress(),
                    MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer)
                });
            Logger.Info(newdelegateeAddress);
            var olddelegation = _tokenContract.CallViewMethod<TransactionFeeDelegations>(TokenMethod.GetTransactionFeeDelegationsOfADelegatee, 
                new GetTransactionFeeDelegationsOfADelegateeInput
                {
                    DelegatorAddress = delegator.ConvertAddress(),
                    DelegateeAddress = delegatee.ConvertAddress()
                });
            Logger.Info(olddelegation);
            var newdelegation = _tokenContract.CallViewMethod<TransactionFeeDelegations>(TokenMethod.GetTransactionFeeDelegateInfo,
                new GetTransactionFeeDelegateInfoInput
                {
                    DelegateeAddress = delegatee.ConvertAddress(),
                    DelegatorAddress = delegator.ConvertAddress(),
                    ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
                    MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer)
                });
            Logger.Info(newdelegation);
        }
        
        [TestMethod]
        public void SecondaryDelegateTestL()
        {
            // set ACS1 MethodFee
            SetTokenContractMethodFee();
            
            // set Free Allowance
            SetFreeAllowance();
            
            // create test Account
            var account = NodeManager.NewAccount();
            var delegateAccount = NodeManager.NewAccount();
            var secondaryDelegateAccountNew = NodeManager.NewAccount();
            var secondaryDelegateAccountOld = NodeManager.NewAccount();
            // var levelThreeDelegateAccount = "2HGiJWCxnMyLmHzd3kThztYPMHTxogXxhmeBTs2Qp9ZMN66F9c";
            var client = NodeManager.NewAccount();
            // set delegator:account, delegatee:delegateAccount
            SetTransactionFeeDelegateInfos_Add(account, delegateAccount, 3_00000000);
            // set delegator:delegateAccount, delegatee:secondaryDelegateAccountNew
            SetTransactionFeeDelegateInfos_Add(delegateAccount, secondaryDelegateAccountNew, 10_00000000);

            // transfer ELF to account for test
            _tokenContract.TransferBalance(InitAccount, account, 3_00000000, "ELF");
            
            // Check allowance before transfer
            Logger.Info("----------------------------Before Transfer Allowance------------------------------");
            FreeAllowance_CheckFreeAllowance(account);
            FreeAllowance_CheckFreeAllowance(delegateAccount);
            FreeAllowance_CheckFreeAllowance(secondaryDelegateAccountNew);
            FreeAllowance_CheckFreeAllowance(secondaryDelegateAccountOld);
            
            
            var result = _tokenContract.TransferBalance(account, client, 1_00000000, "ELF");

            // check FreeAllowance
            Logger.Info("----------------------------After Transfer Allowance------------------------------");
            FreeAllowance_CheckFreeAllowance(account);
            FreeAllowance_CheckFreeAllowance(delegateAccount);
            FreeAllowance_CheckFreeAllowance(secondaryDelegateAccountNew);
            FreeAllowance_CheckFreeAllowance(secondaryDelegateAccountOld);
        }
        
        [TestMethod]
        public void ChargeFeeInSameBlock_Fail()
        {
            var delegator = "inTtQXaqBewvMUpXNm8ExHYFwKJZV2xVVxMW7waAV6oDWNjkJ";
            var delegatee = "28xdsdShJjPU2o75Qk9wnwqfoq7ayLFxzh5DBEqvJZUz4AHwGY";
            var secondaryDelegatee = "ijVtFJqYgvdnQxSaQiQuVaeYxdSNB9owXDULjFWr7qTQ7dH7k";
            _tokenContract.TransferBalance(InitAccount, delegator, 30000000);
            _tokenContract.TransferBalance(InitAccount, delegatee, 50000000);
            _tokenContract.TransferBalance(InitAccount, secondaryDelegatee, 50000000);
            _tokenContract.TransferBalance(InitAccount, secondaryDelegatee, 80, BasicFeeSymbol);
            _tokenContract.TransferBalance(InitAccount, secondaryDelegatee, 10_00000000, SizeFeeSymbol);
            AddTransactionFeeDelegateInfos(delegator, delegatee, 1000_00000000, 500_00000000, 100_00000000,
                false);
            AddTransactionFeeDelegateInfos(delegatee, secondaryDelegatee, 1000_00000000, 500_00000000, 100_00000000,
                false);

            var input1 = new TransferInput
            {
                To = InitAccount.ConvertAddress(),
                Symbol = "ELF",
                Amount = 20000000
            };
            var input2 = new TransferInput
            {
                To = InitAccount.ConvertAddress(),
                Symbol = "ELF",
                Amount = 10000000
            };
            
            Logger.Info("----------------------------Before Transfer Allowance------------------------------");
            FreeAllowance_CheckFreeAllowance(delegator);
            FreeAllowance_CheckFreeAllowance(delegatee);
            FreeAllowance_CheckFreeAllowance(secondaryDelegatee);
            FreeAllowance_CheckFreeAllowance(secondaryDelegatee, BasicFeeSymbol);
            FreeAllowance_CheckFreeAllowance(secondaryDelegatee, SizeFeeSymbol);

            var tx1 = NodeManager.SendTransaction(delegator, _tokenContract.ContractAddress,
                TokenMethod.Transfer.ToString(), input1);
            var tx2 = NodeManager.SendTransaction(delegator, _tokenContract.ContractAddress,
                TokenMethod.Transfer.ToString(), input2);
            
            Logger.Info("----------------------------After Transfer Allowance------------------------------");
            FreeAllowance_CheckFreeAllowance(delegator);
            FreeAllowance_CheckFreeAllowance(delegatee);
            FreeAllowance_CheckFreeAllowance(secondaryDelegatee);
            FreeAllowance_CheckFreeAllowance(secondaryDelegatee, BasicFeeSymbol);
            FreeAllowance_CheckFreeAllowance(secondaryDelegatee, SizeFeeSymbol);

            Logger.Info(tx1);
            Logger.Info(tx2);

        }

        [TestMethod]
        public void CheckDelegateFreeAllowance()
        {
            SetTokenContractMethodFee();
            SetFreeAllowance();

            var account = NodeManager.NewAccount();
            var delegatee1 = NodeManager.NewAccount();

            _tokenContract.TransferBalance(InitAccount, account, 1_00000000, NativeToken);
            _tokenContract.TransferBalance(InitAccount, account, 1_00000000, USDTSymbol);
            
            SetTransactionFeeDelegateInfos_Add(account, delegatee1, 20_00000000);

            Logger.Info("----------------------------Before Transfer Allowance------------------------------");
            FreeAllowance_CheckFreeAllowance(account);
            FreeAllowance_CheckFreeAllowance(delegatee1);
            _tokenContract.TransferBalance(account, InitAccount, 1_00000000, NativeToken);
            Logger.Info("----------------------------After Transfer Allowance------------------------------");
            FreeAllowance_CheckFreeAllowance(account);
            FreeAllowance_CheckFreeAllowance(delegatee1);
        }
        
        [TestMethod]
        public void SetFreeAllowance()
        {
            var threshold = 20_00000000;
            var symbol = "ELF";
            var freeAmount = 5_00000000;
            var organization = _parliament.GetGenesisOwnerAddress();

            // var input = new MethodFeeFreeAllowancesConfig
            // {
            //     FreeAllowances = new MethodFeeFreeAllowances
            //     {
            //         Value =
            //         {
            //             new MethodFeeFreeAllowance
            //             {
            //                 Symbol = symbol,
            //                 Amount = freeAmount
            //             }
            //         }
            //     },
            //     RefreshSeconds = 86400,
            //     Threshold = threshold
            // };

            var basicFreeAllowance = new TransactionFeeFreeAllowance
            {
                Symbol = symbol,
                Amount = freeAmount
            };

            var conf = new ConfigTransactionFeeFreeAllowance
            {
                Symbol = NativeToken,
                RefreshSeconds = 86400,
                Threshold = threshold,
                TransactionFeeFreeAllowances = new TransactionFeeFreeAllowances
                {
                    Value = { basicFreeAllowance }
                }
            };

            var input = new ConfigTransactionFeeFreeAllowancesInput
            {
                Value = { conf }
            };

            var result = AuthorityManager.ExecuteTransactionWithAuthority(_tokenContract.ContractAddress,
                "ConfigTransactionFeeFreeAllowances", input,
                InitAccount, organization);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var proposalId = _parliament.CreateProposal(_tokenContract.ContractAddress,
                "ConfigTransactionFeeFreeAllowances", input,
                organization, InitAccount);
            Logger.Info(proposalId.ToHex());

            // var config = _tokenContract.GetMethodFeeFreeAllowancesConfig();
            // config.Threshold.ShouldBe(threshold);
            // config.FreeAllowances.Value.First().Amount.ShouldBe(freeAmount);
            // config.FreeAllowances.Value.First().Symbol.ShouldBe(symbol);
            // config.RefreshSeconds.ShouldBe(86400);
        }

        [TestMethod]
        public void GetMethodFee()
        {
            var fee = _tokenContract.CallViewMethod<MethodFees>(TokenMethod.GetMethodFee, new StringValue
            {
                Value = nameof(TokenMethod.Transfer)
            });
            Logger.Info(fee);
        }
        
        [TestMethod]
        public void SetTokenContractMethodFee()
        {
            var symbol = Symbol;
            var fee = _tokenContract.CallViewMethod<MethodFees>(TokenMethod.GetMethodFee, new StringValue
            {
                Value = nameof(TokenMethod.Transfer)
            });
            Logger.Info(fee);
//            if (fee.Fees.Count > 0) return;
            var organization =
                _tokenContract.CallViewMethod<AuthorityInfo>(TokenMethod.GetMethodFeeController, new Empty())
                    .OwnerAddress;
            var input = new MethodFees
            {
                MethodName = nameof(TokenMethod.Transfer),
                Fees =
                {
                    new MethodFee
                    {
                        BasicFee = SymbolFee,
                        Symbol = symbol
                    },
                    new MethodFee
                    {
                        BasicFee = 10_00000000,
                        Symbol = USDTSymbol
                    }
                }, 
                IsSizeFeeFree = true
            };
            var result = AuthorityManager.ExecuteTransactionWithAuthority(_tokenContract.ContractAddress,
                "SetMethodFee", input,
                InitAccount, organization);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        
        public void FreeAllowance_CheckFreeAllowance(string account)
        {
            var elfBalance = _tokenContract.GetUserBalance(account);
            Logger.Info(elfBalance);

            /*var beforeFreeAllowance = _tokenContract.GetMethodFeeFreeAllowances(account);
            Logger.Info(beforeFreeAllowance);*/
        }
        
        public void FreeAllowance_CheckFreeAllowance(string account, string symbol)
        {
            var balance = _tokenContract.GetUserBalance(account, symbol);
            Logger.Info(balance);
        }

        [TestMethod] 
        public void SetAllDelegatees()
        {
            SetMethodOrSizeFee(BasicFeeSymbol, SizeFeeSymbol, 80);
            //var init_addr = "roPvXaeXAFB7KrRBrx1oQmEhurtuKivqq9dfnsyNigzBdwNJz";
            var delegator = NewAccount();
            _tokenContract.TransferBalance(InitAccount, delegator, 1_00000000);
            var ds = new List<string>();
            var sds = new Dictionary<string, List<string>>();
            //delegatee list
            for (int i = 1; i <= 2; i++)
            {
                var d = NewAccount();
                Logger.Info($"delegatee({d})");
                ds.Add(d);
                TransferFeeForSetDelegatee(d);
                Logger.Info(_tokenContract.GetUserBalance(d));
                AddTransactionFeeDelegateInfos(delegator, d, 1000_00000000, 500_00000000, 100_00000000, false);

            }
            //loop send set delegatee
            foreach (var d in ds)
            {
                sds.Add(d, new List<string>());
                for (int i = 1; i <= 2; i++)
                {
                    var sd = NewAccount();
                    Logger.Info($"secondary delegatee({sd})");
                    sds[d].Add(sd);
                    TransferFeeForSetDelegatee(sd);
                    Logger.Info(_tokenContract.GetUserBalance(sd));
                    AddTransactionFeeDelegateInfos(d, sd, 1000_00000000, 500_00000000, 100_00000000, false);
                }
            }
    
            Logger.Info($"({ds.Count})First delegatees");
            foreach (KeyValuePair<string, List<string>> item in sds)
            {
                Logger.Info($"({item.Value.Count}) Secondary delegatees for ({item.Key}) are");
                item.Value.ForEach(v => Logger.Info(v));
            }
            //_tokenContract.TransferBalance(init_addr, ds[ds.Count - 2], 100_00000000, "ELF");
            _tokenContract.TransferBalance(InitAccount, sds[ds.Last()].Last(), 80, BasicFeeSymbol);
            _tokenContract.TransferBalance(InitAccount, sds[ds.Last()].Last(), 10_00000000, SizeFeeSymbol);
                          
            Logger.Info($"delegator({delegator})");

            Thread.Sleep(60*1000);
            Logger.Info($"({sds[ds.Last()].Last()}) balance before send transaction({_tokenContract.GetUserBalance(sds[ds.Last()].Last(), SizeFeeSymbol)})");
            Logger.Info($"({sds[ds.Last()].Last()}) balance before send transaction({_tokenContract.GetUserBalance(sds[ds.Last()].Last(), BasicFeeSymbol)})");
            Logger.Info($"({sds[ds.Last()].Last()}) balance before send transaction({_tokenContract.GetUserBalance(sds[ds.Last()].Last())})");
            sds[ds.Last()].ForEach(v => Logger.Info(v));
            Logger.Info($"({sds[ds.Last()][-2]}) balance before send transaction({_tokenContract.GetUserBalance(sds[ds.Last()][-2], SizeFeeSymbol)})");
            Logger.Info($"({sds[ds.Last()][-2]}) balance before send transaction({_tokenContract.GetUserBalance(sds[ds.Last()][-2], BasicFeeSymbol)})");
            _tokenContract.SetAccount(delegator);
            _tokenContract.TransferBalance(delegator, InitAccount, 1_00000000);
            Logger.Info($"({sds[ds.Last()].Last()}) balance after send transaction({_tokenContract.GetUserBalance(sds[ds.Last()].Last(), SizeFeeSymbol)})");
            Logger.Info($"({sds[ds.Last()].Last()}) balance after send transaction({_tokenContract.GetUserBalance(sds[ds.Last()].Last(), BasicFeeSymbol)})");
            Logger.Info($"({sds[ds.Last()].Last()}) balance after send transaction({_tokenContract.GetUserBalance(sds[ds.Last()].Last())})");
            Logger.Info($"({sds[ds.Last()][-2]}) balance after send transaction({_tokenContract.GetUserBalance(sds[ds.Last()][-2], SizeFeeSymbol)})");
            Logger.Info($"({sds[ds.Last()][-2]}) balance after send transaction({_tokenContract.GetUserBalance(sds[ds.Last()][-2], BasicFeeSymbol)})");


        }

        [TestMethod]
        public string NewAccount()
        {
            var user = NodeManager.AccountManager.NewAccount("12345678");
            Logger.Info(user);
            return user;
        }

        [TestMethod]
        public long GetBalance(string user)
        {
            var result = _tokenContract.GetUserBalance(user);
            return result;
        }
        
        [TestMethod]
        public void SetDelegatee_NewDelegation(string delegator, string delegatee)
        {
            // Create an address to be used as Delegator
            // Create 128 addresses to be used as Delegatee
            // Verify that 128 delegatees are set to Delegator
            // Verify that the transaction can use the 128th address (no money, the transaction fails)
            // Verify that the transaction can use the 128th address (the last one has money, the status of transaction is mined)

            var delegations = new Dictionary<string, long>
            {
                [NativeToken] = 20000_00000000,
            };

            //var delegator = "2JJVeiJY7X11PDJYfpx1Mv555BGdEQs5Yz1d2ZXVXX4DmBMv6h";
            //var delegatee = "2iv15qpLpA9JtPrWGNJYoq9uBB2oTyWm9ZRp7Pyk2Ur52Zt9SL";
            _tokenContract.SetAccount(delegatee);
            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.SetTransactionFeeDelegations,
                new SetTransactionFeeDelegationsInput
                {
                    DelegatorAddress = delegator.ConvertAddress(),
                    Delegations =
                    {
                        delegations
                    }
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var delegation = _tokenContract.CallViewMethod<TransactionFeeDelegations>(TokenMethod.GetTransactionFeeDelegationsOfADelegatee, 
                new GetTransactionFeeDelegationsOfADelegateeInput
                {
                    DelegatorAddress = delegator.ConvertAddress(),
                    DelegateeAddress = delegatee.ConvertAddress()
                });
            Logger.Info($"delegation({delegation.Delegations})");
            //Logger.Info(result.GetDefaultTransactionFee());
            var d = _tokenContract.CallViewMethod<GetTransactionFeeDelegateesOutput>(TokenMethod.GetTransactionFeeDelegatees,
                new GetTransactionFeeDelegateesInput()
                {
                    DelegatorAddress = delegator.ConvertAddress()
                });
            Logger.Info($"delegatee count after setdelegatee({d.DelegateeAddresses.Count})");
            Logger.Info($"get after setdelegatee ({JsonConvert.SerializeObject(d.DelegateeAddresses.Select(a => a.ToBase58()))})");


        }
        
        [TestMethod]
        public void TransferFeeForSetDelegatee(string toaddr)
        {
            //var init_addr = "roPvXaeXAFB7KrRBrx1oQmEhurtuKivqq9dfnsyNigzBdwNJz";
            //var to_addr = "2iv15qpLpA9JtPrWGNJYoq9uBB2oTyWm9ZRp7Pyk2Ur52Zt9SL";
            
            var result = _tokenContract.TransferBalance(InitAccount, toaddr, 50000000, "ELF");
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }
        
        private void AddTransactionFeeDelegateInfos(string delegator, string delegatee, long nativeTokenAmount, long BasicFeeAmount, long sizeFeeAmount, bool isUnlimit)
        {
            // SetMethodOrSizeFee(BasicFeeSymbol, SizeFeeSymbol, 80);
            //set delegate
            _tokenContract.SetAccount(delegatee);
            var delegations1 = new Dictionary<string, long>
            {
                [NativeToken] = nativeTokenAmount,
                [BasicFeeSymbol] = BasicFeeAmount,
                [SizeFeeSymbol] = sizeFeeAmount
            };
            var delegateInfo1 = new DelegateInfo()
            {
                ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
                MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
                Delegations =
                {
                    delegations1
                },
                IsUnlimitedDelegate = isUnlimit
            };
            
            var delegateeAddressBefore = _tokenContract.CallViewMethod<GetTransactionFeeDelegateeListOutput>(TokenMethod.GetTransactionFeeDelegateeList,
                new GetTransactionFeeDelegateeListInput
                {
                    ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
                    DelegatorAddress = delegator.ConvertAddress(),
                    MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer)
                });
            
            var result =  _tokenContract.ExecuteMethodWithResult(TokenMethod.SetTransactionFeeDelegateInfos, new SetTransactionFeeDelegateInfosInput
            {
                DelegatorAddress = delegator.ConvertAddress(),
                DelegateInfoList = { delegateInfo1 }
            });
            
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info(result.Transaction.RefBlockNumber);
            
            var delegateeAddress = _tokenContract.CallViewMethod<GetTransactionFeeDelegateeListOutput>(TokenMethod.GetTransactionFeeDelegateeList,
                new GetTransactionFeeDelegateeListInput
                {
                    ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
                    DelegatorAddress = delegator.ConvertAddress(),
                    MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer)
                });
            delegateeAddress.DelegateeAddresses.Count.ShouldBe(delegateeAddressBefore.DelegateeAddresses.Count.Add(1));
        
            var delegation = _tokenContract.CallViewMethod<TransactionFeeDelegations>(TokenMethod.GetTransactionFeeDelegateInfo,
                new GetTransactionFeeDelegateInfoInput
                {
                    DelegateeAddress = delegatee.ConvertAddress(),
                    DelegatorAddress = delegator.ConvertAddress(),
                    ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
                    MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer)
                });
            delegation.Delegations[BasicFeeSymbol].ShouldBe(BasicFeeAmount);
            delegation.Delegations[SizeFeeSymbol].ShouldBe(sizeFeeAmount);
            delegation.Delegations[NativeToken].ShouldBe(nativeTokenAmount);
        }
        
        
        
        
        private void SetMethodOrSizeFee(string basicFeeSymbol, string sizeFeeSymbol, long basicFee)
        {
            var methodFee = new MethodFees
            {
                MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
                Fees =
                {
                    new MethodFee
                    {
                        Symbol = basicFeeSymbol,
                        BasicFee = basicFee
                    }
                }
            };
            SubmitAndPassProposalOfDefaultParliament(_tokenContract.ContractAddress.ConvertAddress(),
                nameof(TokenContractImplContainer.TokenContractImplStub.SetMethodFee), methodFee);

            var sizeFeeSymbolList = new SymbolListToPayTxSizeFee
            {
                SymbolsToPayTxSizeFee =
                {
                    new SymbolToPayTxSizeFee
                    {
                        TokenSymbol = SizeFeeSymbol,
                        AddedTokenWeight = 1,
                        BaseTokenWeight = 1
                    },
                    new SymbolToPayTxSizeFee
                    {
                        TokenSymbol = NativeToken,
                        AddedTokenWeight = 1,
                        BaseTokenWeight = 1
                    }
                }
            };
            SubmitAndPassProposalOfDefaultParliament(_tokenContract.ContractAddress.ConvertAddress(),
                nameof(TokenContractImplContainer.TokenContractImplStub.SetSymbolsToPayTxSizeFee), sizeFeeSymbolList);
        }

        
        private void SubmitAndPassProposalOfDefaultParliament(Address contractAddress, string methodName,
            IMessage input)
        {
            var defaultParliament = _parliament.CallViewMethod<Address>(ParliamentMethod.GetDefaultOrganizationAddress, new Empty());
            var proposal = new CreateProposalInput
            {
                OrganizationAddress = defaultParliament,
                ToAddress = contractAddress,
                Params = input.ToByteString(),
                ContractMethodName = methodName,
                ExpiredTime = DateTime.UtcNow.ToTimestamp().AddHours(1)
            };
            var createProposalRet = _parliament.ExecuteMethodWithResult(ParliamentMethod.CreateProposal, proposal);
            createProposalRet.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var proposalId = Hash.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(createProposalRet.ReturnValue));
            var approveRet = _parliament.ExecuteMethodWithResult(ParliamentMethod.Approve, proposalId);
            var releaseRet = _parliament.ExecuteMethodWithResult(ParliamentMethod.Release, proposalId);
            releaseRet.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }
        
        private void CreateToken(string symbol, int decimals, Address issuer, long totalSupply)
        {
            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create,
                new AElf.Contracts.MultiToken.CreateInput
                {
                    Symbol = symbol,
                    Decimals = decimals,
                    Issuer = issuer,
                    TokenName = $"{symbol} token",
                    TotalSupply = totalSupply,
                    IsBurnable = true
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }
        
        private void IssueBalance(string symbol, long amount, Address toAddress)
        {
            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Issue, new IssueInput
            {
                Symbol = symbol,
                Amount = amount,
                To = toAddress,
            });
            
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"Successfully issue amount {amount} to {toAddress}");
        }
        
        
        #endregion

    }
}