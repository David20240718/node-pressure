using AElf.Contracts.MultiToken;
 using AElf.CSharp.Core;
 using AElf.Standards.ACS1;
 using AElf.Types;
 using AElfChain.Common;
 using AElfChain.Common.Contracts;
 using AElfChain.Common.DtoExtension;
 using AElfChain.Common.Helpers;
 using AElfChain.Common.Managers;
 using Google.Protobuf;
 using Google.Protobuf.WellKnownTypes;
 using log4net;
 using Newtonsoft.Json;
 using Shouldly;
 
 namespace SystemContractTest;
 
 [TestClass]
 public class TokenContractFreeAllowanceTest
 {
     private ILog Logger { get; set; }
     private INodeManager NodeManager { get; set; }
     private AuthorityManager AuthorityManager { get; set; }
 
     private GenesisContract _genesisContract;
     private TokenContract _tokenContract;
     private ParliamentContract _parliament;
     private TokenContractImplContainer.TokenContractImplStub _tokenContractImpl;
 
     private string InitAccount { get; } = "zptx91dhHVJjJRxf5Wg5KAoMrDrWX6i1H2FAyKAiv2q8VZfbg";
     private string Test1 { get; } = "2ac5jcjsNsPQsinNmnfBsYfy8PJaj3LbTUJMtn51nWd4fC2s1W";
     private string Test2 { get; } = "q3JQw1YLXYz3LbQq5Joo2cCoch6af5aKxCcEa6rqi64BNsaEX";
     private string Test3 { get; } = "DBLDqxWRzEqpqrzrT98RG5UvmgeLhPCH5bFSeEqikSQRPCAf1";
 
     private static string RpcUrl { get; } = "127.0.0.1:8000";
 
     [TestInitialize]
     public void Initialize()
     {
         Log4NetHelper.LogInit("TransactionFeeTest");
         Logger = Log4NetHelper.GetLogger();
         NodeInfoHelper.SetConfig("nodes");
 
         NodeManager = new NodeManager(RpcUrl);
         AuthorityManager = new AuthorityManager(NodeManager, InitAccount);
 
         _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
         _tokenContract = _genesisContract.GetTokenContract(InitAccount);
         _parliament = _genesisContract.GetParliamentContract(InitAccount);
         _tokenContractImpl = _genesisContract.GetTokenImplStub();
         CreateSEEDToken();
     }
 
     #region Set Free Allowance
 
     // ConfigMethodFeeFreeAllowances(MethodFeeFreeAllowancesConfig) -- Old
     // GetMethodFeeFreeAllowancesConfig MethodFeeFreeAllowancesConfig -- Old
     // GetMethodFeeFreeAllowances(address) MethodFeeFreeAllowances -- Old
 
     // ConfigTransactionFeeFreeAllowances(ConfigTransactionFeeFreeAllowancesInput)
     // RemoveMethodFeeFreeAllowancesConfig(RemoveTransactionFeeFreeAllowancesConfigInput)
     // GetTransactionFeeFreeAllowances(aelf.Address) 
     // GetTransactionFeeFreeAllowancesConfig(google.protobuf.Empty)
     [TestMethod]
     public void SetFreeAllowance_OldMethod()
     {
         var threshold = 100_00000000;
         var symbol = "ELF";
         var freeAmount = 5_00000000;
         var organization = _parliament.GetGenesisOwnerAddress();
 
         var input = new MethodFeeFreeAllowancesConfig
         {
             FreeAllowances = new MethodFeeFreeAllowances
             {
                 Value =
                 {
                     new MethodFeeFreeAllowance
                     {
                         Symbol = symbol,
                         Amount = freeAmount
                     }
                 }
             },
             RefreshSeconds = 86400,
             Threshold = threshold
         };
 
         var result = AuthorityManager.ExecuteTransactionWithAuthority(_tokenContract.ContractAddress,
             "ConfigMethodFeeFreeAllowances", input,
             InitAccount, organization);
         result.Status.ShouldBe(TransactionResultStatus.Mined);
 
         var config = _tokenContract.GetMethodFeeFreeAllowancesConfig();
         config.Threshold.ShouldBe(threshold);
         config.FreeAllowances.Value.First().Amount.ShouldBe(freeAmount);
         config.FreeAllowances.Value.First().Symbol.ShouldBe(symbol);
         config.RefreshSeconds.ShouldBe(86400);
     }
 
     [TestMethod]
     public void GetFreeAllowanceConfig()
     {
         var config = _tokenContract.GetMethodFeeFreeAllowancesConfig();
         Logger.Info(config);
 
         var userA = NodeManager.NewAccount("12345678");
         var userB = NodeManager.NewAccount("12345678");
         var userC = NodeManager.NewAccount("12345678");
         var toAccount = NodeManager.NewAccount("12345678");
 
         _tokenContract.TransferBalance(InitAccount, userA, 100_00000000);
         _tokenContract.TransferBalance(InitAccount, userB, 110_00000000);
 
         var userAFreeAllowance = _tokenContract.GetMethodFeeFreeAllowances(userA);
         Logger.Info(userAFreeAllowance);
 
         var userBFreeAllowance = _tokenContract.GetMethodFeeFreeAllowances(userB);
         Logger.Info(userBFreeAllowance);
 
         var userCFreeAllowance = _tokenContract.GetMethodFeeFreeAllowances(userC);
         Logger.Info(userCFreeAllowance);
 
         _tokenContract.TransferBalance(userB, toAccount, 10000000);
 
         userBFreeAllowance = _tokenContract.GetMethodFeeFreeAllowances(userB);
         Logger.Info(userBFreeAllowance);
     }
 
     [TestMethod]
     [DataRow("ELF", 10_0000000, 600, "ELF", 1_0000000, "USDT", 1_000000, 600, "USDT", 10_00000)]
     public void SetFreeAllowanceOneByOne(string firstThresholdSymbol, long firstThreshold, long firstRefreshSeconds,
         string firstFreeSymbol, long firstFreeAmount,
         string secondThresholdSymbol, long secondThreshold, long secondRefreshSeconds,
         string secondFreeSymbol, long secondFreeAmount)
     {
         _tokenContract.CheckToken(firstThresholdSymbol, InitAccount, InitAccount);
         _tokenContract.CheckToken(firstFreeSymbol,InitAccount, InitAccount);
         _tokenContract.CheckToken(secondThresholdSymbol, InitAccount, InitAccount);
         _tokenContract.CheckToken(secondFreeSymbol, InitAccount, InitAccount);
         var config = _tokenContract.GetTransactionFeeFreeAllowancesConfig();
         var firstFreeAllowanceList = new TransactionFeeFreeAllowances
         {
             Value =
             {
                 new TransactionFeeFreeAllowance
                 {
                     Symbol = firstFreeSymbol,
                     Amount = firstFreeAmount
                 }
             }
         };
         var secondFreeAllowanceList = new TransactionFeeFreeAllowances
         {
             Value =
             {
                 new TransactionFeeFreeAllowance
                 {
                     Symbol = secondFreeSymbol,
                     Amount = secondFreeAmount
                 }
             }
         };
 
         SetOneThresholdFreeAllowance(firstThresholdSymbol, firstThreshold, firstRefreshSeconds, firstFreeAllowanceList);
         var newConfig = _tokenContract.GetTransactionFeeFreeAllowancesConfig();
         Logger.Info(newConfig);
 
         if (config.Value.Any(l => l.Symbol.Equals(firstThresholdSymbol)))
             newConfig.Value.Count.ShouldBe(config.Value.Count);
         else
             newConfig.Value.Count.ShouldBe(config.Value.Count + 1);
 
         var user1 = NodeManager.NewAccount("12345678");
         var user2 = NodeManager.NewAccount("12345678");
         var user3 = NodeManager.NewAccount("12345678");
 
         _tokenContract.TransferBalance(InitAccount, user1, firstThreshold);
         _tokenContract.TransferBalance(InitAccount, user3, firstThreshold);
 
         _tokenContract.IssueBalance(InitAccount, user2, secondThreshold, secondThresholdSymbol);
         _tokenContract.IssueBalance(InitAccount, user3, secondThreshold, secondThresholdSymbol);
 
         CheckUserFreeAllowance(user1, firstThresholdSymbol, firstThreshold, firstFreeSymbol, firstFreeAmount);
         CheckUserFreeAllowance(user3, firstThresholdSymbol, firstThreshold, firstFreeSymbol, firstFreeAmount);
 
         SetOneThresholdFreeAllowance(secondThresholdSymbol, secondThreshold, secondRefreshSeconds,
             secondFreeAllowanceList);
         var secondConfig = _tokenContract.GetTransactionFeeFreeAllowancesConfig();
         Logger.Info(secondConfig);
         if (newConfig.Value.Any(l => l.Symbol.Equals(secondThresholdSymbol)))
             secondConfig.Value.Count.ShouldBe(newConfig.Value.Count);
         else
             secondConfig.Value.Count.ShouldBe(newConfig.Value.Count + 1);
 
         CheckUserFreeAllowance(user1, firstThresholdSymbol, firstThreshold, firstFreeSymbol, firstFreeAmount);
         CheckUserFreeAllowance(user2, secondThresholdSymbol, secondThreshold, secondFreeSymbol, secondFreeAmount);
         CheckUserFreeAllowance(user3, firstThresholdSymbol, firstThreshold, firstFreeSymbol, firstFreeAmount);
         CheckUserFreeAllowance(user3, secondThresholdSymbol, secondThreshold, secondFreeSymbol, secondFreeAmount);
     }
 
     [TestMethod]
     [DataRow("ELF", 5_00000000, 1200, "ELF", 1_00000000, "USDT", 10_000000, 600, "USDT", 1_000000, true)]
     public void SetFreeAllowanceAtOnce(string firstThresholdSymbol, long firstThreshold, long firstRefreshSeconds,
         string firstFreeSymbol, long firstFreeAmount,
         string secondThresholdSymbol, long secondThreshold, long secondRefreshSeconds,
         string secondFreeSymbol, long secondFreeAmount, bool isOnlySet)
     {
         _tokenContract.CheckToken(firstThresholdSymbol, InitAccount, InitAccount);
         _tokenContract.CheckToken(firstFreeSymbol, InitAccount, InitAccount);
         _tokenContract.CheckToken(secondThresholdSymbol, InitAccount, InitAccount);
         _tokenContract.CheckToken(secondFreeSymbol, InitAccount, InitAccount);
 
         var firstFreeAllowanceList = new TransactionFeeFreeAllowances
         {
             Value =
             {
                 new TransactionFeeFreeAllowance
                 {
                     Symbol = firstFreeSymbol,
                     Amount = firstFreeAmount
                 }
             }
         };
         var secondFreeAllowanceList = new TransactionFeeFreeAllowances
         {
             Value =
             {
                 new TransactionFeeFreeAllowance
                 {
                     Symbol = secondFreeSymbol,
                     Amount = secondFreeAmount
                 }
             }
         };
 
         SetTwoThresholdFreeAllowance(firstThresholdSymbol, firstThreshold, firstRefreshSeconds, firstFreeAllowanceList,
             secondThresholdSymbol, secondThreshold, secondRefreshSeconds, secondFreeAllowanceList);
 
         if (!isOnlySet)
         {
             var user1 = NodeManager.NewAccount("12345678");
             var user2 = NodeManager.NewAccount("12345678");
             var user3 = NodeManager.NewAccount("12345678");
 
             _tokenContract.TransferBalance(InitAccount, user1, firstThreshold);
             _tokenContract.TransferBalance(InitAccount, user3, firstThreshold);
 
             _tokenContract.IssueBalance(InitAccount, user2, secondThreshold, secondThresholdSymbol);
             _tokenContract.IssueBalance(InitAccount, user3, secondThreshold, secondThresholdSymbol);
 
             CheckUserFreeAllowance(user1, firstThresholdSymbol, firstThreshold, firstFreeSymbol, firstFreeAmount);
             CheckUserFreeAllowance(user2, secondThresholdSymbol, secondThreshold, secondFreeSymbol, secondFreeAmount);
             CheckUserFreeAllowance(user3, firstThresholdSymbol, firstThreshold, firstFreeSymbol, firstFreeAmount);
             CheckUserFreeAllowance(user3, secondThresholdSymbol, secondThreshold, secondFreeSymbol, secondFreeAmount);
         }
     }
 
     [TestMethod]
     [DataRow("ELF", 1_00000000, 1200, "ELF", 1_00000000, "ABC", 1_00000000, "USDT", 1_000000, 600, "ABC", 1_00000000,
         "USDT", 1_000000, false)]
     public void SetFreeAllowanceTwoFreeAllowanceAtOnce(string firstThresholdSymbol, long firstThreshold,
         long firstRefreshSeconds,
         string firstFreeSymbol1, long firstFreeAmount1, string secondFreeSymbol1, long secondFreeAmount1,
         string secondThresholdSymbol, long secondThreshold, long secondRefreshSeconds,
         string firstFreeSymbol2, long firstFreeAmount2, string secondFreeSymbol2, long secondFreeAmount2,
         bool isOnlySet)
     {
         _tokenContract.CheckToken(firstThresholdSymbol, InitAccount, InitAccount);
         _tokenContract.CheckToken(firstFreeSymbol1, InitAccount, InitAccount);
         _tokenContract.CheckToken(secondFreeSymbol1, InitAccount, InitAccount);
 
         _tokenContract.CheckToken(secondThresholdSymbol, InitAccount, InitAccount);
         _tokenContract.CheckToken(firstFreeSymbol2, InitAccount, InitAccount);
         _tokenContract.CheckToken(secondFreeSymbol2, InitAccount, InitAccount);
 
         var firstFreeAllowanceList = new TransactionFeeFreeAllowances
         {
             Value =
             {
                 new TransactionFeeFreeAllowance
                 {
                     Symbol = firstFreeSymbol1,
                     Amount = firstFreeAmount1
                 },
                 new TransactionFeeFreeAllowance
                 {
                     Symbol = secondFreeSymbol1,
                     Amount = secondFreeAmount1
                 }
             }
         };
         var secondFreeAllowanceList = new TransactionFeeFreeAllowances
         {
             Value =
             {
                 new TransactionFeeFreeAllowance
                 {
                     Symbol = firstFreeSymbol2,
                     Amount = firstFreeAmount2
                 },
                 new TransactionFeeFreeAllowance
                 {
                     Symbol = secondFreeSymbol2,
                     Amount = secondFreeAmount2
                 }
             }
         };
 
         SetTwoThresholdFreeAllowance(firstThresholdSymbol, firstThreshold, firstRefreshSeconds, firstFreeAllowanceList,
             secondThresholdSymbol, secondThreshold, secondRefreshSeconds, secondFreeAllowanceList);
 
         if (!isOnlySet)
         {
             var user1 = NodeManager.NewAccount("12345678");
             var user2 = NodeManager.NewAccount("12345678");
             var user3 = NodeManager.NewAccount("12345678");
 
             _tokenContract.TransferBalance(InitAccount, user1, firstThreshold);
             _tokenContract.TransferBalance(InitAccount, user3, firstThreshold);
 
             _tokenContract.IssueBalance(InitAccount, user2, secondThreshold, secondThresholdSymbol);
             _tokenContract.IssueBalance(InitAccount, user3, secondThreshold, secondThresholdSymbol);
         }
     }
 
     [TestMethod]
     public void ModifyFreeAllowance(string modifyType, string changeThresholdSymbol, long newRefreshSecond,
         long newThreshold, string newFreeSymbol, long newFreeAmount)
     {
         var config = _tokenContract.GetTransactionFeeFreeAllowancesConfig();
         Logger.Info(config);
         if (!config.Value.Any())
             SetDefaultConfig();
         var changeSymbolInfo = config.Value.First(l => l.Symbol.Equals(changeThresholdSymbol));
         if (changeSymbolInfo.Equals(new TransactionFeeFreeAllowanceConfig())) return;
         var freeAllowanceList = new TransactionFeeFreeAllowances
         {
             Value = { changeSymbolInfo.FreeAllowances.Map.Values }
         };
         switch (modifyType)
         {
             case "RefreshSecond":
                 SetOneThresholdFreeAllowance(changeThresholdSymbol, changeSymbolInfo.Threshold, newRefreshSecond,
                     freeAllowanceList);
                 break;
             case "Threshold":
                 SetOneThresholdFreeAllowance(changeThresholdSymbol, newThreshold, changeSymbolInfo.RefreshSeconds,
                     freeAllowanceList);
                 break;
             case "FreeFeeAllowance":
                 var newFreeAllowance = new TransactionFeeFreeAllowances
                 {
                     Value =
                     {
                         new TransactionFeeFreeAllowance
                         {
                             Symbol = newFreeSymbol,
                             Amount = newFreeAmount
                         }
                     }
                 };
                 SetOneThresholdFreeAllowance(changeThresholdSymbol, changeSymbolInfo.Threshold,
                     changeSymbolInfo.RefreshSeconds,
                     newFreeAllowance);
                 break;
         }
 
         config = _tokenContract.GetTransactionFeeFreeAllowancesConfig();
         Logger.Info(config);
     }
 
     [TestMethod]
     public void RemoveMethodFeeFreeAllowancesConfig()
     {
         var config = _tokenContract.GetTransactionFeeFreeAllowancesConfig();
         Logger.Info(config);
         var tokenList = config.Value.Select(c => c.Symbol).ToList();
         
         var user1 = NodeManager.NewAccount("12345678");
         var user2 = NodeManager.NewAccount("12345678");
         var user3 = NodeManager.NewAccount("12345678");
         
         _tokenContract.TransferBalance(InitAccount, user1, config.Value.First().Threshold, config.Value.First().Symbol);
         _tokenContract.TransferBalance(InitAccount, user3, config.Value.First().Threshold, config.Value.First().Symbol);
         
         _tokenContract.IssueBalance(InitAccount, user2, config.Value.Last().Threshold, config.Value.Last().Symbol);
         _tokenContract.IssueBalance(InitAccount, user3, config.Value.Last().Threshold, config.Value.Last().Symbol);
         
         
         foreach (var symbol in tokenList)
         {
             GetUserFreeAllowance(user1, symbol);
             GetUserFreeAllowance(user2, symbol);
             GetUserFreeAllowance(user3, symbol);
             GetUserFreeAllowance(InitAccount, symbol);
         }
         
         RemoveConfigTransactionFeeFreeAllowances(tokenList);
 
         foreach (var symbol in tokenList)
         {
             GetUserFreeAllowance(user1, symbol);
             GetUserFreeAllowance(user2, symbol);
             GetUserFreeAllowance(user3, symbol);
         }
     }
 
 
     [TestMethod]
     [DataRow("2ZvsThBd7kByBDa4yrBgRTvzFRAubT3yaEWmL2qrfznNykCDbu",
         "ei8wbUHLrHiSYV71LZ3nXB9i2ucNgig8TWNDngM1E4HBDpPsC",
         "V6LuP6FXKPoXqR5V9X2XuufZ8wSwKu4kNJbxY8i9JJ4NDPxib",
         "FreeFeeAllowance", "ELF", 0, 1_00000000, "USDT", 1000000)]
     public void TransferThroughFreeAllowanceWithModifyConfig(string user1, string user2, string user3,
         string modifyType, string changeThresholdSymbol, long newRefreshSecond,
         long newThreshold, string newFreeSymbol, long newFreeAmount)
     {
         var testSymbol = "TEST";
         var transferAmount = 1_0000000;
 
         _tokenContract.CheckToken(testSymbol, InitAccount, InitAccount);
         CheckBalance(user1, testSymbol, transferAmount, "Issue");
         CheckBalance(user2, testSymbol, transferAmount, "Issue");
         CheckBalance(user3, testSymbol, transferAmount, "Issue");
 
         var config = _tokenContract.GetTransactionFeeFreeAllowancesConfig();
         Logger.Info(config);
 
         var thresholdList = config.Value.Select(l => l.Symbol).ToList();
         foreach (var symbol in thresholdList)
         {
             GetUserFreeAllowance(user1, symbol);
             GetUserFreeAllowance(user2, symbol);
             GetUserFreeAllowance(user3, symbol);
         }
 
         var toAccount = NodeManager.NewAccount("12345678");
 
         _tokenContract.TransferBalance(user1, toAccount, transferAmount, testSymbol);
         _tokenContract.TransferBalance(user2, toAccount, transferAmount, testSymbol);
         _tokenContract.TransferBalance(user3, toAccount, transferAmount, testSymbol);
 
         foreach (var symbol in thresholdList)
         {
             GetUserFreeAllowance(user1, symbol);
             GetUserFreeAllowance(user2, symbol);
             GetUserFreeAllowance(user3, symbol);
         }
 
         ModifyFreeAllowance(modifyType, changeThresholdSymbol, newRefreshSecond, newThreshold, newFreeSymbol,
             newFreeAmount);
 
         foreach (var symbol in thresholdList)
         {
             GetUserFreeAllowance(user1, symbol);
             GetUserFreeAllowance(user2, symbol);
             GetUserFreeAllowance(user3, symbol);
         }
 
         _tokenContract.TransferBalance(user1, toAccount, transferAmount, testSymbol);
         _tokenContract.TransferBalance(user2, toAccount, transferAmount, testSymbol);
         _tokenContract.TransferBalance(user3, toAccount, transferAmount, testSymbol);
 
         foreach (var symbol in thresholdList)
         {
             GetUserFreeAllowance(user1, symbol);
             GetUserFreeAllowance(user2, symbol);
             GetUserFreeAllowance(user3, symbol);
         }
     }
 
     //     [DataRow("2ZvsThBd7kByBDa4yrBgRTvzFRAubT3yaEWmL2qrfznNykCDbu",
     // "ei8wbUHLrHiSYV71LZ3nXB9i2ucNgig8TWNDngM1E4HBDpPsC",
     // "V6LuP6FXKPoXqR5V9X2XuufZ8wSwKu4kNJbxY8i9JJ4NDPxib",
     [TestMethod]
     public void TransferThroughFreeAllowance()
     {
         var testSymbol = "TT";
         var transferAmount = 1_0000000;
 
         var user = NodeManager.NewAccount("12345678");
         var toAccount = NodeManager.NewAccount("12345678");

         _tokenContract.TransferBalance(InitAccount, user, 500000000, "ELF");
         _tokenContract.TransferBalance(InitAccount, user, 10000000, "USDT");

         _tokenContract.CheckToken(testSymbol, InitAccount, InitAccount);
         CheckBalance(user, testSymbol, transferAmount, "Transfer");
 
         var config = _tokenContract.GetTransactionFeeFreeAllowancesConfig();
         Logger.Info(config);
 
         var thresholdList = config.Value.Select(l => l.Symbol).ToList();
         foreach (var symbol in thresholdList)
         {
             GetUserFreeAllowance(user, symbol);
         }
 
         _tokenContract.TransferBalance(user, toAccount, transferAmount, testSymbol);
 
         foreach (var symbol in thresholdList)
         {
             GetUserFreeAllowance(user, symbol);
         }
     }
 
     [TestMethod]
     public void FreeAllowance_NotClear()
     {
         var config = _tokenContract.GetTransactionFeeFreeAllowancesConfig();
         Logger.Info(config);
         var freeAllowanceConfig = config.Value.First();
         var thresholdSymbol = freeAllowanceConfig.Symbol;
         var threshold = freeAllowanceConfig.Threshold;
         var freeSymbol = freeAllowanceConfig.FreeAllowances.Map.Values.First().Symbol;
         var freeAmount = freeAllowanceConfig.FreeAllowances.Map.Values.First().Amount;
 
         var user = "GNPFHv87x9j6ch8jycuKZx4kYz8Si9E4f2EqxcyBKSAwSMmRT";
         var toAccount = "D2j2VvFWFF9Wbxv7DhGtcWUp4aYkMK4zREbj1GTNZv8vqZcz2";
 
         // _tokenContract.TransferBalance(InitAccount, user, threshold, thresholdSymbol);
         GetUserFreeAllowance(user, thresholdSymbol);
 
         // Executed transaction 
         var testSymbol = "TT";
         var transferAmount = 1_00000;
         _tokenContract.CheckToken(testSymbol, InitAccount, InitAccount);
         CheckBalance(user, testSymbol, transferAmount, "Transfer");
         _tokenContract.TransferBalance(user, toAccount, transferAmount.Div(4), testSymbol);
         var userFreeAllowance = GetUserFreeAllowance(user, thresholdSymbol);
         userFreeAllowance.Map.Values.First().Map.Values.First().Symbol.ShouldBe(freeSymbol);
         userFreeAllowance.Map.Values.First().Map.Values.First().Amount.ShouldBeLessThan(freeAmount);
         var fee = freeAmount - userFreeAllowance.Map.Values.First().Map.Values.First().Amount;
         Logger.Info($"Should charge {fee}");
 
         // Reduce balance
         _tokenContract.TransferBalance(user, toAccount, threshold + 100000000, thresholdSymbol);
         var reduceBalance = _tokenContract.GetUserBalance(user, thresholdSymbol);
         Logger.Info($"After reduce balance {reduceBalance}");
         GetUserFreeAllowance(user, thresholdSymbol);
 
         //  Executed transaction 
         var txResult = _tokenContract.TransferBalance(user, toAccount, transferAmount.Div(4) + 1, testSymbol);
         var txFee = txResult.GetDefaultTransactionFee();
         Logger.Info($"Charge {txFee}");
 
         // Increase balance
         _tokenContract.TransferBalance(InitAccount, user, threshold + 1, thresholdSymbol);
         var thresholdBalance = _tokenContract.GetUserBalance(user, thresholdSymbol);
         Logger.Info($"After increase balance {thresholdBalance}");
 
         var restoredUserFreeAllowance = GetUserFreeAllowance(user, thresholdSymbol);
         restoredUserFreeAllowance.Map.Values.First().Map.Values.First().Symbol.ShouldBe(freeSymbol);
         restoredUserFreeAllowance.Map.Values.First().Map.Values.First().Amount
             .ShouldBeLessThan(userFreeAllowance.Map.Values.First().Map.Values.First().Amount);
 
         //  Executed transaction 
         _tokenContract.TransferBalance(user, toAccount, transferAmount.Div(4) + 2, testSymbol);
         var newUserFreeAllowance = GetUserFreeAllowance(user, thresholdSymbol);
         newUserFreeAllowance.Map.Values.First().Map.Values.First().Symbol.ShouldBe(freeSymbol);
         newUserFreeAllowance.Map.Values.First().Map.Values.First().Amount
             .ShouldBeLessThan(restoredUserFreeAllowance.Map.Values.First().Map.Values.First().Amount);
         fee = restoredUserFreeAllowance.Map.Values.First().Map.Values.First().Amount -
               newUserFreeAllowance.Map.Values.First().Map.Values.First().Amount;
         Logger.Info($"Should charge {fee}");
     }
 
     [TestMethod]
     public void FreeAllowance_Priority()
     {
         var sizeFeeSymbol = "USDT";
         var thresholdSymbol = "ELF";
         var threshold = 100000000;
         var refreshSecond = 600;
 
         var user = NodeManager.NewAccount("12345678");
         var toAddress = NodeManager.NewAccount("12345678");
 
         _tokenContract.CheckToken(sizeFeeSymbol, InitAccount, InitAccount);
         SetSizeFee(sizeFeeSymbol, 1, 10, false);
         CleanConfig();
 
         var freeAllowanceList = new TransactionFeeFreeAllowances
         {
             Value =
             {
                 new TransactionFeeFreeAllowance
                 {
                     Symbol = sizeFeeSymbol,
                     Amount = 10_000000
                 }
             }
         };
         SetOneThresholdFreeAllowance(thresholdSymbol, threshold, refreshSecond, freeAllowanceList);
 
         _tokenContract.TransferBalance(InitAccount, user, threshold, thresholdSymbol);
         CheckUserFreeAllowance(user);
 
         _tokenContract.ApproveToken(user, toAddress, 1000000000, "ELF");
         CheckUserFreeAllowance(user);
         var userBalance = _tokenContract.GetUserBalance(user);
         Logger.Info(userBalance);
     }
 
     [TestMethod]
     public void MultiFreeAllowance()
     {
         var baseFeeSymbol = "USDT";
         var baseFeeAmount = 1_000000;
         var baseFeeList = new List<MethodFee>
         {
             new()
             {
                 Symbol = baseFeeSymbol,
                 BasicFee = baseFeeAmount
             }
         };
 
         var user = NodeManager.NewAccount("12345678");
         var toAddress = NodeManager.NewAccount("12345678");
 
         TransferUserForGettingFreeAllowance(user);
         _tokenContract.CheckToken(baseFeeSymbol, InitAccount, InitAccount);
         RemoveSizeFee();
         SetBaseFee("Approve", false, baseFeeList);
 
         CheckUserFreeAllowance(user);
         var result = _tokenContract.ApproveToken(user, toAddress, 1000000000, "ELF");
         result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
         CheckUserFreeAllowance(user);
     }
 
     [TestMethod]
     public void MultiFreeAllowance_Priority_1()
     {
         var feeSymbol = "USDT";
         var baseFeeAmount = 2_000000;
         var elfBaseFeeAmount = 2_00000000;
 
         var baseFeeList = new List<MethodFee>
         {
             new()
             {
                 Symbol = feeSymbol,
                 BasicFee = baseFeeAmount
             },
             new()
             {
                 Symbol = "ELF",
                 BasicFee = elfBaseFeeAmount
             }
         };
 
         var user = NodeManager.NewAccount("12345678");
         var toAddress = NodeManager.NewAccount("12345678");
 
         TransferUserForGettingFreeAllowance(user);
         _tokenContract.CheckToken(feeSymbol, InitAccount, InitAccount);
         SetSizeFee(feeSymbol, 10, 1, false);
         SetBaseFee("Approve", false, baseFeeList);
 
         CheckUserFreeAllowance(user);
         var result = _tokenContract.ApproveToken(user, toAddress, 1000000000, "ELF");
         result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
         CheckUserFreeAllowance(user);
     }
 
     [TestMethod]
     public void MultiFreeAllowance_Priority_2()
     {
         var feeSymbol = "USDT";
         var baseFeeAmount = 2_000000;
         var elfBaseFeeAmount = 1_00000000;
 
         var user = NodeManager.NewAccount("12345678");
         var toAddress = NodeManager.NewAccount("12345678");
 
         var baseFeeList = new List<MethodFee>
         {
             new()
             {
                 Symbol = "ELF",
                 BasicFee = elfBaseFeeAmount
             },
             new()
             {
                 Symbol = feeSymbol,
                 BasicFee = baseFeeAmount
             }
         };
 
         TransferUserForGettingFreeAllowance(user);
         _tokenContract.CheckToken(feeSymbol, InitAccount, InitAccount);
         SetSizeFee(feeSymbol, 1, 10, false);
         SetBaseFee("Approve", false, baseFeeList);
 
         CheckUserFreeAllowance(user);
         var result = _tokenContract.ApproveToken(user, toAddress, 1000000000, "ELF");
         result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
         CheckUserFreeAllowance(user);
     }
 
     [TestMethod]
     [DataRow("ELF", 4_00000000)]
     [DataRow("ELF", 2_00000000)]
     public void MultiFreeAllowance_OnlyOneBaseFee_FreeAllowanceNotEnough(string feeSymbol, long baseFeeAmount)
     {
         var user = NodeManager.NewAccount("12345678");
         var toAddress = NodeManager.NewAccount("12345678");
 
         var baseFeeList = new List<MethodFee>
         {
             new()
             {
                 Symbol = feeSymbol,
                 BasicFee = baseFeeAmount
             }
         };
 
         TransferUserForGettingFreeAllowance(user);
         _tokenContract.CheckToken(feeSymbol, InitAccount, InitAccount);
         RemoveSizeFee();
         SetBaseFee("Approve", false, baseFeeList);
 
         CheckUserFreeAllowance(user);
         var result = _tokenContract.ApproveToken(user, toAddress, 1000000000, "ELF");
         result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
         CheckUserFreeAllowance(user);
     }
 
     [TestMethod]
     [DataRow(10, 1, 2_000000, 1_00000000)]
     [DataRow(1, 3, 11_000000, 1_50000000)]
     [DataRow(1, 2, 12_000000, 1_50000000)]
     public void MultiFreeAllowance_FreeAllowanceNotEnough(int addedWeight, int baseWeight, long baseFeeAmount,
         long elfBaseFeeAmount)
     {
         var feeSymbol = "USDT";
         var user = NodeManager.NewAccount("12345678");
         var toAddress = NodeManager.NewAccount("12345678");
 
         var baseFeeList = new List<MethodFee>
         {
             new()
             {
                 Symbol = feeSymbol,
                 BasicFee = baseFeeAmount
             },
             new()
             {
                 Symbol = "ELF",
                 BasicFee = elfBaseFeeAmount
             }
         };
 
         TransferUserForGettingFreeAllowance(user);
         _tokenContract.CheckToken(feeSymbol, InitAccount, InitAccount);
         SetSizeFee(feeSymbol, addedWeight, baseWeight, false);
         SetBaseFee("Approve", false, baseFeeList);
 
         CheckUserFreeAllowance(user);
         var result = _tokenContract.ApproveToken(user, toAddress, 1000000000, "ELF");
         result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
         CheckUserFreeAllowance(user);
     }
 
     [TestMethod]
     [DataRow(1, 3, 11_000000, 1_50000000)]
     public void MultiFreeAllowance_Check(int addedWeight, int baseWeight, long baseFeeAmount, long elfBaseFeeAmount)
     {
         var feeSymbol = "USDT";
         var user = NodeManager.NewAccount("12345678");
         var toAddress = NodeManager.NewAccount("12345678");
 
         var baseFeeList = new List<MethodFee>
         {
             new MethodFee()
             {
                 Symbol = "ELF",
                 BasicFee = elfBaseFeeAmount
             },
             new MethodFee()
             {
                 Symbol = feeSymbol,
                 BasicFee = baseFeeAmount
             }
         };
 
         _tokenContract.TransferBalance(InitAccount, user, 10_00000000);
         // _tokenContract.CheckToken(feeSymbol, InitAccount, InitAccount);
         // SetSizeFee(feeSymbol, addedWeight, baseWeight, false);
         RemoveSizeFee();
         var elfBalance = _tokenContract.GetUserBalance(user);
         var usdtBalance = _tokenContract.GetUserBalance(user, "USDT");
         Logger.Info(elfBalance);
         Logger.Info(usdtBalance);
 
 
         var result = _tokenContract.ApproveToken(user, toAddress, 1000000000, "ELF");
         result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
         elfBalance = _tokenContract.GetUserBalance(user);
         usdtBalance = _tokenContract.GetUserBalance(user, "USDT");
         Logger.Info(elfBalance);
         Logger.Info(usdtBalance);
     }
 
     [TestMethod]
     [DataRow("ABC", 1, 10, "ABC", "USDT", 1_00000000, 2_000000)]
     public void MultiFreeAllowance_DiffFeeSymbol_FreeAllowanceNotEnough(string sizeFeeSymbol, int addedWeight,
         int baseWeight, string baseFeeSymbol, string baseFeeSymbol2, long baseFeeAmount, long baseFeeAmount2)
     {
         var user = NodeManager.NewAccount("12345678");
         var toAddress = NodeManager.NewAccount("12345678");
 
         var baseFeeList = new List<MethodFee>
         {
             new()
             {
                 Symbol = baseFeeSymbol,
                 BasicFee = baseFeeAmount
             },
             new()
             {
                 Symbol = baseFeeSymbol2,
                 BasicFee = baseFeeAmount2
             },
         };
 
         TransferUserForGettingFreeAllowance(user);
         _tokenContract.CheckToken(baseFeeSymbol, InitAccount, InitAccount);
         _tokenContract.CheckToken(baseFeeSymbol2, InitAccount, InitAccount);
         _tokenContract.CheckToken(sizeFeeSymbol, InitAccount, InitAccount);
         CheckBalance(user, baseFeeSymbol, 2_00000000, "Issue");
 
         SetSizeFee(sizeFeeSymbol, addedWeight, baseWeight, false);
         SetBaseFee("Approve", false, baseFeeList);
 
         CheckUserFreeAllowance(user);
         var result = _tokenContract.ApproveToken(user, toAddress, 1000000000, "ELF");
         result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
         CheckUserFreeAllowance(user);
     }
 
     [TestMethod]
     [DataRow(3_000000, 2_00000000)]
     public void SameTokenFreeAllowance_FreeAllowanceNotEnough(long baseFeeAmount, long elfBaseFeeAmount)
     {
         var feeSymbol = "USDT";
         var user = NodeManager.NewAccount("12345678");
         var toAddress = NodeManager.NewAccount("12345678");
 
 
         var baseFeeList = new List<MethodFee>
         {
             new()
             {
                 Symbol = feeSymbol,
                 BasicFee = baseFeeAmount
             },
             new()
             {
                 Symbol = "ELF",
                 BasicFee = elfBaseFeeAmount
             }
         };
 
         _tokenContract.CheckToken(feeSymbol, InitAccount, InitAccount);
         SetBaseFee("Approve", false, baseFeeList);
 
         TransferUserForGettingFreeAllowance(user);
         CheckUserFreeAllowance(user);
         var result = _tokenContract.ApproveToken(user, toAddress, 1000000000, "ELF");
         result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
         CheckUserFreeAllowance(user);
     }
 
     [TestMethod]
     [DataRow("BcvWP4F1qbRPgwnsXMLQseUR9GvCQZMRkXAajafxkWPaEVfz8")]
     [DataRow("2dAzGzs92ESJAccELpkD4vtStZN8DTKKYYkdNAWdbfdiKrRFT7")]
     public void CheckUserFreeAllowance(string user)
     {
         var config = _tokenContract.GetTransactionFeeFreeAllowancesConfig();
         Logger.Info(config);
         var thresholdList = config.Value.Select(l => l.Symbol).ToList();
 
         foreach (var symbol in thresholdList)
         {
             GetUserFreeAllowance(user, symbol);
         }
     }
 
 
     [TestMethod]
     public void CheckFreeAllowance()
     {
         var newAccount = NodeManager.NewAccount("12345678");
         var testAccount = NodeManager.NewAccount("12345678");
         _tokenContract.TransferBalance(InitAccount, newAccount, 100_00000000);
         _tokenContract.TransferBalance(InitAccount, newAccount, 1000_00000000, "ABC");
         var elfBalance = _tokenContract.GetUserBalance(newAccount);
         var testBalance = _tokenContract.GetUserBalance(newAccount, "TEST");
         var abcBalance = _tokenContract.GetUserBalance(newAccount, "ABC");
 
         var beforeFreeAllowance = _tokenContract.GetMethodFeeFreeAllowances(newAccount);
         Logger.Info(beforeFreeAllowance);
         var result = _tokenContract.TransferBalance(newAccount, testAccount, 1000000000, "ABC");
 
         var eventLogs = result.Logs;
         if (eventLogs.Any(n => n.Name.Equals(nameof(TransactionFeeCharged))))
         {
             var charged = eventLogs.First(n => n.Name.Equals(nameof(TransactionFeeCharged)));
             var fee = TransactionFeeCharged.Parser.ParseFrom(
                 ByteString.FromBase64(charged.NonIndexed));
             Logger.Info($"fee: {fee}");
         }
 
         var afterFreeAllowance = _tokenContract.GetMethodFeeFreeAllowances(newAccount);
         Logger.Info(afterFreeAllowance);
         var afterElfBalance = _tokenContract.GetUserBalance(newAccount);
         var afterTestBalance = _tokenContract.GetUserBalance(newAccount, "TEST");
         var afterAbcBalance = _tokenContract.GetUserBalance(newAccount, "ABC");
 
 
         Logger.Info($"{elfBalance} {testBalance} {abcBalance}\n" +
                     $"{afterElfBalance} {afterTestBalance} {afterAbcBalance}");
     }
 
     [TestMethod]
     public void FreeAllowance_NotEnough()
     {
         var newAccount = NodeManager.NewAccount("12345678");
         var toAccount = NodeManager.NewAccount("12345678");
         _tokenContract.TransferBalance(InitAccount, newAccount, 102_00000000);
         var elfBalance = _tokenContract.GetUserBalance(newAccount);
         var beforeFreeAllowance = _tokenContract.GetMethodFeeFreeAllowances(newAccount);
         Logger.Info(beforeFreeAllowance);
         var result = _tokenContract.TransferBalance(newAccount, toAccount, 1_00000000);
         var eventLogs = result.Logs;
         if (eventLogs.Any(n => n.Name.Equals(nameof(TransactionFeeCharged))))
         {
             var charged = eventLogs.First(n => n.Name.Equals(nameof(TransactionFeeCharged)));
             var fee = TransactionFeeCharged.Parser.ParseFrom(
                 ByteString.FromBase64(charged.NonIndexed));
             Logger.Info($"fee: {fee}");
         }
 
         var afterFreeAllowance = _tokenContract.GetMethodFeeFreeAllowances(newAccount);
         Logger.Info(afterFreeAllowance);
         var afterElfBalance = _tokenContract.GetUserBalance(newAccount);
 
         Logger.Info($"balance: {elfBalance}\n" +
                     $"{afterElfBalance}");
 
         var transfer = _tokenContract.TransferBalance(newAccount, toAccount, 10_00000000);
         var transferLogs = transfer.Logs;
         if (transferLogs.Any(n => n.Name.Equals(nameof(TransactionFeeCharged))))
         {
             var charged = transferLogs.First(n => n.Name.Equals(nameof(TransactionFeeCharged)));
             var fee = TransactionFeeCharged.Parser.ParseFrom(
                 ByteString.FromBase64(charged.NonIndexed));
             Logger.Info($"fee: {fee}");
         }
 
         afterFreeAllowance = _tokenContract.GetMethodFeeFreeAllowances(newAccount);
         Logger.Info(afterFreeAllowance);
     }
 
     [TestMethod]
     public void FreeAllowance_notEnough()
     {
         var newAccount = NodeManager.NewAccount("12345678");
         var testAccount = NodeManager.NewAccount("12345678");
         _tokenContract.TransferBalance(InitAccount, newAccount, 10_00000000);
         _tokenContract.TransferBalance(InitAccount, newAccount, 1000_00000000, "TEST");
         var elfBalance = _tokenContract.GetUserBalance(newAccount);
         var testBalance = _tokenContract.GetUserBalance(newAccount, "TEST");
         var abcBalance = _tokenContract.GetUserBalance(newAccount, "ABC");
 
         // var beforeFreeAllowance = _tokenContract.GetMethodFeeFreeAllowances(newAccount);
         // Logger.Info(beforeFreeAllowance);
 
         var result = _tokenContract.TransferBalance(newAccount, testAccount, 1_00000000, "TEST");
         var eventLogs = result.Logs;
         if (eventLogs.Any(n => n.Name.Equals(nameof(TransactionFeeCharged))))
         {
             var charged = eventLogs.Where(n => n.Name.Equals(nameof(TransactionFeeCharged)));
             foreach (var c in charged)
             {
                 var fee = TransactionFeeCharged.Parser.ParseFrom(
                     ByteString.FromBase64(c.NonIndexed));
                 Logger.Info(fee);
             }
         }
 
         var afterFreeAllowance = _tokenContract.GetMethodFeeFreeAllowances(newAccount);
         Logger.Info(afterFreeAllowance);
         var afterElfBalance = _tokenContract.GetUserBalance(newAccount);
         var afterTestBalance = _tokenContract.GetUserBalance(newAccount, "TEST");
         var afterAbcBalance = _tokenContract.GetUserBalance(newAccount, "ABC");
 
         Logger.Info($"{elfBalance} {testBalance} {abcBalance}\n" +
                     $"{afterElfBalance} {afterTestBalance} {afterAbcBalance}");
     }
 
     [TestMethod]
     public void FreeAllowance_RestoreBalance()
     {
         var account = "2aE2aFzkGabteMiziHqFLn7txRZz2kFVJGhs4cCLHKHPD7SnB3";
         TransferUserForGettingFreeAllowance(account);
         CheckUserFreeAllowance(account);
     }
 
     #endregion
 
     #region Parallel
 
     [TestMethod]
     public void Parallel_Transfer()
     {
         var testSymbol = "TEST";
         _tokenContract.CheckToken(testSymbol, InitAccount, InitAccount);
         RemoveSizeFee();
         SetBaseFee("Transfer", false, new List<MethodFee>());
 
         var count = 4;
         var fromAddressList = new List<string>();
         var toAddressList = new List<string>();
         var config = _tokenContract.GetTransactionFeeFreeAllowancesConfig();
         var symbol1 = config.Value.First().Symbol;
         var symbol2 = config.Value.Last().Symbol;
 
         for (var i = 0; i < count; i++)
         {
             var address = NodeManager.NewAccount("12345678");
             var toAddress = NodeManager.NewAccount("12345678");
             TransferUserForGettingFreeAllowance(address, i < count.Div(2) ? symbol1 : symbol2);
             CheckBalance(address, testSymbol, 10000000000, "Issue");
             fromAddressList.Add(address);
             toAddressList.Add(toAddress);
         }
 
         var rawTransactionList = new List<string>();
 
         for (var i = 0; i < fromAddressList.Count; i++)
         {
             var input = new TransferInput
             {
                 To = Address.FromBase58(toAddressList[i]),
                 Symbol = testSymbol,
                 Amount = 10000000 * (i + 1)
             };
             var rawTransaction = NodeManager.GenerateRawTransaction(fromAddressList[i],
                 _tokenContract.ContractAddress, TokenMethod.Transfer.ToString(),
                 input);
             rawTransactionList.Add(rawTransaction);
         }
 
         var rawTransactions = string.Join(",", rawTransactionList);
         var transactions = NodeManager.SendTransactions(rawTransactions);
         Logger.Info(transactions);
         NodeManager.CheckTransactionListResult(transactions);
 
         foreach (var from in fromAddressList)
         {
             var afterFreeAllowance1 = GetUserFreeAllowance(from, symbol1);
             Logger.Info(afterFreeAllowance1);
             var afterFreeAllowance2 = GetUserFreeAllowance(from, symbol2);
             Logger.Info(afterFreeAllowance2);
 
             var afterBalance = _tokenContract.GetUserBalance(from, testSymbol);
             Logger.Info(afterBalance);
             var afterSizeFeeBalance = _tokenContract.GetUserBalance(from, symbol1);
             Logger.Info(afterSizeFeeBalance);
             var afterBaseBalance = _tokenContract.GetUserBalance(from, symbol1);
             Logger.Info(afterBaseBalance);
         }
 
         foreach (var to in toAddressList)
         {
             var afterTestBalance = _tokenContract.GetUserBalance(to, testSymbol);
             Logger.Info(afterTestBalance);
         }
     }
 
     [TestMethod]
     public void Parallel_Transfer_Old()
     {
         var testSymbol = "TEST";
         _tokenContract.CheckToken(testSymbol, InitAccount, InitAccount);
         RemoveSizeFee();
         SetBaseFee("Transfer", false, new List<MethodFee>());
 
         var count = 4;
         var fromAddressList = new List<string>();
         var toAddressList = new List<string>();
         var config = _tokenContract.GetMethodFeeFreeAllowancesConfig();
 
         for (var i = 0; i < count; i++)
         {
             var amount = config.Threshold;
             var address = NodeManager.NewAccount("12345678");
             var toAddress = NodeManager.NewAccount("12345678");
             CheckBalance(address, "ELF", amount, "Transfer");
             CheckBalance(address, testSymbol, 10000000000, "Issue");
             fromAddressList.Add(address);
             toAddressList.Add(toAddress);
         }
 
         var rawTransactionList = new List<string>();
 
         for (var i = 0; i < fromAddressList.Count; i++)
         {
             var input = new TransferInput
             {
                 To = Address.FromBase58(toAddressList[i]),
                 Symbol = testSymbol,
                 Amount = 10000000 * (i + 1)
             };
             var rawTransaction = NodeManager.GenerateRawTransaction(fromAddressList[i],
                 _tokenContract.ContractAddress, TokenMethod.Transfer.ToString(),
                 input);
             rawTransactionList.Add(rawTransaction);
         }
 
         var rawTransactions = string.Join(",", rawTransactionList);
         var transactions = NodeManager.SendTransactions(rawTransactions);
         Logger.Info(transactions);
         NodeManager.CheckTransactionListResult(transactions);
     }
 
     [TestMethod]
     [DataRow("ABC", 10000000)]
     public void Parallel_Transfer_MultiFee(string feeSymbol, long baseFeeAmount)
     {
         var testSymbol = "TEST";
         _tokenContract.CheckToken(testSymbol, InitAccount, InitAccount);
         _tokenContract.CheckToken(feeSymbol, InitAccount, InitAccount);
 
         RemoveSizeFee();
 
         var baseFeeList = new List<MethodFee>
         {
             new()
             {
                 Symbol = feeSymbol,
                 BasicFee = baseFeeAmount
             }
         };
         SetBaseFee("Transfer", false, baseFeeList);
         _tokenContract.IssueBalance(InitAccount, InitAccount, 10000000000, feeSymbol);
 
         var count = 6;
         var fromAddressList = new List<string>();
         var toAddressList = new List<string>();
         var config = _tokenContract.GetTransactionFeeFreeAllowancesConfig();
         var symbol1 = config.Value.First().Symbol;
         var symbol2 = config.Value.Last().Symbol;
 
         for (var i = 0; i < count.Div(3).Mul(2); i++)
         {
             var address = NodeManager.NewAccount("12345678");
             var toAddress = NodeManager.NewAccount("12345678");
             TransferUserForGettingFreeAllowance(address, i < count.Div(2) ? symbol1 : symbol2);
             CheckBalance(address, testSymbol, 10000000000, "Issue");
             fromAddressList.Add(address);
             toAddressList.Add(toAddress);
         }
 
         for (var i = 0; i < count.Div(3); i++)
         {
             var address = NodeManager.NewAccount("12345678");
             var toAddress = NodeManager.NewAccount("12345678");
             TransferUserForGettingFreeAllowance(address, symbol1);
             TransferUserForGettingFreeAllowance(address, symbol2);
 
             CheckBalance(address, testSymbol, 10000000000, "Issue");
             fromAddressList.Add(address);
             toAddressList.Add(toAddress);
         }
 
         var rawTransactionList = new List<string>();
 
         for (var i = 0; i < fromAddressList.Count; i++)
         {
             var input = new TransferInput
             {
                 To = Address.FromBase58(toAddressList[i]),
                 Symbol = testSymbol,
                 Amount = 10000000 * (i + 1)
             };
             var rawTransaction = NodeManager.GenerateRawTransaction(fromAddressList[i],
                 _tokenContract.ContractAddress, TokenMethod.Transfer.ToString(),
                 input);
             rawTransactionList.Add(rawTransaction);
         }
 
         var rawTransactions = string.Join(",", rawTransactionList);
         var transactions = NodeManager.SendTransactions(rawTransactions);
         Logger.Info(transactions);
         NodeManager.CheckTransactionListResult(transactions);
 
         foreach (var from in fromAddressList)
         {
             var afterFreeAllowance1 = GetUserFreeAllowance(from, symbol1);
             Logger.Info(afterFreeAllowance1);
             var afterFreeAllowance2 = GetUserFreeAllowance(from, symbol2);
             Logger.Info(afterFreeAllowance2);
 
             var afterBalance = _tokenContract.GetUserBalance(from, testSymbol);
             Logger.Info(afterBalance);
             var afterSizeFeeBalance = _tokenContract.GetUserBalance(from, symbol1);
             Logger.Info(afterSizeFeeBalance);
             var afterBaseBalance = _tokenContract.GetUserBalance(from, symbol1);
             Logger.Info(afterBaseBalance);
         }
 
         foreach (var to in toAddressList)
         {
             var afterTestBalance = _tokenContract.GetUserBalance(to, testSymbol);
             Logger.Info(afterTestBalance);
         }
     }
 
     [TestMethod]
     [DataRow("ABC", 10000000)]
     public void Parallel_Transfer_SameFrom(string feeSymbol, long baseFeeAmount)
     {
         var testSymbol = "TEST";
         _tokenContract.CheckToken(testSymbol, InitAccount, InitAccount);
         _tokenContract.CheckToken(feeSymbol, InitAccount, InitAccount);
 
         RemoveSizeFee();
 
         var baseFeeList = new List<MethodFee>
         {
             new()
             {
                 Symbol = feeSymbol,
                 BasicFee = baseFeeAmount
             }
         };
         SetBaseFee("Transfer", false, baseFeeList);
         _tokenContract.IssueBalance(InitAccount, InitAccount, 10000000000, feeSymbol);
 
         var count = 5;
         var fromAddress = NodeManager.NewAccount("12345678");
         var toAddressList = new List<string>();
         var config = _tokenContract.GetTransactionFeeFreeAllowancesConfig();
         var symbol1 = config.Value.First().Symbol;
         var symbol2 = config.Value.Last().Symbol;
 
         TransferUserForGettingFreeAllowance(fromAddress, symbol1);
         TransferUserForGettingFreeAllowance(fromAddress, symbol2);
         CheckBalance(fromAddress, testSymbol, 10000000000, "Issue");
 
         for (var i = 0; i < count; i++)
         {
             var toAddress = NodeManager.NewAccount("12345678");
             toAddressList.Add(toAddress);
         }
 
         var rawTransactionList = new List<string>();
 
         for (var i = 0; i < count; i++)
         {
             var input = new TransferInput
             {
                 To = Address.FromBase58(toAddressList[i]),
                 Symbol = testSymbol,
                 Amount = 10000000 * (i + 1)
             };
             var rawTransaction = NodeManager.GenerateRawTransaction(fromAddress,
                 _tokenContract.ContractAddress, TokenMethod.Transfer.ToString(),
                 input);
             rawTransactionList.Add(rawTransaction);
         }
 
         var rawTransactions = string.Join(",", rawTransactionList);
         var transactions = NodeManager.SendTransactions(rawTransactions);
         Logger.Info(transactions);
         NodeManager.CheckTransactionListResult(transactions);
 
         var afterFreeAllowance1 = GetUserFreeAllowance(fromAddress, symbol1);
         Logger.Info(afterFreeAllowance1);
         var afterFreeAllowance2 = GetUserFreeAllowance(fromAddress, symbol2);
         Logger.Info(afterFreeAllowance2);
 
         var afterBalance = _tokenContract.GetUserBalance(fromAddress, testSymbol);
         Logger.Info(afterBalance);
         var afterSizeFeeBalance = _tokenContract.GetUserBalance(fromAddress, symbol1);
         Logger.Info(afterSizeFeeBalance);
         var afterBaseBalance = _tokenContract.GetUserBalance(fromAddress, symbol1);
         Logger.Info(afterBaseBalance);
 
         foreach (var to in toAddressList)
         {
             var afterTestBalance = _tokenContract.GetUserBalance(to, testSymbol);
             Logger.Info(afterTestBalance);
         }
     }
 
     [TestMethod]
     [DataRow("ABC", 10000000)]
     public void NON_Parallel_Transfer_SameFrom(string feeSymbol, long baseFeeAmount)
     {
         _tokenContract.CheckToken(feeSymbol, InitAccount, InitAccount);
         RemoveSizeFee();
 
         var baseFeeList = new List<MethodFee>
         {
             new()
             {
                 Symbol = feeSymbol,
                 BasicFee = baseFeeAmount
             }
         };
         SetBaseFee("Transfer", false, baseFeeList);
         _tokenContract.IssueBalance(InitAccount, InitAccount, 10000000000, feeSymbol);
 
         var userA = NodeManager.NewAccount("12345678");
         var userB = NodeManager.NewAccount("12345678");
         var userC = NodeManager.NewAccount("12345678");

         var config = _tokenContract.GetTransactionFeeFreeAllowancesConfig();
         var symbol1 = config.Value.First().Symbol;
         var symbol2 = config.Value.Last().Symbol;
 
         TransferUserForGettingFreeAllowance(userA, symbol1);
         TransferUserForGettingFreeAllowance(userA, symbol2);
         CheckBalance(userA, "USDT", 10000000000, "Issue");
         CheckBalance(userB, feeSymbol, 10000000000, "Issue");
         CheckBalance(userC, feeSymbol, 10000000000, "Issue");
         CheckBalance(userB, "ELF", 10000000000, "Transfer");
         CheckBalance(userC, "ELF", 10000000000, "Transfer");

         var rawTransactionList = new List<string>();
         var inputA = new TransferInput
         {
             To = Address.FromBase58(userB),
             Symbol = "USDT",
             Amount = 10000000
         };
         var rawTransactionA = NodeManager.GenerateRawTransaction(userA,
             _tokenContract.ContractAddress, TokenMethod.Transfer.ToString(),
             inputA);
         rawTransactionList.Add(rawTransactionA);
 
         var inputB = new TransferInput
         {
             To = Address.FromBase58(userA),
             Symbol = feeSymbol,
             Amount = 1000000000
         };
         var rawTransactionB = NodeManager.GenerateRawTransaction(userC,
             _tokenContract.ContractAddress, TokenMethod.Transfer.ToString(),
             inputB);
         rawTransactionList.Add(rawTransactionB);
         
         var rawTransactions = string.Join(",", rawTransactionList);
         var transactions = NodeManager.SendTransactions(rawTransactions);
         Logger.Info(transactions);
         NodeManager.CheckTransactionListResult(transactions);
 
         var userList = new List<string> { userA, userB, userC };
         foreach (var user in userList)
         {
             var afterFreeAllowance1 = GetUserFreeAllowance(user, symbol1);
             Logger.Info(afterFreeAllowance1);
             var afterFreeAllowance2 = GetUserFreeAllowance(user, symbol2);
             Logger.Info(afterFreeAllowance2);
 
             var afterBalance = _tokenContract.GetUserBalance(user, feeSymbol);
             Logger.Info(afterBalance);
             var afterSizeFeeBalance = _tokenContract.GetUserBalance(user, symbol1);
             Logger.Info(afterSizeFeeBalance);
             var afterBaseBalance = _tokenContract.GetUserBalance(user, symbol1);
             Logger.Info(afterBaseBalance);
         }
     }
 
 
     [TestMethod]
     [DataRow("ABC", 10000000)]
     public void NON_Parallel_Transfer_SameFrom_OldMethod(string feeSymbol, long baseFeeAmount)
     {
         _tokenContract.CheckToken(feeSymbol, InitAccount, InitAccount);
         RemoveSizeFee();
 
         var baseFeeList = new List<MethodFee>
         {
             new()
             {
                 Symbol = feeSymbol,
                 BasicFee = baseFeeAmount
             }
         };
         SetBaseFee("Transfer", false, new List<MethodFee>());
         _tokenContract.IssueBalance(InitAccount, InitAccount, 10000000000, feeSymbol);
 
         var userA = NodeManager.NewAccount("12345678");
         var userB = NodeManager.NewAccount("12345678");
         var userC = NodeManager.NewAccount("12345678");
         var userD = NodeManager.NewAccount("12345678");
         var userE = NodeManager.NewAccount("12345678");
 
         // var config = _tokenContract.GetTransactionFeeFreeAllowancesConfig();
         // var symbol1 = config.Value.First().Symbol;
         // var symbol2 = config.Value.Last().Symbol;
         //
         // TransferUserForGettingFreeAllowance(userA, symbol1);
         // TransferUserForGettingFreeAllowance(userA, symbol2);
         // CheckBalance(userA, "USDT", 10000000000, "Issue");
         CheckBalance(userA, "ELF", 1000_00000000, "Transfer");
         CheckBalance(userA, feeSymbol, 10000_00000000, "Issue");
         CheckBalance(userB, feeSymbol, 10000000000, "Issue");
         CheckBalance(userC, feeSymbol, 10000000000, "Issue");
         CheckBalance(userB, "ELF", 10000000000, "Transfer");
         CheckBalance(userC, "ELF", 10000000000, "Transfer");
         CheckBalance(userD, feeSymbol, 10000000000, "Issue");
         CheckBalance(userE, feeSymbol, 10000000000, "Issue");
         CheckBalance(userD, "ELF", 10000000000, "Transfer");
         CheckBalance(userE, "ELF", 10000000000, "Transfer");
 
         var rawTransactionList = new List<string>();
         var inputA = new TransferInput
         {
             To = Address.FromBase58(userB),
             Symbol = feeSymbol,
             Amount = 10000000
         };
         var rawTransactionA = NodeManager.GenerateRawTransaction(userA,
             _tokenContract.ContractAddress, TokenMethod.Transfer.ToString(),
             inputA);
         rawTransactionList.Add(rawTransactionA);
 
         var inputB = new TransferInput
         {
             To = Address.FromBase58(userC),
             Symbol = feeSymbol,
             Amount = 1000000000
         };
 
         var rawTransactionB = NodeManager.GenerateRawTransaction(userA,
             _tokenContract.ContractAddress, TokenMethod.Transfer.ToString(),
             inputB);
         rawTransactionList.Add(rawTransactionB);
 
         var inputC = new TransferInput
         {
             To = Address.FromBase58(userD),
             Symbol = feeSymbol,
             Amount = 10000000
         };
         var rawTransactionC = NodeManager.GenerateRawTransaction(userA,
             _tokenContract.ContractAddress, TokenMethod.Transfer.ToString(),
             inputC);
         rawTransactionList.Add(rawTransactionC);
 
         var inputD = new TransferInput
         {
             To = Address.FromBase58(userE),
             Symbol = feeSymbol,
             Amount = 1000000000
         };
 
         var rawTransactionD = NodeManager.GenerateRawTransaction(userA,
             _tokenContract.ContractAddress, TokenMethod.Transfer.ToString(),
             inputD);
         rawTransactionList.Add(rawTransactionD);
 
         var rawTransactions = string.Join(",", rawTransactionList);
         var transactions = NodeManager.SendTransactions(rawTransactions);
         Logger.Info(transactions);
         NodeManager.CheckTransactionListResult(transactions);
     }
 
 
     [TestMethod]
     [DataRow(4, 8)]
     public void Parallel_Transfer_Delegation(long delegatorCount, long delegateeCount)
     {
         var testSymbol = "TEST";
         _tokenContract.CheckToken(testSymbol, InitAccount, InitAccount);
         RemoveSizeFee();
         SetBaseFee("Transfer", false, new List<MethodFee>());
 
         var config = _tokenContract.GetTransactionFeeFreeAllowancesConfig();
         var symbol1 = config.Value.First().Symbol;
 
         var delegatorList = new List<string>();
         var delegateeList = new List<string>();
         var toAddressList = new List<string>();
 
         for (var i = 0; i < delegatorCount; i++)
         {
             var d = NodeManager.NewAccount("12345678");
             var t = NodeManager.NewAccount("12345678");
 
             _tokenContract.IssueBalance(InitAccount, d, 100_00000000, testSymbol);
             delegatorList.Add(d);
             toAddressList.Add(t);
         }
 
         for (var i = 0; i < delegateeCount; i++)
         {
             var d = NodeManager.NewAccount("12345678");
             TransferUserForGettingFreeAllowance(d, symbol1);
             delegateeList.Add(d);
             var delegatorIndex = i.Div(2);
             var delegator = delegatorList[delegatorIndex];
             var delegatee = delegateeList[i];
             SetNewTransactionFeeDelegations_Add(delegator, delegatee, "ELF", 500000000);
         }
 
         Thread.Sleep(60000);
 
         var rawList = new List<string>();
         for (var i = 0; i < delegatorCount; i++)
         {
             var transferInput = new TransferInput
             {
                 Symbol = testSymbol,
                 Amount = 1_00000000,
                 To = Address.FromBase58(toAddressList[i])
             };
             var rawTransaction = NodeManager.GenerateRawTransaction(delegatorList[i],
                 _tokenContract.ContractAddress, TokenMethod.Transfer.ToString(),
                 transferInput);
             rawList.Add(rawTransaction);
         }
 
         var rawTransactions = string.Join(",", rawList);
         var transactions = NodeManager.SendTransactions(rawTransactions);
         Logger.Info(transactions);
         NodeManager.CheckTransactionListResult(transactions);
     }
 
     [TestMethod]
     public void NON_Parallel_Transfer_Delegation()
     {
         var testSymbol = "TEST";
         _tokenContract.CheckToken(testSymbol, InitAccount, InitAccount);
         RemoveSizeFee();
         SetBaseFee("Transfer", false, new List<MethodFee>());
 
         var config = _tokenContract.GetTransactionFeeFreeAllowancesConfig();
         var symbol1 = config.Value.First().Symbol;
 
         var count = 4;
         var d = NodeManager.NewAccount("12345678");
         var d1 = NodeManager.NewAccount("12345678");
         var d2 = NodeManager.NewAccount("12345678");
         _tokenContract.IssueBalance(InitAccount, d, 100_00000000, testSymbol);
         TransferUserForGettingFreeAllowance(d1, symbol1);
         TransferUserForGettingFreeAllowance(d2, symbol1);
 
         SetNewTransactionFeeDelegations_Add(d, d1, "ELF", 500000000);
         SetNewTransactionFeeDelegations_Add(d, d2, "ELF", 500000000);
 
 
         var toAddressList = new List<string>();
 
         for (var i = 0; i < count; i++)
         {
             var t = NodeManager.NewAccount("12345678");
             toAddressList.Add(t);
         }
 
         Thread.Sleep(60000);
 
         var rawList = new List<string>();
         for (var i = 0; i < count; i++)
         {
             var transferInput = new TransferInput
             {
                 Symbol = testSymbol,
                 Amount = 1_00000000,
                 To = Address.FromBase58(toAddressList[i])
             };
             var rawTransaction = NodeManager.GenerateRawTransaction(d,
                 _tokenContract.ContractAddress, TokenMethod.Transfer.ToString(),
                 transferInput);
             rawList.Add(rawTransaction);
         }
 
         var rawTransactions = string.Join(",", rawList);
         var transactions = NodeManager.SendTransactions(rawTransactions);
         Logger.Info(transactions);
         NodeManager.CheckTransactionListResult(transactions);
     }
 
     [TestMethod]
     public void Parallel_Transfer_Delegation_Second()
     {
         var count = 4;
         var testSymbol = "TEST";
         _tokenContract.CheckToken(testSymbol, InitAccount, InitAccount);
         RemoveSizeFee();
         SetBaseFee("Transfer", false, new List<MethodFee>());
 
         var config = _tokenContract.GetTransactionFeeFreeAllowancesConfig();
         var symbol1 = config.Value.First().Symbol;
 
         var delegatorList = new List<string>();
         var delegateeList = new List<string>();
         var delegatee2List = new List<string>();
         var toAddressList = new List<string>();
 
         for (var i = 0; i < count; i++)
         {
             var d = NodeManager.NewAccount("12345678");
             var d1 = NodeManager.NewAccount("12345678");
             var d2 = NodeManager.NewAccount("12345678");
             var t = NodeManager.NewAccount("12345678");
 
             _tokenContract.IssueBalance(InitAccount, d, 100_00000000, testSymbol);
             delegatorList.Add(d);
             delegateeList.Add(d1);
             delegatee2List.Add(d2);
             toAddressList.Add(t);
 
             TransferUserForGettingFreeAllowance(d2, symbol1);
             _tokenContract.TransferBalance(InitAccount, d1, 50000000, symbol1);
             _tokenContract.TransferBalance(InitAccount, d2, 2000000000, symbol1);
 
             SetNewTransactionFeeDelegations_Add(d, d1, "ELF", 500000000);
             SetNewTransactionFeeDelegations_Add(d1, d2, "ELF", 500000000);
         }
 
         Thread.Sleep(60000);
 
         var rawList = new List<string>();
         for (var i = 0; i < count; i++)
         {
             var transferInput = new TransferInput
             {
                 Symbol = testSymbol,
                 Amount = 100000000,
                 To = Address.FromBase58(toAddressList[i])
             };
             var rawTransaction = NodeManager.GenerateRawTransaction(delegatorList[i],
                 _tokenContract.ContractAddress, TokenMethod.Transfer.ToString(),
                 transferInput);
             rawList.Add(rawTransaction);
         }
 
         var rawTransactions = string.Join(",", rawList);
         var transactions = NodeManager.SendTransactions(rawTransactions);
         Logger.Info(transactions);
         NodeManager.CheckTransactionListResult(transactions);
     }
 
     [TestMethod]
     public void NON_Parallel_Transfer_Delegation_Second()
     {
         var count = 4;
         var testSymbol = "TEST";
         _tokenContract.CheckToken(testSymbol, InitAccount, InitAccount);
         RemoveSizeFee();
         SetBaseFee("Transfer", false, new List<MethodFee>());
 
         var config = _tokenContract.GetTransactionFeeFreeAllowancesConfig();
         var symbol1 = config.Value.First().Symbol;
 
         var toAddressList = new List<string>();
 
         var delegator = NodeManager.NewAccount("12345678");
         var delegatee1 = NodeManager.NewAccount("12345678");
         var delegatee2 = NodeManager.NewAccount("12345678");
 
         _tokenContract.IssueBalance(InitAccount, delegator, 100000000000, testSymbol);
         TransferUserForGettingFreeAllowance(delegatee2, symbol1);
         _tokenContract.TransferBalance(InitAccount, delegatee1, 50000000, symbol1);
         _tokenContract.TransferBalance(InitAccount, delegatee2, 100000000, symbol1);
 
         SetNewTransactionFeeDelegations_Add(delegator, delegatee1, "ELF", 500000000);
         SetNewTransactionFeeDelegations_Add(delegatee1, delegatee2, "ELF", 500000000);
 
         Thread.Sleep(60000);
 
 
         var rawList = new List<string>();
         for (var i = 0; i < count; i++)
         {
             var t = NodeManager.NewAccount("12345678");
             var transferInput = new TransferInput
             {
                 Symbol = testSymbol,
                 Amount = 100000000,
                 To = Address.FromBase58(t)
             };
             var rawTransaction = NodeManager.GenerateRawTransaction(delegator,
                 _tokenContract.ContractAddress, TokenMethod.Transfer.ToString(),
                 transferInput);
             rawList.Add(rawTransaction);
         }
 
         var rawTransactions = string.Join(",", rawList);
         var transactions = NodeManager.SendTransactions(rawTransactions);
         Logger.Info(transactions);
         NodeManager.CheckTransactionListResult(transactions);
     }
     
     [TestMethod]
     public void Delegator_With_OldDelegateeAndNewDelegatee_OldDelegateeConflic()
     {
         var count = 2;
         var testSymbol = "TEST";
         _tokenContract.CheckToken(testSymbol, InitAccount, InitAccount);
         RemoveSizeFee();
         SetBaseFee("Transfer", false, new List<MethodFee>());
 
         // var config = _tokenContract.GetTransactionFeeFreeAllowancesConfig();
         var symbol1 = "ELF";
         var dalegatorList = new List<string>();
 
         var delegator1 = NodeManager.NewAccount("12345678");
         var delegator2 = NodeManager.NewAccount("12345678");
         dalegatorList.Add(delegator1);
         dalegatorList.Add(delegator2);

         var delegatee1 = NodeManager.NewAccount("12345678");
         var delegatee2 = NodeManager.NewAccount("12345678");
         var delegatee3 = NodeManager.NewAccount("12345678");

         _tokenContract.IssueBalance(InitAccount, delegator1, 100000000000, testSymbol);
         _tokenContract.IssueBalance(InitAccount, delegator2, 100000000000, testSymbol);

         _tokenContract.TransferBalance(InitAccount, delegatee1, 1000000000, symbol1);
         _tokenContract.TransferBalance(InitAccount, delegatee2, 1000000000, symbol1);
         _tokenContract.TransferBalance(InitAccount, delegatee3, 1000000000, symbol1);

         SetOldTransactionFeeDelegations_Add(delegator1, delegatee1, "ELF", 500000000);
         SetNewTransactionFeeDelegations_Add(delegator1, delegatee2, "ELF", 500000000);
         SetOldTransactionFeeDelegations_Add(delegator2, delegatee1, "ELF", 500000000);
         SetNewTransactionFeeDelegations_Add(delegator2, delegatee3, "ELF", 500000000);

 
         Thread.Sleep(60000);
 
 
         var rawList = new List<string>();
         for (var i = 0; i < count; i++)
         {
             var t = NodeManager.NewAccount("12345678");
             var transferInput = new TransferInput
             {
                 Symbol = testSymbol,
                 Amount = 100000000,
                 To = Address.FromBase58(t)
             };
             var rawTransaction = NodeManager.GenerateRawTransaction(dalegatorList[i],
                 _tokenContract.ContractAddress, TokenMethod.Transfer.ToString(),
                 transferInput);
             rawList.Add(rawTransaction);
         }
 
         var rawTransactions = string.Join(",", rawList);
         var transactions = NodeManager.SendTransactions(rawTransactions);
         Logger.Info(transactions);
         NodeManager.CheckTransactionListResult(transactions);
     }
     
      // A -- B(old) + C(new) 
      // B -- D
     [TestMethod]
     public void Delegator_With_OldDelegateeAndNewDelegatee_FromUserConflic()
     {
         var count = 2;
         var testSymbol = "TEST";
         _tokenContract.CheckToken(testSymbol, InitAccount, InitAccount);
         RemoveSizeFee();
         SetBaseFee("Transfer", false, new List<MethodFee>());
 
         // var config = _tokenContract.GetTransactionFeeFreeAllowancesConfig();
         var symbol1 = "ELF";
         var dalegatorList = new List<string>();
 
         var delegator1 = NodeManager.NewAccount("12345678"); //A
         
         var delegatee1 = NodeManager.NewAccount("12345678"); // B 
         var delegatee2 = NodeManager.NewAccount("12345678"); // C
         var delegatee3 = NodeManager.NewAccount("12345678"); // D
         
         dalegatorList.Add(delegator1);
         dalegatorList.Add(delegatee1);

         _tokenContract.IssueBalance(InitAccount, delegator1, 100000000000, testSymbol);
         _tokenContract.IssueBalance(InitAccount, delegatee1, 100000000000, testSymbol);


         _tokenContract.TransferBalance(InitAccount, delegatee1, 1000000000, symbol1);
         _tokenContract.TransferBalance(InitAccount, delegatee2, 1000000000, symbol1);
         _tokenContract.TransferBalance(InitAccount, delegatee3, 1000000000, symbol1);


         SetOldTransactionFeeDelegations_Add(delegator1, delegatee1, "ELF", 500000000);
         SetNewTransactionFeeDelegations_Add(delegator1, delegatee2, "ELF", 500000000);
         SetNewTransactionFeeDelegations_Add(delegatee1, delegatee3, "ELF", 500000000);
         
 
         Thread.Sleep(60000);
 
 
         var rawList = new List<string>();
         for (var i = 0; i < count; i++)
         {
             var t = NodeManager.NewAccount("12345678");
             var transferInput = new TransferInput
             {
                 Symbol = testSymbol,
                 Amount = 100000000,
                 To = Address.FromBase58(t)
             };
             var rawTransaction = NodeManager.GenerateRawTransaction(dalegatorList[i],
                 _tokenContract.ContractAddress, TokenMethod.Transfer.ToString(),
                 transferInput);
             rawList.Add(rawTransaction);
         }
 
         var rawTransactions = string.Join(",", rawList);
         var transactions = NodeManager.SendTransactions(rawTransactions);
         Logger.Info(transactions);
         NodeManager.CheckTransactionListResult(transactions);
     }
     
     [TestMethod]
     public void Delegator_With_OldDelegateeAndNewDelegatee_NewDelegateeConflic()
     {
         var count = 2;
         var testSymbol = "TEST";
         _tokenContract.CheckToken(testSymbol, InitAccount, InitAccount);
         RemoveSizeFee();
         SetBaseFee("Transfer", false, new List<MethodFee>());
 
         // var config = _tokenContract.GetTransactionFeeFreeAllowancesConfig();
         var symbol1 = "ELF";
         var dalegatorList = new List<string>();
 
         var delegator1 = NodeManager.NewAccount("12345678");
         var delegator2 = NodeManager.NewAccount("12345678");
         dalegatorList.Add(delegator1);
         dalegatorList.Add(delegator2);

         var delegatee1 = NodeManager.NewAccount("12345678");
         var delegatee2 = NodeManager.NewAccount("12345678");
         var delegatee3 = NodeManager.NewAccount("12345678");

         _tokenContract.IssueBalance(InitAccount, delegator1, 100000000000, testSymbol);
         _tokenContract.IssueBalance(InitAccount, delegator2, 100000000000, testSymbol);

         _tokenContract.TransferBalance(InitAccount, delegatee1, 1000000000, symbol1);
         _tokenContract.TransferBalance(InitAccount, delegatee2, 1000000000, symbol1);
         _tokenContract.TransferBalance(InitAccount, delegatee3, 1000000000, symbol1);

         SetOldTransactionFeeDelegations_Add(delegator1, delegatee1, "ELF", 500000000);
         SetNewTransactionFeeDelegations_Add(delegator1, delegatee3, "ELF", 500000000);
         SetOldTransactionFeeDelegations_Add(delegator2, delegatee2, "ELF", 500000000);
         SetNewTransactionFeeDelegations_Add(delegator2, delegatee3, "ELF", 500000000);

 
         Thread.Sleep(60000);
 
 
         var rawList = new List<string>();
         for (var i = 0; i < count; i++)
         {
             var t = NodeManager.NewAccount("12345678");
             var transferInput = new TransferInput
             {
                 Symbol = testSymbol,
                 Amount = 100000000,
                 To = Address.FromBase58(t)
             };
             var rawTransaction = NodeManager.GenerateRawTransaction(dalegatorList[i],
                 _tokenContract.ContractAddress, TokenMethod.Transfer.ToString(),
                 transferInput);
             rawList.Add(rawTransaction);
         }
 
         var rawTransactions = string.Join(",", rawList);
         var transactions = NodeManager.SendTransactions(rawTransactions);
         Logger.Info(transactions);
         NodeManager.CheckTransactionListResult(transactions);
     }
     
     [TestMethod]
     public void Delegator_With_OldDelegateeWithNewTwoDelegatee_OldDelegateeConflic()
     {
         var count = 2;
         var testSymbol = "TEST";
         _tokenContract.CheckToken(testSymbol, InitAccount, InitAccount);
         RemoveSizeFee();
         SetBaseFee("Transfer", false, new List<MethodFee>());
 
         // var config = _tokenContract.GetTransactionFeeFreeAllowancesConfig();
         var symbol1 = "ELF";
         var dalegatorList = new List<string>();
 
         var delegator1 = NodeManager.NewAccount("12345678");
         var delegator2 = NodeManager.NewAccount("12345678");
         dalegatorList.Add(delegator1);
         dalegatorList.Add(delegator2);

         var delegatee1 = NodeManager.NewAccount("12345678");
         var delegatee2 = NodeManager.NewAccount("12345678");
         var delegatee3 = NodeManager.NewAccount("12345678");

         _tokenContract.IssueBalance(InitAccount, delegator1, 100000000000, testSymbol);
         _tokenContract.IssueBalance(InitAccount, delegator2, 100000000000, testSymbol);

         _tokenContract.TransferBalance(InitAccount, delegatee1, 60000000, symbol1);
         _tokenContract.TransferBalance(InitAccount, delegatee2, 1000000000, symbol1);
         _tokenContract.TransferBalance(InitAccount, delegatee3, 1000000000, symbol1);

         SetOldTransactionFeeDelegations_Add(delegator1, delegatee1, "ELF", 1_0000000);
         SetNewTransactionFeeDelegations_Add(delegatee1, delegatee2, "ELF", 5_00000000);
         SetOldTransactionFeeDelegations_Add(delegator2, delegatee1, "ELF", 1_0000000);
         SetNewTransactionFeeDelegations_Add(delegatee1, delegatee3, "ELF", 5_00000000);
         
         CheckOldDelegation(delegator1, delegatee1);
         CheckNewDelegation(delegatee1, delegatee2);
         CheckOldDelegation(delegator2, delegatee1);
         CheckNewDelegation(delegatee1, delegatee3);
 
         Thread.Sleep(60000);
         
         var rawList = new List<string>();
         for (var i = 0; i < count; i++)
         {
             var t = NodeManager.NewAccount("12345678");
             var transferInput = new TransferInput
             {
                 Symbol = testSymbol,
                 Amount = 100000000,
                 To = Address.FromBase58(t)
             };
             var rawTransaction = NodeManager.GenerateRawTransaction(dalegatorList[i],
                 _tokenContract.ContractAddress, TokenMethod.Transfer.ToString(),
                 transferInput);
             rawList.Add(rawTransaction);
         }
 
         var rawTransactions = string.Join(",", rawList);
         var transactions = NodeManager.SendTransactions(rawTransactions);
         Logger.Info(transactions);
         NodeManager.CheckTransactionListResult(transactions);

         CheckOldDelegation(delegator1, delegatee1);
         CheckNewDelegation(delegatee1, delegatee2);
         CheckOldDelegation(delegator2, delegatee1);
         CheckNewDelegation(delegatee1, delegatee3);
     }
     
     [TestMethod]
     public void Delegator_With_OldDelegateeWithNewTwoDelegatee_NewDelegateeConflic()
     {
         var count = 2;
         var testSymbol = "TEST";
         _tokenContract.CheckToken(testSymbol, InitAccount, InitAccount);
         RemoveSizeFee();
         SetBaseFee("Transfer", false, new List<MethodFee>());
 
         // var config = _tokenContract.GetTransactionFeeFreeAllowancesConfig();
         var symbol1 = "ELF";
         var dalegatorList = new List<string>();
 
         var delegator1 = NodeManager.NewAccount("12345678");
         var delegator2 = NodeManager.NewAccount("12345678");
         dalegatorList.Add(delegator1);
         dalegatorList.Add(delegator2);

         var delegatee1 = NodeManager.NewAccount("12345678");
         var delegatee2 = NodeManager.NewAccount("12345678");
         var delegatee3 = NodeManager.NewAccount("12345678");

         _tokenContract.IssueBalance(InitAccount, delegator1, 100000000000, testSymbol);
         _tokenContract.IssueBalance(InitAccount, delegator2, 100000000000, testSymbol);

         _tokenContract.TransferBalance(InitAccount, delegatee1, 60000000, symbol1);
         _tokenContract.TransferBalance(InitAccount, delegatee2, 1000000000, symbol1);
         _tokenContract.TransferBalance(InitAccount, delegatee3, 1000000000, symbol1);

         SetOldTransactionFeeDelegations_Add(delegator1, delegatee1, "ELF", 1_0000000);
         SetNewTransactionFeeDelegations_Add(delegatee1, delegatee3, "ELF", 5_00000000);
         SetOldTransactionFeeDelegations_Add(delegator2, delegatee2, "ELF", 1_0000000);
         SetNewTransactionFeeDelegations_Add(delegatee2, delegatee3, "ELF", 5_00000000);
         
         CheckOldDelegation(delegator1, delegatee1);
         CheckNewDelegation(delegatee1, delegatee3);
         CheckOldDelegation(delegator2, delegatee2);
         CheckNewDelegation(delegatee2, delegatee3);
 
         Thread.Sleep(60000);
         
         var rawList = new List<string>();
         for (var i = 0; i < count; i++)
         {
             var t = NodeManager.NewAccount("12345678");
             var transferInput = new TransferInput
             {
                 Symbol = testSymbol,
                 Amount = 100000000,
                 To = Address.FromBase58(t)
             };
             var rawTransaction = NodeManager.GenerateRawTransaction(dalegatorList[i],
                 _tokenContract.ContractAddress, TokenMethod.Transfer.ToString(),
                 transferInput);
             rawList.Add(rawTransaction);
         }
 
         var rawTransactions = string.Join(",", rawList);
         var transactions = NodeManager.SendTransactions(rawTransactions);
         Logger.Info(transactions);
         NodeManager.CheckTransactionListResult(transactions);

         CheckOldDelegation(delegator1, delegatee1);
         CheckNewDelegation(delegatee1, delegatee3);
         CheckOldDelegation(delegator2, delegatee2);
         CheckNewDelegation(delegatee2, delegatee3);
     }
 
 
     #endregion
 
     private void SetOneThresholdFreeAllowance(string thresholdSymbol, long threshold, long refreshSeconds,
         TransactionFeeFreeAllowances freeAllowanceList)
     {
         var organization = _parliament.GetGenesisOwnerAddress();
         var input = new ConfigTransactionFeeFreeAllowancesInput
         {
             Value =
             {
                 new ConfigTransactionFeeFreeAllowance
                 {
                     Symbol = thresholdSymbol,
                     TransactionFeeFreeAllowances = freeAllowanceList,
                     RefreshSeconds = refreshSeconds,
                     Threshold = threshold,
                 }
             }
         };
 
         var result = AuthorityManager.ExecuteTransactionWithAuthority(_tokenContract.ContractAddress,
             "ConfigTransactionFeeFreeAllowances", input,
             InitAccount, organization);
         result.Status.ShouldBe(TransactionResultStatus.Mined);
 
 
         var config = _tokenContract.GetTransactionFeeFreeAllowancesConfig();
         var setConfig = config.Value.First(c => c.Symbol.Equals(thresholdSymbol));
         setConfig.ShouldNotBeNull();
         setConfig.Threshold.ShouldBe(threshold);
         setConfig.FreeAllowances.Map.Values.ShouldBe(freeAllowanceList.Value);
         setConfig.RefreshSeconds.ShouldBe(refreshSeconds);
     }
 
     private void SetTwoThresholdFreeAllowance(string firstThresholdSymbol, long firstThreshold,
         long firstRefreshSeconds, TransactionFeeFreeAllowances firstFreeAllowanceList,
         string secondThresholdSymbol, long secondThreshold, long secondRefreshSeconds,
         TransactionFeeFreeAllowances secondFreeAllowanceList)
     {
         var organization = _parliament.GetGenesisOwnerAddress();
         var input = new ConfigTransactionFeeFreeAllowancesInput
         {
             Value =
             {
                 new ConfigTransactionFeeFreeAllowance
                 {
                     Symbol = firstThresholdSymbol,
                     TransactionFeeFreeAllowances = firstFreeAllowanceList,
                     RefreshSeconds = firstRefreshSeconds,
                     Threshold = firstThreshold,
                 },
                 new ConfigTransactionFeeFreeAllowance
                 {
                     Symbol = secondThresholdSymbol,
                     TransactionFeeFreeAllowances = secondFreeAllowanceList,
                     RefreshSeconds = secondRefreshSeconds,
                     Threshold = secondThreshold,
                 }
             }
         };
 
         var result = AuthorityManager.ExecuteTransactionWithAuthority(_tokenContract.ContractAddress,
             "ConfigTransactionFeeFreeAllowances", input,
             InitAccount, organization);
         result.Status.ShouldBe(TransactionResultStatus.Mined);
 
         var config = _tokenContract.GetTransactionFeeFreeAllowancesConfig();
         config.Value.First().Symbol.ShouldBe(firstThresholdSymbol);
         config.Value.First().Threshold.ShouldBe(firstThreshold);
         config.Value.First().RefreshSeconds.ShouldBe(firstRefreshSeconds);
         config.Value.First().FreeAllowances.Map.First().Value.ShouldBe(firstFreeAllowanceList.Value.First());
         config.Value.First().FreeAllowances.Map.Last().Value.ShouldBe(firstFreeAllowanceList.Value.Last());
 
         config.Value.Last().Symbol.ShouldBe(secondThresholdSymbol);
         config.Value.Last().Threshold.ShouldBe(secondThreshold);
         config.Value.Last().RefreshSeconds.ShouldBe(secondRefreshSeconds);
         config.Value.Last().FreeAllowances.Map.First().Value.ShouldBe(secondFreeAllowanceList.Value.First());
         config.Value.Last().FreeAllowances.Map.Last().Value.ShouldBe(secondFreeAllowanceList.Value.Last());
         Logger.Info(config);
     }
 
     private void SetDefaultConfig()
     {
         var firstThresholdSymbol = "ELF";
         var firstThreshold = 10_00000000;
         var firstRefreshSeconds = 600;
         var firstFreeSymbol = "ELF";
         var firstFreeAmount = 5_00000000;
 
         var secondThresholdSymbol = "USDT";
         var secondThreshold = 10_000000;
         var secondRefreshSeconds = 300;
         var secondFreeSymbol = "USDT";
         var secondFreeAmount = 5_000000;
 
         _tokenContract.CheckToken(firstThresholdSymbol, InitAccount, InitAccount);
         _tokenContract.CheckToken(firstFreeSymbol, InitAccount, InitAccount);
         _tokenContract.CheckToken(secondThresholdSymbol, InitAccount, InitAccount);
         _tokenContract.CheckToken(secondFreeSymbol, InitAccount, InitAccount);
 
         var firstFreeAllowanceList = new TransactionFeeFreeAllowances
         {
             Value =
             {
                 new TransactionFeeFreeAllowance
                 {
                     Symbol = firstFreeSymbol,
                     Amount = firstFreeAmount
                 }
             }
         };
         var secondFreeAllowanceList = new TransactionFeeFreeAllowances
         {
             Value =
             {
                 new TransactionFeeFreeAllowance
                 {
                     Symbol = secondFreeSymbol,
                     Amount = secondFreeAmount
                 }
             }
         };
 
         SetTwoThresholdFreeAllowance(firstThresholdSymbol, firstThreshold, firstRefreshSeconds, firstFreeAllowanceList,
             secondThresholdSymbol, secondThreshold, secondRefreshSeconds, secondFreeAllowanceList);
     }
 
     private void RemoveConfigTransactionFeeFreeAllowances(List<string> removeTokenList)
     {
         var config = _tokenContract.GetTransactionFeeFreeAllowancesConfig();
         var organization = _parliament.GetGenesisOwnerAddress();
         var input = new RemoveTransactionFeeFreeAllowancesConfigInput
         {
             Symbols = { removeTokenList }
         };
 
         var result = AuthorityManager.ExecuteTransactionWithAuthority(_tokenContract.ContractAddress,
             "RemoveTransactionFeeFreeAllowancesConfig", input,
             InitAccount, organization);
         result.Status.ShouldBe(TransactionResultStatus.Mined);
 
         var afterConfig = _tokenContract.GetTransactionFeeFreeAllowancesConfig();
         afterConfig.Value.Count.ShouldBe(config.Value.Count.Sub(removeTokenList.Count));
         Logger.Info(afterConfig);
     }
 
     private void CheckUserFreeAllowance(string account, string thresholdSymbol, long threshold, string freeSymbol,
         long freeAmount)
     {
         var balance = _tokenContract.GetUserBalance(account, thresholdSymbol);
         var allowance = _tokenContract.GetTransactionFeeFreeAllowances(account);
         if (balance < threshold) return;
         allowance.Map.Keys.ShouldContain(thresholdSymbol);
         var thresholdFreeAllowance = allowance.Map[thresholdSymbol];
         thresholdFreeAllowance.Map.Keys.ShouldContain(freeSymbol);
         var checkFreeAmount = thresholdFreeAllowance.Map.Values.First(l => l.Symbol.Equals(freeSymbol));
         checkFreeAmount.Amount.ShouldBe(freeAmount);
         Logger.Info(allowance);
     }
 
     private void TransferUserForGettingFreeAllowance(string account)
     {
         var config = _tokenContract.GetTransactionFeeFreeAllowancesConfig();
         Logger.Info(config);
         foreach (var c in config.Value)
         {
             var type = c.Symbol.Equals("ELF") ? "Transfer" : "Issue";
             CheckBalance(account, c.Symbol, c.Threshold, type);
         }
     }
 
     private void TransferUserForGettingFreeAllowance(string account, string symbol)
     {
         var config = _tokenContract.GetTransactionFeeFreeAllowancesConfig();
         Logger.Info(config);
         var configInfo = config.Value.First(c => c.Symbol.Equals(symbol));
         var type = symbol.Equals("ELF") ? "Transfer" : "Issue";
         CheckBalance(account, symbol, configInfo.Threshold, type);
     }
 
     private TransactionFeeFreeAllowancesMap GetUserFreeAllowance(string account, string thresholdSymbol)
     {
         var balance = _tokenContract.GetUserBalance(account, thresholdSymbol);
         var allowance = _tokenContract.GetTransactionFeeFreeAllowances(account);
         Logger.Info($"{account}: \n" +
                     $"{thresholdSymbol} balance: {balance}\n" +
                     $"FreeAllowance: {allowance}");
         return allowance;
     }

     private void CreateSEEDToken()
     {
         _tokenContract.CreateSEED0Token();
     }


     private void CheckBalance(string user, string symbol, long amount, string type)
     {
         var balance = _tokenContract.GetUserBalance(user, symbol);
         if (balance >= amount) return;
         var transferAmount = amount - balance;
         var transactionResultDto = type == "Issue"
             ? _tokenContract.IssueBalance(InitAccount, user, transferAmount, symbol)
             : _tokenContract.TransferBalance(InitAccount, user, transferAmount, symbol);
         transactionResultDto.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
     }
 
     private void SetBaseFee(string methodName, bool isSizeFeeFree, List<MethodFee> methodFee)
     {
         var organization =
             _tokenContract.CallViewMethod<AuthorityInfo>(TokenMethod.GetMethodFeeController, new Empty())
                 .OwnerAddress;
         var input = new MethodFees
         {
             MethodName = methodName,
             Fees =
             {
                 methodFee
             },
             IsSizeFeeFree = isSizeFeeFree
         };
         var result = AuthorityManager.ExecuteTransactionWithAuthority(_tokenContract.ContractAddress,
             "SetMethodFee", input,
             InitAccount, organization);
         result.Status.ShouldBe(TransactionResultStatus.Mined);
         GetTokenMethodFee(methodName);
     }
 
     private void SetSizeFee(string addedSymbol, int addedWeight, int baseWeight, bool isFirstToken)
     {
         var availableTokenInfo = isFirstToken
             ? new SymbolListToPayTxSizeFee
             {
                 SymbolsToPayTxSizeFee =
                 {
                     new SymbolToPayTxSizeFee
                     {
                         TokenSymbol = addedSymbol,
                         AddedTokenWeight = addedWeight,
                         BaseTokenWeight = baseWeight
                     },
                     new SymbolToPayTxSizeFee
                     {
                         TokenSymbol = "ELF",
                         AddedTokenWeight = 1,
                         BaseTokenWeight = 1
                     }
                 }
             }
             : new SymbolListToPayTxSizeFee
             {
                 SymbolsToPayTxSizeFee =
                 {
                     new SymbolToPayTxSizeFee
                     {
                         TokenSymbol = "ELF",
                         AddedTokenWeight = 1,
                         BaseTokenWeight = 1
                     },
                     new SymbolToPayTxSizeFee
                     {
                         TokenSymbol = addedSymbol,
                         AddedTokenWeight = addedWeight,
                         BaseTokenWeight = baseWeight
                     }
                 }
             };
 
         var miners = AuthorityManager.GetCurrentMiners();
         var transactionResult = AuthorityManager.ExecuteTransactionWithAuthority(
             _tokenContract.ContractAddress, nameof(TokenMethod.SetSymbolsToPayTxSizeFee),
             availableTokenInfo, miners.First());
         transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
 
         var symbolListInfo = QueryAvailableTokenInfos();
         symbolListInfo.ShouldBe(availableTokenInfo);
     }
 
     private void RemoveSizeFee()
     {
         var availableTokenInfo = new SymbolListToPayTxSizeFee
         {
             SymbolsToPayTxSizeFee =
             {
                 new SymbolToPayTxSizeFee
                 {
                     TokenSymbol = "ELF",
                     AddedTokenWeight = 1,
                     BaseTokenWeight = 1
                 }
             }
         };
 
         var miners = AuthorityManager.GetCurrentMiners();
         var transactionResult = AuthorityManager.ExecuteTransactionWithAuthority(
             _tokenContract.ContractAddress, nameof(TokenMethod.SetSymbolsToPayTxSizeFee),
             availableTokenInfo, miners.First());
         transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
 
         var symbolListInfo = QueryAvailableTokenInfos();
         symbolListInfo.ShouldBe(availableTokenInfo);
     }
 
     private void CleanConfig()
     {
         var config = _tokenContract.GetTransactionFeeFreeAllowancesConfig();
         Logger.Info(config);
         if (!config.Value.Any()) return;
         var tokenList = config.Value.Select(c => c.Symbol).ToList();
         RemoveConfigTransactionFeeFreeAllowances(tokenList);
     }
 
     private void GetTokenMethodFee(string methodName)
     {
         var fee = _tokenContract.CallViewMethod<MethodFees>(TokenMethod.GetMethodFee, new StringValue
         {
             Value = methodName
         });
         Logger.Info(JsonConvert.SerializeObject(fee));
     }
 
     private SymbolListToPayTxSizeFee QueryAvailableTokenInfos()
     {
         var symbolListToPayTxSizeFee =
             _tokenContract.CallViewMethod<SymbolListToPayTxSizeFee>(TokenMethod.GetSymbolsToPayTxSizeFee,
                 new Empty());
         if (symbolListToPayTxSizeFee.Equals(new SymbolListToPayTxSizeFee()))
         {
             Logger.Info("GetAvailableTokenInfos: Null");
             return new SymbolListToPayTxSizeFee();
         }
 
         foreach (var info in symbolListToPayTxSizeFee.SymbolsToPayTxSizeFee)
             Logger.Info(
                 $"Symbol: {info.TokenSymbol}, TokenWeight: {info.AddedTokenWeight}, BaseWeight: {info.BaseTokenWeight}");
 
         return symbolListToPayTxSizeFee;
     }
 
     [TestMethod]
     public void SetNewTransactionFeeDelegations_Add(string delegator, string delegatee, string symbol, long amount)
     {
         var delegations = new Dictionary<string, long>
         {
             [symbol] = amount
         };
         var delegateInfo = new DelegateInfo
         {
             ContractAddress = _tokenContract.Contract,
             MethodName = "Transfer",
             Delegations =
             {
                 delegations
             },
             IsUnlimitedDelegate = false
         };
 
         _tokenContract.SetAccount(delegatee);
         var executionResult = _tokenContract.ExecuteMethodWithResult(TokenMethod.SetTransactionFeeDelegateInfos,
             new SetTransactionFeeDelegateInfosInput
             {
                 DelegatorAddress = delegator.ConvertAddress(),
                 DelegateInfoList = { delegateInfo }
             });
         var delegateInfoOfADelegatee =
             _tokenContract.GetTransactionFeeDelegateInfo(_tokenContract.Contract, delegator, delegatee, "Transfer");
         Logger.Info(delegateInfoOfADelegatee);
     }
 
     public void SetOldTransactionFeeDelegations_Add(string delegator, string delegatee, string symbol, long amount)
     {
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
     }

     public void CheckOldDelegation(string delegator, string delegatee)
     {
         var originDelegations = _tokenContract.GetTransactionFeeDelegationsOfADelegatee(delegator, delegatee);
         Logger.Info(originDelegations);
         var delegateeBalance = _tokenContract.GetUserBalance(delegatee);
         Logger.Info(delegateeBalance);
         var delegatorBalance = _tokenContract.GetUserBalance(delegator);
         Logger.Info(delegatorBalance);
     }
     
     public void CheckNewDelegation(string delegator, string delegatee)
     {
         var delegateInfoOfADelegatee =
             _tokenContract.GetTransactionFeeDelegateInfo(_tokenContract.Contract, delegator, delegatee, "Transfer");
         Logger.Info(delegateInfoOfADelegatee);
         var delegateeBalance = _tokenContract.GetUserBalance(delegatee);
         Logger.Info(delegateeBalance);
         var delegatorBalance = _tokenContract.GetUserBalance(delegator);
         Logger.Info(delegatorBalance);
     }
 }