using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Client.Dto;
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

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class TokenContractSecondaryDelegateeTest
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
        
        private Dictionary<SchemeType, Scheme> Schemes { get; set; }
        private string InitAccount { get; } = "Du9KDALrVrboq51NHDaFgrfPWUJbur2PMCjVgGt7iw2qdMuBk";
        private string transferAccount { get; } = "zkWrJiNT8B4af6auBzn3WuhNrd3zHtmercyQ4sar7GxM8Xwy9";
        private string chargeAccount { get; } = "NCt7dRM9m7vFfbXcrhhN1cLZL8R4Yveur2uS8C7T8Pim2Umhk";
        private string testAccount { get; } = "2dDpyx4sqTnZvTJHzxRaaaWwcSwJjFVHMWaJq7QPREaW47mwzS";
        private static string RpcUrl { get; } = "https://aelf-public-node.aelf.io";
        // private static string SideRpcUrl { get; } = "";
        
        private const string BasicFeeSymbol = "BASIC";
        private const string BasicASymbol = "BASICA";
        private const string SizeFeeSymbol = "SIZE";
        private const string SizeASymbol = "SIZEA";
        private const string NativeToken = "ELF";

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("TokenContractSecondaryDelegateeTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes");

            NodeManager = new NodeManager(RpcUrl);
            ContractManager = new ContractManager(NodeManager, InitAccount);
            AuthorityManager = new AuthorityManager(NodeManager, InitAccount);

            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
            _tokenConverter = _genesisContract.GetTokenConverterContract(InitAccount);
            _parliament = _genesisContract.GetParliamentContract(InitAccount);
            _tokenContractImpl = _genesisContract.GetTokenImplStub();

            // _treasury = _genesisContract.GetTreasuryContract(InitAccount);
            // _profit = _genesisContract.GetProfitContract(InitAccount);
            // _profit.GetTreasurySchemes(_treasury.ContractAddress);
            Schemes = ProfitContract.Schemes;

            _tokenConverterSub = _genesisContract.GetTokenConverterStub(InitAccount);

            // if (_tokenContract.GetTokenInfo(BasicFeeSymbol).Equals(new TokenInfo()))
            // {
            //     CreateToken(BasicFeeSymbol, 8, InitAccount.ConvertAddress(), 100000000_00000000);
            //     IssueBalance(BasicFeeSymbol, 100000000_00000000, InitAccount.ConvertAddress());
            // }
            // if (_tokenContract.GetTokenInfo(SizeFeeSymbol).Equals(new TokenInfo()))
            // {
            //     CreateToken(SizeFeeSymbol, 8, InitAccount.ConvertAddress(), 100000000_00000000);
            //     IssueBalance(SizeFeeSymbol, 100000000_00000000, InitAccount.ConvertAddress());
            // }
            // if (_tokenContract.GetTokenInfo(BasicASymbol).Equals(new TokenInfo()))
            // {
            //     CreateToken(BasicASymbol, 8, InitAccount.ConvertAddress(), 100000000_00000000);
            //     IssueBalance(BasicASymbol, 100000000_00000000, InitAccount.ConvertAddress());
            // }
            // if (_tokenContract.GetTokenInfo(SizeASymbol).Equals(new TokenInfo()))
            // {
            //     CreateToken(SizeASymbol, 8, InitAccount.ConvertAddress(), 100000000_00000000);
            //     IssueBalance(SizeASymbol, 100000000_00000000, InitAccount.ConvertAddress());
            // }
        }

        [TestMethod]
        [DataRow("NCt7dRM9m7vFfbXcrhhN1cLZL8R4Yveur2uS8C7T8Pim2Umhk")]
        [DataRow("Du9KDALrVrboq51NHDaFgrfPWUJbur2PMCjVgGt7iw2qdMuBk")]
        // [DataRow("zkWrJiNT8B4af6auBzn3WuhNrd3zHtmercyQ4sar7GxM8Xwy9")]
        public void GetUserBalance(string account, string symbol="ELF")
        {
            var result = _tokenContract.GetUserBalance(account, symbol);
            Logger.Info(result);
        }

        [TestMethod]
        public void transfer()
        {
            var from = chargeAccount;
            var to = testAccount;
            var amount = 1;
            Logger.Info("before transfer from balance:");
            GetUserBalance(from);
            Logger.Info("before transfer to balance:");
            GetUserBalance(to);
            _tokenContract.TransferBalance(from, to, amount, NativeToken);
            Logger.Info("after transfer from balance:");
            GetUserBalance(from);
            Logger.Info("after transfer to balance:");
            GetUserBalance(to);
        }

        [TestMethod]
        public void GetDelegationInfo()
        {
            var delegator = InitAccount;
            var delegatee = transferAccount;
            var newdelegateeAddress = _tokenContract.CallViewMethod<GetTransactionFeeDelegateeListOutput>(TokenMethod.GetTransactionFeeDelegateeList,
                new GetTransactionFeeDelegateeListInput
                {
                    ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
                    DelegatorAddress = delegator.ConvertAddress(),
                    MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer)
                });
            Logger.Info(newdelegateeAddress);
            var olddelegateeAddress = _tokenContract.CallViewMethod<GetTransactionFeeDelegateesOutput>(TokenMethod.GetTransactionFeeDelegatees,
                new GetTransactionFeeDelegateesInput()
                {
                    DelegatorAddress = delegator.ConvertAddress()
                });
            Logger.Info(olddelegateeAddress);
            var delegation = _tokenContract.CallViewMethod<TransactionFeeDelegations>(TokenMethod.GetTransactionFeeDelegateInfo,
                new GetTransactionFeeDelegateInfoInput
                {
                    DelegateeAddress = delegatee.ConvertAddress(),
                    DelegatorAddress = delegator.ConvertAddress(),
                    ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
                    MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer)
                });
            Logger.Info(delegation);
        }

        [TestMethod]
        public void CheckLog()
        {
            var log = "CiIKIB1J73VYxzm0IKQZ0p+DCpVsWa4oxjrfBaEbzq8VxE+9EiIKIIMhBwfL+j8r4bGDGpZyzfBLe0dEg0C0pwMW2qc/xgfOGiIKIIMhBwfL+j8r4bGDGpZyzfBLe0dEg0C0pwMW2qc/xgfOIjAKLgoiCiAnkemSpX8o51oR8TrywK7IsOs10vBI1C66iQHJLgN43BIIVHJhbnNmZXI=";
            var result = TransactionFeeDelegateInfoUpdated.Parser.ParseFrom(ByteString.FromBase64(log));
            Logger.Info(result);
        }

        [TestMethod]
        public void SetTransactionFeeDelegateInfos_Add()
        {
            // SetMethodOrSizeFee(BasicFeeSymbol, SizeFeeSymbol, 80);
            var delegatee = transferAccount;
            var delegator = InitAccount;
            // _tokenContract.TransferBalance(InitAccount, delegatee, 50000000);
            // _tokenContract.TransferBalance(InitAccount, delegator, 1_00000000);
            // _tokenContract.TransferBalance(InitAccount, delegatee, 80, BasicFeeSymbol);
            // _tokenContract.TransferBalance(InitAccount, delegatee, 10_00000000,SizeFeeSymbol);
            
            //set delegate
            var delegations1 = new Dictionary<string, long>
            {
                [NativeToken] = 10_00000000,
                // [BasicFeeSymbol] = 500,
                // [SizeFeeSymbol] = 100
            };
            var delegateInfo1 = new DelegateInfo()
            {
                ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
                MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
                Delegations =
                {
                    delegations1
                },
                IsUnlimitedDelegate = true
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
            Logger.Info(delegateeAddress);
        
            var delegation = _tokenContract.CallViewMethod<TransactionFeeDelegations>(TokenMethod.GetTransactionFeeDelegateInfo,
                new GetTransactionFeeDelegateInfoInput
                {
                    DelegateeAddress = delegatee.ConvertAddress(),
                    DelegatorAddress = delegator.ConvertAddress(),
                    ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
                    MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer)
                });
            // delegation.Delegations[BasicFeeSymbol].ShouldBe(500);
            // delegation.Delegations[SizeFeeSymbol].ShouldBe(100);
            // delegation.Delegations[NativeToken].ShouldBe(10_00000000);
            delegation.BlockHeight.ShouldBe(result.BlockNumber);
            delegation.IsUnlimitedDelegate.ShouldBe(true);
            Logger.Info(delegation);
            
            var logs = result.Logs.First(n => n.Name.Equals(nameof(TransactionFeeDelegateInfoAdded))).NonIndexed;
            var eventlog = TransactionFeeDelegateInfoAdded.Parser.ParseFrom(ByteString.FromBase64(logs));
            eventlog.Delegatee.ShouldBe(delegatee.ConvertAddress());
            eventlog.Delegator.ShouldBe(delegator.ConvertAddress());
            eventlog.Caller.ShouldBe(delegatee.ConvertAddress());

            var dt = new DelegateTransaction
            {
                ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
                MethodName = TokenMethod.Transfer.ToString()
            };
            eventlog.DelegateTransactionList.Value[0].ShouldBe(dt);
        }

        [TestMethod]
        public void SetTransactionFeeDelegateInfos_Update()
        {
            // SetMethodOrSizeFee(BasicFeeSymbol, SizeFeeSymbol, 80);
            var delegator = InitAccount;
            var delegatee = transferAccount;
            // _tokenContract.TransferBalance(InitAccount, delegatee, 1_00000000);
            // _tokenContract.TransferBalance(InitAccount, delegator, 1_00000000);
            // _tokenContract.TransferBalance(InitAccount, delegatee, 80, BasicFeeSymbol);
            // _tokenContract.TransferBalance(InitAccount, delegatee, 10_00000000,SizeFeeSymbol);
            // AddTransactionFeeDelegateInfos(delegator, delegatee, 1000_00000000, 500, 100_00000000, false);

            var delegateeAddress = _tokenContract.CallViewMethod<GetTransactionFeeDelegateeListOutput>(TokenMethod.GetTransactionFeeDelegateeList,
                new GetTransactionFeeDelegateeListInput
                {
                    ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
                    DelegatorAddress = delegator.ConvertAddress(),
                    MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer)
                });
            delegateeAddress.DelegateeAddresses.Count.ShouldBe(1);
            
            var delegations1 = new Dictionary<string, long>
            {
                // [BasicFeeSymbol] = 500,
                // [SizeFeeSymbol] = 100
                [NativeToken] = 2000
            };
            var delegateInfo1 = new DelegateInfo()
            {
                ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
                MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
                Delegations =
                {
                    delegations1
                },
                IsUnlimitedDelegate = true
            };
            
            _tokenContract.SetAccount(delegatee);
            var result =  _tokenContract.ExecuteMethodWithResult(TokenMethod.SetTransactionFeeDelegateInfos, new SetTransactionFeeDelegateInfosInput
            {
                DelegatorAddress = delegator.ConvertAddress(),
                DelegateInfoList = { delegateInfo1 }
            });
            
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var delegation = _tokenContract.CallViewMethod<TransactionFeeDelegations>(TokenMethod.GetTransactionFeeDelegateInfo,
                new GetTransactionFeeDelegateInfoInput
                {
                    DelegateeAddress = delegatee.ConvertAddress(),
                    DelegatorAddress = delegator.ConvertAddress(),
                    ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
                    MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer)
                });

            delegation.IsUnlimitedDelegate.ShouldBe(true);

            var delegateeAddress1 = _tokenContract.CallViewMethod<GetTransactionFeeDelegateeListOutput>(TokenMethod.GetTransactionFeeDelegateeList,
                new GetTransactionFeeDelegateeListInput
                {
                    ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
                    DelegatorAddress = delegator.ConvertAddress(),
                    MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer)
                });
            delegateeAddress1.DelegateeAddresses.Count.ShouldBe(1);
            var logs = result.Logs.First(n => n.Name.Equals(nameof(TransactionFeeDelegateInfoUpdated))).NonIndexed;
            var eventlog = TransactionFeeDelegateInfoUpdated.Parser.ParseFrom(ByteString.FromBase64(logs));
            eventlog.Delegatee.ShouldBe(delegatee.ConvertAddress());
            eventlog.Delegator.ShouldBe(delegator.ConvertAddress());
            eventlog.Caller.ShouldBe(delegatee.ConvertAddress());
            
            var dt = new DelegateTransaction
            {
                ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
                MethodName = TokenMethod.Transfer.ToString()
            };
            eventlog.DelegateTransactionList.Value[0].ShouldBe(dt);
            
        }

        [TestMethod]
        public void RemoveTransactionFeeDelegateeInfos()
        {
            // SetMethodOrSizeFee(BasicFeeSymbol, SizeFeeSymbol, 80);
            var delegator = chargeAccount;
            var delegatee = InitAccount;
            // var delegatee1 = NewAccount();
            // _tokenContract.TransferBalance(InitAccount, delegatee, 50000000);
            // _tokenContract.TransferBalance(InitAccount, delegator, 1_00000000);
            // _tokenContract.TransferBalance(InitAccount, delegatee1, 1_00000000);
            // _tokenContract.TransferBalance(InitAccount, delegatee, 80, BasicFeeSymbol);
            // _tokenContract.TransferBalance(InitAccount, delegatee, 10_00000000,SizeFeeSymbol);
            
            // AddTransactionFeeDelegateInfos(delegator, delegatee, 1000_00000000, 500_00000000, 100_00000000, false);
            // AddTransactionFeeDelegateInfos(delegator, delegatee1, 1000_00000000, 500_00000000, 100_00000000, false);

            var delegateeAddress = _tokenContract.CallViewMethod<GetTransactionFeeDelegateeListOutput>(TokenMethod.GetTransactionFeeDelegateeList,
                new GetTransactionFeeDelegateeListInput
                {
                    ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
                    DelegatorAddress = delegator.ConvertAddress(),
                    MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer)
                });
            delegateeAddress.DelegateeAddresses.Count.ShouldBe(1);
            
            var delegateTransaction = new DelegateTransaction
            {
                ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
                MethodName = TokenMethod.Transfer.ToString()
            };
            
            _tokenContract.SetAccount(delegator);
            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.RemoveTransactionFeeDelegateeInfos,
                new RemoveTransactionFeeDelegateeInfosInput
                {
                    DelegateTransactionList = {delegateTransaction},
                    DelegateeAddress = delegatee.ConvertAddress()
                });
            
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            // var delegateeAddress1 = _tokenContract.CallViewMethod<GetTransactionFeeDelegateeListOutput>(TokenMethod.GetTransactionFeeDelegateeList,
            //     new GetTransactionFeeDelegateeListInput
            //     {
            //         ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
            //         DelegatorAddress = delegator.ConvertAddress(),
            //         MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer)
            //     });
            // delegateeAddress1.DelegateeAddresses.Count.ShouldBe(1);
            // delegateeAddress1.DelegateeAddresses[0].ShouldBe(delegatee1.ConvertAddress());
            
            var logs = result.Logs.First(n => n.Name.Equals(nameof(TransactionFeeDelegateInfoCancelled))).NonIndexed;
            var eventlog = TransactionFeeDelegateInfoCancelled.Parser.ParseFrom(ByteString.FromBase64(logs));
            eventlog.Delegatee.ShouldBe(delegatee.ConvertAddress());
            eventlog.Delegator.ShouldBe(delegator.ConvertAddress());
            eventlog.Caller.ShouldBe(delegator.ConvertAddress());
        }
        
        [TestMethod]
        public void RemoveTransactionFeeDelegateeInfos_failed()
        {
            SetMethodOrSizeFee(BasicFeeSymbol, SizeFeeSymbol, 80);
            var delegator = NewAccount();
            var delegator1 = NewAccount();
            var delegatee = NewAccount();
            _tokenContract.TransferBalance(InitAccount, delegatee, 1_00000000);
            _tokenContract.TransferBalance(InitAccount, delegator, 1_00000000);
            _tokenContract.TransferBalance(InitAccount, delegator1, 1_00000000);
            _tokenContract.TransferBalance(InitAccount, delegatee, 80, BasicFeeSymbol);
            _tokenContract.TransferBalance(InitAccount, delegatee, 10_00000000,SizeFeeSymbol);

            AddTransactionFeeDelegateInfos(delegator, delegatee, 1000_00000000, 500_00000000, 100_00000000, false);

            var delegateTransaction = new DelegateTransaction
            {
                ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
                MethodName = TokenMethod.Transfer.ToString()
            };

            _tokenContract.SetAccount(delegator1);
            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.RemoveTransactionFeeDelegateeInfos,
                new RemoveTransactionFeeDelegateeInfosInput
                {
                    DelegateTransactionList = {delegateTransaction},
                    DelegateeAddress = delegatee.ConvertAddress()
                });
            
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logs = result.Logs.Any(l => l.Name.Equals("TransactionFeeDelegateInfoCancelled"));
            logs.ShouldBe(false);
        }

        [TestMethod]
        public void RemoveTransactionFeeDelegatorInfos()
        {
            SetMethodOrSizeFee(BasicFeeSymbol, SizeFeeSymbol, 80);
            var delegator = NewAccount();
            var delegator1 = NewAccount();
            var delegatee = NewAccount();
            _tokenContract.TransferBalance(InitAccount, delegatee, 1_00000000);
            _tokenContract.TransferBalance(InitAccount, delegator, 1_00000000);
            _tokenContract.TransferBalance(InitAccount, delegator1, 1_00000000);
            _tokenContract.TransferBalance(InitAccount, delegatee, 80, BasicFeeSymbol);
            _tokenContract.TransferBalance(InitAccount, delegatee, 10_00000000,SizeFeeSymbol);
            
            AddTransactionFeeDelegateInfos(delegator, delegatee, 1000_00000000, 500_00000000, 100_00000000, false);
            AddTransactionFeeDelegateInfos(delegator1, delegatee, 1000_00000000, 500_00000000, 100_00000000, false);

            _tokenContract.SetAccount(delegatee);
            
            var delegateTransaction = new DelegateTransaction
            {
                ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
                MethodName = TokenMethod.Transfer.ToString()
            };
            
            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.RemoveTransactionFeeDelegatorInfos,
                new RemoveTransactionFeeDelegatorInfosInput
                {
                    DelegatorAddress = delegator.ConvertAddress(),
                    DelegateTransactionList = { delegateTransaction }
                });
            
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var delegateeAddress = _tokenContract.CallViewMethod<GetTransactionFeeDelegateeListOutput>(TokenMethod.GetTransactionFeeDelegateeList,
                new GetTransactionFeeDelegateeListInput
                {
                    ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
                    DelegatorAddress = delegator.ConvertAddress(),
                    MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer)
                });
            delegateeAddress.DelegateeAddresses.Count.ShouldBe(0);
            
            var delegateeAddress1 = _tokenContract.CallViewMethod<GetTransactionFeeDelegateeListOutput>(TokenMethod.GetTransactionFeeDelegateeList,
                new GetTransactionFeeDelegateeListInput
                {
                    ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
                    DelegatorAddress = delegator1.ConvertAddress(),
                    MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer)
                });
            delegateeAddress1.DelegateeAddresses.Count.ShouldBe(1);
            
            var logs = result.Logs.First(n => n.Name.Equals(nameof(TransactionFeeDelegateInfoCancelled))).NonIndexed;
            var eventlog = TransactionFeeDelegateInfoCancelled.Parser.ParseFrom(ByteString.FromBase64(logs));
            eventlog.Delegatee.ShouldBe(delegatee.ConvertAddress());
            eventlog.Delegator.ShouldBe(delegator.ConvertAddress());
            eventlog.Caller.ShouldBe(delegatee.ConvertAddress());
            
        }
        
        [TestMethod]
        public void RemoveTransactionFeeDelegatorInfos_failed()
        {
            SetMethodOrSizeFee(BasicFeeSymbol, SizeFeeSymbol, 80);
            var delegator = NewAccount();
            var delegator1 = NewAccount();
            var delegatee = NewAccount();
            _tokenContract.TransferBalance(InitAccount, delegatee, 1_00000000);
            _tokenContract.TransferBalance(InitAccount, delegator, 1_00000000);
            _tokenContract.TransferBalance(InitAccount, delegator1, 1_00000000);
            _tokenContract.TransferBalance(InitAccount, delegatee, 80, BasicFeeSymbol);
            _tokenContract.TransferBalance(InitAccount, delegatee, 10_00000000,SizeFeeSymbol);
            
            AddTransactionFeeDelegation(delegator, delegatee, 1000_00000000, 500_00000000, 100_00000000);
            
            var delegateTransaction = new DelegateTransaction
            {
                ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
                MethodName = TokenMethod.Transfer.ToString()
            };

            _tokenContract.SetAccount(delegatee);
            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.RemoveTransactionFeeDelegatorInfos,
                new RemoveTransactionFeeDelegatorInfosInput
                {
                    DelegatorAddress = delegator1.ConvertAddress(),
                    DelegateTransactionList = {delegateTransaction }
                });
            
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logs = result.Logs.Any(l => l.Name.Equals("TransactionFeeDelegateInfoCancelled"));
            logs.ShouldBe(false);
            
        }
        
        [TestMethod]
        public void ChargeTransactionFee_InSameBlock_Fail()
        {
            SetMethodOrSizeFee(BasicFeeSymbol, SizeFeeSymbol, 80);
            var delegator = NewAccount();
            var delegatee = NewAccount();
            var secondaryDelegatee = NewAccount();
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
            
            Thread.Sleep(60 * 1000);
            var tx1 = NodeManager.SendTransaction(delegator, _tokenContract.ContractAddress,
                TokenMethod.Transfer.ToString(), input1);
            var tx2 = NodeManager.SendTransaction(delegator, _tokenContract.ContractAddress,
                TokenMethod.Transfer.ToString(), input2);
            
            Logger.Info(tx1);
            Logger.Info(tx2);
            var txs = new List<string>{tx1, tx2};
            var status = new List<TransactionResultStatus>();
            foreach (var tx in txs)
            {
                var transactionResult =
                    AsyncHelper.RunSync(() => NodeManager.ApiClient.GetTransactionResultAsync(tx));
                status.Add(transactionResult.Status.ConvertTransactionResultStatus());
            }
            
            status.GroupBy(x => TransactionResultStatus.Mined).Count().ShouldBe(1);
            status.GroupBy(x => TransactionResultStatus.Failed).Count().ShouldBe(txs.Count - 1);

        }

        [TestMethod]
        public void ChargeTransactionFee_Delegation_OldFirst_OldSecondAndNewSecond()
        {
            SetMethodOrSizeFee(BasicFeeSymbol, SizeFeeSymbol, 80);
            var delegator = NewAccount();
            var oldFirstDelegatee = NewAccount();
            var newSecondaryDelegatee = NewAccount();
            var oldSecondaryDelegatee = NewAccount();
            _tokenContract.TransferBalance(InitAccount, delegator, 1_00000000);
            _tokenContract.TransferBalance(InitAccount, oldFirstDelegatee, 50000000);
            _tokenContract.TransferBalance(InitAccount, newSecondaryDelegatee, 50000000);
            _tokenContract.TransferBalance(InitAccount, oldSecondaryDelegatee, 50000000);
            _tokenContract.TransferBalance(InitAccount, newSecondaryDelegatee, 80, BasicFeeSymbol);
            _tokenContract.TransferBalance(InitAccount, oldSecondaryDelegatee, 80, BasicFeeSymbol);
            _tokenContract.TransferBalance(InitAccount, newSecondaryDelegatee, 10_00000000, SizeFeeSymbol);
            _tokenContract.TransferBalance(InitAccount, oldSecondaryDelegatee, 10_00000000, SizeFeeSymbol);
            AddTransactionFeeDelegation(delegator, oldFirstDelegatee, 1000_00000000, 500, 100_00000000);
            AddTransactionFeeDelegation(oldFirstDelegatee, oldSecondaryDelegatee, 1000, 500_00000000, 100_00000000);
            
            AddTransactionFeeDelegateInfos(oldFirstDelegatee, newSecondaryDelegatee, 1000, 500_00000000, 100_00000000,
                false);
            
            Thread.Sleep(60*1000);
            
            Logger.Info($"({newSecondaryDelegatee}) balance before send transaction({_tokenContract.GetUserBalance(newSecondaryDelegatee, SizeFeeSymbol)})");
            Logger.Info($"({newSecondaryDelegatee}) balance before send transaction({_tokenContract.GetUserBalance(newSecondaryDelegatee, BasicFeeSymbol)})");
            Logger.Info($"({newSecondaryDelegatee}) balance before send transaction({_tokenContract.GetUserBalance(newSecondaryDelegatee)})");
            var beforeBalance = _tokenContract.GetUserBalance(oldSecondaryDelegatee, BasicFeeSymbol);
            var result = _tokenContract.TransferBalance(delegator, InitAccount, 1_00000000);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _tokenContract.GetUserBalance(newSecondaryDelegatee,BasicFeeSymbol).ShouldBe(beforeBalance.Sub(80));
            Logger.Info($"({newSecondaryDelegatee}) balance after send transaction({_tokenContract.GetUserBalance(newSecondaryDelegatee, SizeFeeSymbol)})");
            Logger.Info($"({newSecondaryDelegatee}) balance after send transaction({_tokenContract.GetUserBalance(newSecondaryDelegatee, BasicFeeSymbol)})");
            Logger.Info($"({newSecondaryDelegatee}) balance after send transaction({_tokenContract.GetUserBalance(newSecondaryDelegatee)})");
        }

        [TestMethod]
        public void ChargeTransactionFee_Delegation_NewSecond_FreeAllowance()
        {
            SetMethodOrSizeFee(BasicFeeSymbol, SizeFeeSymbol, 80);
            SetPrimaryTokenSymbol();
            
            //set free allowance
            var basicFreeAllowance = new TransactionFeeFreeAllowance
            {
                Symbol = BasicFeeSymbol,
                Amount = 1000
            };
            var sizeFreeAllowance = new TransactionFeeFreeAllowance
            {
                Symbol = SizeFeeSymbol,
                Amount = 10_00000000
            };
            var conf = new ConfigTransactionFeeFreeAllowance
            {
                Symbol = NativeToken,
                RefreshSeconds = 100,
                Threshold = 1000_00000000,
                TransactionFeeFreeAllowances = new TransactionFeeFreeAllowances
                {
                    Value = {basicFreeAllowance, sizeFreeAllowance}
                }
            };
            
            SubmitAndPassProposalOfDefaultParliament(_tokenContract.ContractAddress.ConvertAddress(),
                nameof(TokenMethod.ConfigTransactionFeeFreeAllowances), new ConfigTransactionFeeFreeAllowancesInput
                {
                    Value = { conf }
                });

            var delegator = NewAccount();
            var delegatee = NewAccount();
            var secondaryDelegatee = NewAccount();
            
            _tokenContract.TransferBalance(InitAccount, delegator, 1_00000000);
            _tokenContract.TransferBalance(InitAccount, delegatee, 50000000);
            _tokenContract.TransferBalance(InitAccount, secondaryDelegatee, 1000_00000000);
            _tokenContract.TransferBalance(InitAccount, secondaryDelegatee, 80, BasicFeeSymbol);
            _tokenContract.TransferBalance(InitAccount, secondaryDelegatee, 10_00000000, SizeFeeSymbol);
            AddTransactionFeeDelegateInfos(delegator, delegatee, 1000_00000000, 500, 100_00000000,
                false);
            AddTransactionFeeDelegateInfos(delegatee, secondaryDelegatee, 1000_00000000, 500, 100_00000000,
                false);

            var delegateeAddress = _tokenContract.CallViewMethod<GetTransactionFeeDelegateeListOutput>(TokenMethod.GetTransactionFeeDelegateeList,
                new GetTransactionFeeDelegateeListInput
                {
                    ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
                    DelegatorAddress = delegator.ConvertAddress(),
                    MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer)
                });
            delegateeAddress.DelegateeAddresses.Count.ShouldBe(1);
            
            Thread.Sleep(60 * 1000);

            var result = _tokenContract.TransferBalance(delegator, InitAccount, 1_00000000);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            //secondary balance not change
            _tokenContract.GetUserBalance(secondaryDelegatee, BasicFeeSymbol).ShouldBe(80);
            _tokenContract.GetUserBalance(secondaryDelegatee, SizeFeeSymbol).ShouldBe(10_00000000);
            _tokenContract.CallViewMethod<TransactionFeeFreeAllowancesMap>(TokenMethod.GetTransactionFeeFreeAllowances,
                secondaryDelegatee.ConvertAddress()).Map[NativeToken].Map[BasicFeeSymbol].Amount.ShouldBe(920);

            //secondary delegation
            var delegation = _tokenContract.CallViewMethod<TransactionFeeDelegations>(TokenMethod.GetTransactionFeeDelegateInfo, new GetTransactionFeeDelegateInfoInput
            {
                DelegateeAddress = secondaryDelegatee.ConvertAddress(),
                DelegatorAddress = delegatee.ConvertAddress(),
                ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
                MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer)
            });
            delegation.Delegations[BasicFeeSymbol].ShouldBe(420);
        }

        [TestMethod]
        public void ChargeTransactionFee_Delegation_MultiSymbolFreeAllowance()
        {
            //set basic fee
            var methodFee = new MethodFees
            {
                MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
                Fees =
                {
                    new MethodFee
                    {
                        Symbol = BasicFeeSymbol,
                        BasicFee = 80
                    },
                    new MethodFee
                    {
                        Symbol = BasicASymbol,
                        BasicFee = 30
                    }
                }
            };
            SubmitAndPassProposalOfDefaultParliament(_tokenContract.ContractAddress.ConvertAddress(),
                nameof(TokenContractImplContainer.TokenContractImplStub.SetMethodFee), methodFee);
            
            //set size fee
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
                        TokenSymbol = SizeASymbol,
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
            
            
            SetPrimaryTokenSymbol();
            
            //SetFreeAllowance(BasicFeeSymbol, SizeFeeSymbol, 1000_00000000,1000, 10_00000000);
            //set free allowance
            var basicFreeAllowance = new TransactionFeeFreeAllowance
            {
                Symbol = BasicFeeSymbol,
                Amount = 50
            };
            var basicAFreeAllowance = new TransactionFeeFreeAllowance
            {
                Symbol = BasicASymbol,
                Amount = 40
            };
            var sizeFreeAllowance = new TransactionFeeFreeAllowance
            {
                Symbol = SizeFeeSymbol,
                Amount = 1
            };
            var sizeAFreeAllowance = new TransactionFeeFreeAllowance
            {
                Symbol = SizeASymbol,
                Amount = 10_00000000
            };
            var conf1 = new ConfigTransactionFeeFreeAllowance
            {
                Symbol = NativeToken,
                RefreshSeconds = 100,
                Threshold = 1000_00000000,
                TransactionFeeFreeAllowances = new TransactionFeeFreeAllowances
                {
                    Value = { basicFreeAllowance, basicAFreeAllowance, sizeFreeAllowance, sizeAFreeAllowance}
                }
            };
            SubmitAndPassProposalOfDefaultParliament(_tokenContract.ContractAddress.ConvertAddress(),
                nameof(TokenMethod.ConfigTransactionFeeFreeAllowances), new ConfigTransactionFeeFreeAllowancesInput
                {
                    Value = { conf1 }
                });
            
            var delegator = NewAccount();
            var delegatee = NewAccount();
            
            _tokenContract.TransferBalance(InitAccount, delegator, 1_00000000);
            _tokenContract.TransferBalance(InitAccount, delegatee, 1000_50000000);
            _tokenContract.TransferBalance(InitAccount, delegatee, 80, BasicFeeSymbol);
            _tokenContract.TransferBalance(InitAccount, delegatee, 30, BasicASymbol);
            _tokenContract.TransferBalance(InitAccount, delegatee, 10_00000000, SizeFeeSymbol);
            _tokenContract.TransferBalance(InitAccount, delegatee, 10_00000000, SizeASymbol);
            
            //add delegatee
            _tokenContract.SetAccount(delegatee);
            var delegations1 = new Dictionary<string, long>
            {
                [NativeToken] = 1000_00000000,
                [BasicFeeSymbol] = 500,
                [SizeFeeSymbol] = 100_00000000,
                [BasicASymbol] = 500,
                [SizeASymbol] = 100_00000000,
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

            var result =  _tokenContract.ExecuteMethodWithResult(TokenMethod.SetTransactionFeeDelegateInfos, new SetTransactionFeeDelegateInfosInput
            {
                DelegatorAddress = delegator.ConvertAddress(),
                DelegateInfoList = { delegateInfo1 }
            });
            
            Thread.Sleep(60 * 1000);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var balanceBefore = _tokenContract.GetUserBalance(delegatee, BasicASymbol);
           var result1 = _tokenContract.TransferBalance(delegator, InitAccount, 1_00000000);
           result1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
           
           _tokenContract.GetUserBalance(delegatee, BasicFeeSymbol).ShouldBe(80);
           _tokenContract.GetUserBalance(delegatee, BasicASymbol).ShouldBe(30);
           _tokenContract.GetUserBalance(delegatee, SizeFeeSymbol).ShouldBe(10_00000000);
           _tokenContract.GetUserBalance(delegatee, SizeASymbol).ShouldBe(10_00000000);
           _tokenContract.CallViewMethod<TransactionFeeFreeAllowancesMap>(TokenMethod.GetTransactionFeeFreeAllowances,
               delegatee.ConvertAddress()).Map[NativeToken].Map[BasicASymbol].Amount.ShouldBe(10);
           _tokenContract.CallViewMethod<TransactionFeeFreeAllowancesMap>(TokenMethod.GetTransactionFeeFreeAllowances,
               delegatee.ConvertAddress()).Map[NativeToken].Map[SizeASymbol].Amount.ShouldBeLessThan(10_00000000);
  
        }
        
        [TestMethod] 
        [DataRow(5, 5, true)]
        public void SetAndChargeFromDelegateeUpperLimitTest(long firstDelegateeAmount, long secondDelegateeAmount, bool isLastDelegateeSatisfiedWithChargeFee)
        {
            SetMethodOrSizeFee(BasicFeeSymbol, SizeFeeSymbol, 80);
            var delegator = NewAccount();
            _tokenContract.TransferBalance(InitAccount, delegator, 1_00000000);
            var ds = new List<string>();
            var sds = new Dictionary<string, List<string>>();

            //set first delegatee
            for (int i = 1; i <= firstDelegateeAmount; i++)
            {
                var d = NewAccount();
                Logger.Info($"number ({i}) delegatee ({d})");
                ds.Add(d);
            }
            
            Logger.Info(ds.Count);
            int groupNum = ds.Count / 128;
            for (int i = 0; i <= groupNum; i++)
            {
                int startIndex = i * 128;
                int endIndex = Math.Min(startIndex + 128, ds.Count);
                if (endIndex - startIndex != 0)
                {
                    var rawTransferList = new List<string>();
                    var rawSetList = new List<string>();
                    for (int j = 0; j < endIndex - startIndex; j++)
                    {
                        var d = ds[i * (endIndex - startIndex) + j];
                        var input = new TransferInput
                        {
                            To = d.ConvertAddress(),
                            Symbol = "ELF",
                            Amount = 50000000
                        };
                        var rawTx = NodeManager.GenerateRawTransaction(InitAccount, _tokenContract.ContractAddress, TokenMethod.Transfer.ToString(), input);
                        rawTransferList.Add(rawTx);

                        var setInput =
                            GenerateSetTransactionFeeDelegateInfosInput(delegator, 1000_00000000, 500_00000000, 100_00000000,
                                false);
                        var rawSetTx = NodeManager.GenerateRawTransaction(d, _tokenContract.ContractAddress, TokenMethod.SetTransactionFeeDelegateInfos.ToString(), setInput);
                        rawSetList.Add(rawSetTx);
                    }
                    var rawTransferTransactions = string.Join(",", rawTransferList);
                    Logger.Info($"raw transactions({rawTransferTransactions})");
                    var transferTransactions = NodeManager.SendTransactions(rawTransferTransactions);
                    NodeManager.CheckTransactionListResult(transferTransactions);
                
                    var rawSetTransactions = string.Join(",", rawSetList);
                    var setTransactions = NodeManager.SendTransactions(rawSetTransactions);
                    NodeManager.CheckTransactionListResult(setTransactions); 
                }
            }
            
            //set secondary delegatee
            foreach (var d in ds.Select((value, i) => new { i, value }))
            {
                sds.Add(d.value, new List<string>());
                for (int i = 1; i <= secondDelegateeAmount; i++)
                {
                    var sd = NewAccount();
                    Logger.Info($"number ({i}) secondary delegatee({sd}) of ({d.i})");
                    sds[d.value].Add(sd);
                }
                
                int groupNum1 = sds[d.value].Count / 128;
                for (int i = 0; i <= groupNum1; i++)
                {
                    int startIndex = i * 128;
                    int endIndex = Math.Min(startIndex + 128, sds[d.value].Count);
                    if ((endIndex - startIndex) != 0)
                    {
                        var rawTransferList = new List<string>();
                        var rawSetList = new List<string>();
                        for (int j = 0; j < endIndex - startIndex; j++)
                        {
                            var d1 = sds[d.value][i * (endIndex - startIndex) + j];
                            var input = new TransferInput
                            {
                                To = d1.ConvertAddress(),
                                Symbol = "ELF",
                                Amount = 50000000
                            };
                            var rawTx = NodeManager.GenerateRawTransaction(InitAccount, _tokenContract.ContractAddress, TokenMethod.Transfer.ToString(), input);
                            rawTransferList.Add(rawTx);
                        
                            var setInput =
                                GenerateSetTransactionFeeDelegateInfosInput(d.value, 1000_00000000, 500_00000000, 100_00000000,
                                    false);
                            var rawSetTx = NodeManager.GenerateRawTransaction(d1, _tokenContract.ContractAddress, TokenMethod.SetTransactionFeeDelegateInfos.ToString(), setInput);
                            rawSetList.Add(rawSetTx);

                        }
                        var rawTransferTransactions = string.Join(",", rawTransferList);
                        var transferTransactions = NodeManager.SendTransactions(rawTransferTransactions);
                        NodeManager.CheckTransactionListResult(transferTransactions);
                    
                        var rawSetTransactions = string.Join(",", rawSetList);
                        var setTransactions = NodeManager.SendTransactions(rawSetTransactions);
                        NodeManager.CheckTransactionListResult(setTransactions);
                    }
                }
            }

            Logger.Info($"({ds.Count})First delegatees");
            
            var firstDelegateeAddress = _tokenContract.CallViewMethod<GetTransactionFeeDelegateeListOutput>(TokenMethod.GetTransactionFeeDelegateeList,
                new GetTransactionFeeDelegateeListInput
                {
                    ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
                    DelegatorAddress = delegator.ConvertAddress(),
                    MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer)
                });
            Logger.Info($"({JsonConvert.SerializeObject(firstDelegateeAddress.DelegateeAddresses.Select(a => a.ToBase58()))})");
            var fd = firstDelegateeAddress.DelegateeAddresses.Last();
        
            var secDelegateeAddress = _tokenContract.CallViewMethod<GetTransactionFeeDelegateeListOutput>(TokenMethod.GetTransactionFeeDelegateeList,
                new GetTransactionFeeDelegateeListInput
                {
                    ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
                    DelegatorAddress = fd,
                    MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer)
                });
                var delegatee = secDelegateeAddress.DelegateeAddresses.Last();
            
            foreach (KeyValuePair<string, List<string>> item in sds)
            {
                var delegateeAddress = _tokenContract.CallViewMethod<GetTransactionFeeDelegateeListOutput>(TokenMethod.GetTransactionFeeDelegateeList,
                    new GetTransactionFeeDelegateeListInput
                    {
                        ContractAddress = _tokenContract.ContractAddress.ConvertAddress(),
                        DelegatorAddress = item.Key.ConvertAddress(),
                        MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer)
                    });
                Logger.Info($"({delegateeAddress.DelegateeAddresses.Count}) Secondary delegatees for ({item.Key}) are");
                Logger.Info($"({JsonConvert.SerializeObject(delegateeAddress.DelegateeAddresses.Select(a => a.ToBase58()))})");
            }

            if (isLastDelegateeSatisfiedWithChargeFee)
            {
                _tokenContract.TransferBalance(InitAccount, delegatee.ToBase58(), 80, BasicFeeSymbol);
                _tokenContract.TransferBalance(InitAccount, delegatee.ToBase58(), 10_00000000, SizeFeeSymbol);
            }
            
            Thread.Sleep(120*1000);
            Logger.Info($"({delegatee.ToBase58()}) balance before send transaction({_tokenContract.GetUserBalance(delegatee.ToBase58(), SizeFeeSymbol)})");
            Logger.Info($"({delegatee.ToBase58()}) balance before send transaction({_tokenContract.GetUserBalance(delegatee.ToBase58(), BasicFeeSymbol)})");
            Logger.Info($"({delegatee.ToBase58()}) balance before send transaction({_tokenContract.GetUserBalance(delegatee.ToBase58())})");
            _tokenContract.TransferBalance(delegator, InitAccount, 1_00000000);
            Logger.Info($"({delegatee.ToBase58()}) balance after send transaction({_tokenContract.GetUserBalance(delegatee.ToBase58(), SizeFeeSymbol)})");
            Logger.Info($"({delegatee.ToBase58()}) balance after send transaction({_tokenContract.GetUserBalance(delegatee.ToBase58(), BasicFeeSymbol)})");
            Logger.Info($"({delegatee.ToBase58()}) balance after send transaction({_tokenContract.GetUserBalance(delegatee.ToBase58())})");
        }

        private string NewAccount()
        {
            var user = NodeManager.AccountManager.NewAccount("12345678");
            Logger.Info(user);
            return user;
        }
        
        private void AddTransactionFeeDelegateInfos(string delegator, string delegatee, long nativeTokenAmount, long BasicFeeAmount, long sizeFeeAmount, bool isUnlimit)
        {
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

            var result =  _tokenContract.ExecuteMethodWithResult(TokenMethod.SetTransactionFeeDelegateInfos, new SetTransactionFeeDelegateInfosInput
            {
                DelegatorAddress = delegator.ConvertAddress(),
                DelegateInfoList = { delegateInfo1 }
            });
            
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

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
        
        private void AddTransactionFeeDelegation(string delegator, string delegatee, long nativeTokenAmount, long BasicFeeAmount, long sizeFeeAmount)
        {
            
            _tokenContract.SetAccount(delegatee);
            var delegations = new Dictionary<string, long>
            {
                [NativeToken] = nativeTokenAmount,
                [BasicFeeSymbol] = BasicFeeAmount,
                [SizeFeeSymbol] = sizeFeeAmount
            };
            var input = new SetTransactionFeeDelegationsInput()
            {
                DelegatorAddress = delegator.ConvertAddress(),
                Delegations =
                {
                    delegations
                }
            };
            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.SetTransactionFeeDelegations, input);

            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var delegation = _tokenContract.CallViewMethod<TransactionFeeDelegations>(TokenMethod.GetTransactionFeeDelegationsOfADelegatee,
                new GetTransactionFeeDelegationsOfADelegateeInput
                {
                    DelegateeAddress = delegatee.ConvertAddress(),
                    DelegatorAddress = delegator.ConvertAddress(),
                });
            delegation.Delegations[BasicFeeSymbol].ShouldBe(BasicFeeAmount);
            delegation.Delegations[SizeFeeSymbol].ShouldBe(sizeFeeAmount);
            delegation.Delegations[NativeToken].ShouldBe(nativeTokenAmount);
        }
        
        private SetTransactionFeeDelegateInfosInput GenerateSetTransactionFeeDelegateInfosInput(string delegator, long nativeTokenAmount, long BasicFeeAmount, long sizeFeeAmount, bool isUnlimit)
        {
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

            var result =  new SetTransactionFeeDelegateInfosInput
            {
                DelegatorAddress = delegator.ConvertAddress(),
                DelegateInfoList = { delegateInfo1 }
            };

            return result;
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
                        TokenSymbol = sizeFeeSymbol,
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

        protected void SetPrimaryTokenSymbol()
        {
            _tokenContract.ExecuteMethodWithResult(TokenMethod.SetPrimaryTokenSymbol,
                new SetPrimaryTokenSymbolInput
                {
                    Symbol = NativeToken
                });
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
        
        
    }
}