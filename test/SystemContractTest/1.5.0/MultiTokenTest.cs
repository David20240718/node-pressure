
using AElf.Client.Dto;
using AElf.Contracts.Association;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenConverter;
using AElf.Standards.ACS10;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Shouldly;
using Volo.Abp.Threading;
using AElf.Contracts.TestContract.BasicFunction;

namespace SystemContractTest;

[TestClass]
public class MultiTokenTest
{
    private readonly List<string> ResourceSymbol = new List<string>
            { "CPU", "NET", "DISK", "RAM", "READ", "WRITE", "STORAGE", "TRAFFIC", "ELF", "SHARE" };

        private TokenContractContainer.TokenContractStub _bpTokenSub;
        private GenesisContract _genesisContract;
        private ParliamentContract _parliamentContract;
        private TokenConverterContractContainer.TokenConverterContractStub _testTokenConverterStub;
        // private TokenContractContainer.TokenContractStub _testTokenSub;
        private TokenContractImplContainer.TokenContractImplStub _testTokenSub;

        private TokenContract _tokenContract;
        private TokenConverterContract _tokenConverterContract;
        private TokenConverterContractContainer.TokenConverterContractStub _tokenConverterStub;
        private TokenContractImplContainer.TokenContractImplStub _tokenStub;
        private AssociationContract _association;
        private AssociationContractContainer.AssociationContractStub _associationStub;
        private BasicFunctionContractContainer.BasicFunctionContractStub _basicFunctionStub;
        private BasicFunctionContract _basicFunctionContract;
        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private AuthorityManager AuthorityManager { get; set; }

        private static string InitAccount { get; } = "2q7dzhurq3v7WseffxJYnR6WU9wZNAk8h3B23qLbcq3NUrGxrY";
        // private string BpAccount { get; } = "2ZYyxEH6j8zAyJjef6Spa99Jx2zf5GbFktyAQEBPWLCvuSAn8D";
        private string TestAccount { get; } = "NCt7dRM9m7vFfbXcrhhN1cLZL8R4Yveur2uS8C7T8Pim2Umhk";
        // private string Account { get; } = "2a6MGBRVLPsy6pu4SVMWdQqHS5wvmkZv8oas9srGWHJk7GSJPV";
        private string _basicFunctionAddress = "2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS";

        // private static string RpcUrl { get; } = "192.168.66.163:8000";
        // private static string RpcUrl { get; } = "127.0.0.1:8000";
        private static string RpcUrl { get; } = "https://aelf-public-node.aelf.io";
        private static string SideRpcUrl { get; } = "https://tdvw-test-node.aelf.io";

        private static string Symbol { get; } = "TEST";
        private string Symbol1 { get; } = "NOPROFIT";
        private string Symbol2 { get; } = "NOWHITE";
        

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("MultiTokenTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes");

            NodeManager = new NodeManager(RpcUrl);
            AuthorityManager = new AuthorityManager(NodeManager, InitAccount);
            _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
            _tokenContract = _genesisContract.GetTokenContract(InitAccount);
            _parliamentContract = _genesisContract.GetParliamentContract(InitAccount);
            _association = _genesisContract.GetAssociationAuthContract(InitAccount);
            // _tokenConverterContract = _genesisContract.GetTokenConverterContract(InitAccount);

            _tokenStub = _genesisContract.GetTokenImplStub(InitAccount);
            // _bpTokenSub = _genesisContract.GetTokenStub(BpAccount);
            // _testTokenSub = _genesisContract.GetTokenStub(TestAccount);
            _testTokenSub = _genesisContract.GetTokenImplStub(TestAccount);
            _associationStub = _genesisContract.GetAssociationAuthStub(InitAccount);
            // _tokenConverterStub = _genesisContract.GetTokenConverterStub(InitAccount);
            // _testTokenConverterStub = _genesisContract.GetTokenConverterStub(TestAccount);
            // _tokenContract.TransferBalance(InitAccount, TestAccount, 1000_00000000);
            
            // _basicFunctionContract = _basicFunctionAddress == ""
            //     ? new BasicFunctionContract(NodeManager, InitAccount)
            //     : new BasicFunctionContract(NodeManager, InitAccount, _basicFunctionAddress);
            // _basicFunctionStub = _basicFunctionContract
            //     .GetTestStub<BasicFunctionContractContainer.BasicFunctionContractStub>(InitAccount);
        }

        [TestMethod]
        public async Task InitCreateSeed()
        {
            var symbol = "SEED-0";
            if (!_tokenContract.GetTokenInfo(symbol).Equals(new TokenInfo())) return;
            var result = await _tokenStub.Create.SendAsync(new CreateInput
            {
                Issuer = InitAccount.ConvertAddress(),
                Symbol = symbol,
                Decimals = 0,
                IsBurnable = true,
                TokenName = $"{symbol} symbol",
                TotalSupply = 1,
                IssueChainId = 0,
                LockWhiteList = { _tokenContract.Contract }
            });
            Logger.Info(result);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var tokenInfo = _tokenContract.GetTokenInfo(symbol);
            tokenInfo.Symbol.ShouldBe(symbol);
            Logger.Info(tokenInfo);
        }
        
        public async Task CreateToken(string symbol, long amount, int d, string ownedSymbol = "", string expirationTime = "")
        {
            // if (!_tokenContract.GetTokenInfo(symbol).Equals(new TokenInfo())) return;
            Logger.Info("User balance before create Token: " + _tokenContract.GetUserBalance(InitAccount));
            var result = await _tokenStub.Create.SendAsync(new CreateInput
            {
                Issuer = InitAccount.ConvertAddress(),
                Symbol = symbol,
                Decimals = d,
                IsBurnable = true,
                TokenName = $"{symbol} symbol",
                TotalSupply = amount,
                IssueChainId = 0,
                LockWhiteList = { _tokenContract.Contract },
                ExternalInfo = new AElf.Contracts.MultiToken.ExternalInfo()
                {
                    Value =
                    {
                        {
                            "__seed_owned_symbol", ownedSymbol
                        },
                        {
                            "__seed_exp_time", expirationTime
                        }
                    }
                }
            });
            Logger.Info("User balance after create Token: " + _tokenContract.GetUserBalance(InitAccount));
            Logger.Info("**********create " + symbol + " Logs: " + result.TransactionResult.Logs);
            Logger.Info("**********create " + symbol + " Error: " + result.TransactionResult.Error);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var tokenInfo = _tokenContract.GetTokenInfo(symbol);
            tokenInfo.Symbol.ShouldBe(symbol);
            Logger.Info(tokenInfo);
        }
        
        public async Task IssueToken(string account, string symbol, long amount)
        {
            account = InitAccount;
            var balance = _tokenContract.GetUserBalance(account, symbol);
            var issueResult = await _tokenStub.Issue.SendAsync(new IssueInput
            {
                Amount = amount,
                Symbol = symbol,
                To = account.ConvertAddress()
            });
            issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var afterBalance = _tokenContract.GetUserBalance(account, symbol);
            afterBalance.ShouldBe(amount + balance);
            var tokenInfo = _tokenContract.GetTokenInfo(symbol);
            tokenInfo.Symbol.ShouldBe(symbol);
            Logger.Info(tokenInfo);
        }

        [TestMethod]
        [DataRow("SEED-3", "AINTST")]
        public async Task CreateSymbol(string NftCollection, string symbol)
        {
            long amount = 1;
            // CreateToken(NftCollection, 1, 0, symbol, "1688292801");
            // IssueToken(InitAccount, NftCollection, 1);
            CreateToken(symbol, amount, 6, "", "");
        }

        [TestMethod]
        public async Task DifferentIssuerCreateTest()
        {
            DynamicSenderCreateToken("SEED-3", 1, 0, _testTokenSub, "KDJ", "1688105640");
            IssueToken(InitAccount, "SEED-3", 1);
            DynamicSenderCreateToken("KDJ", 1000, 10, _testTokenSub, "", "");
        }
        
        [TestMethod]
        [DataRow("SEED-3", "1688292801", "1688379201")]
        public async Task ResetExternalInfoTest(string symbol, string expirationTime, string resetExpTime)
        {
            var createInput = new CreateInput
            {
                Issuer = InitAccount.ConvertAddress(),
                Symbol = symbol,
                Decimals = 0,
                IsBurnable = true,
                TokenName = $"{symbol} symbol",
                TotalSupply = 1,
                IssueChainId = 0,
                LockWhiteList = { _tokenContract.Contract },
                ExternalInfo = new AElf.Contracts.MultiToken.ExternalInfo()
                {
                    Value =
                    {
                        {
                            "__seed_owned_symbol", "ABC"
                        },
                        {
                            "__seed_exp_time", expirationTime
                        }
                    }
                }
            };
            var result = await _tokenStub.Create.SendAsync(createInput);
            Logger.Info("**********create " + symbol + " Logs: " + result.TransactionResult.Logs);
            Logger.Info("**********create " + symbol + " Error: " + result.TransactionResult.Error);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var tokenInfoRes = _tokenContract.GetTokenInfo(symbol);
            createInput.ExternalInfo.Value["__seed_exp_time"] = resetExpTime;
        }

        public async Task DynamicSenderCreateToken(string symbol, long amount, int d,  
            TokenContractImplContainer.TokenContractImplStub _tokenStub, string ownedSymbol = "",
            string expirationTime = "")
        {
            var result = await _tokenStub.Create.SendAsync(new CreateInput
            {
                Issuer = InitAccount.ConvertAddress(),
                Symbol = symbol,
                Decimals = d,
                IsBurnable = true,
                TokenName = $"{symbol} symbol",
                TotalSupply = amount,
                IssueChainId = 0,
                LockWhiteList = { _tokenContract.Contract },
                ExternalInfo = new AElf.Contracts.MultiToken.ExternalInfo()
                {
                    Value =
                    {
                        {
                            "__seed_owned_symbol", ownedSymbol
                        },
                        {
                            "__seed_exp_time", expirationTime
                        }
                    }
                }
            });
            Logger.Info("**********create " + symbol + " Logs: " + result.TransactionResult.Logs);
            Logger.Info("**********create " + symbol + " Error: " + result.TransactionResult.Error);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var tokenInfo = _tokenContract.GetTokenInfo(symbol);
            tokenInfo.Symbol.ShouldBe(symbol);
            Logger.Info(tokenInfo);
        }

        [TestMethod]
        public async Task DifferentSymbolTest()
        {
            var symbol = "SEED-4";
            var createInput = new CreateInput
            {
                Issuer = InitAccount.ConvertAddress(),
                Symbol = symbol,
                Decimals = 0,
                IsBurnable = true,
                TokenName = $"{symbol} symbol",
                TotalSupply = 1,
                IssueChainId = 0,
                LockWhiteList = { _tokenContract.Contract },
                ExternalInfo = new AElf.Contracts.MultiToken.ExternalInfo()
                {
                    Value =
                    {
                        {
                            "__seed_owned_symbol", "CDE"
                        },
                        {
                            "__seed_exp_time", "1688027400"
                        }
                    }
                }
            };
            var result = await _tokenStub.Create.SendAsync(createInput);
            Logger.Info("**********create " + symbol + " Logs: " + result.TransactionResult.Logs);
            Logger.Info("**********create " + symbol + " Error: " + result.TransactionResult.Error);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            // var account = InitAccount;
            // var balance = _tokenContract.GetUserBalance(account, symbol);
            // var issueResult = await _testTokenSub.Issue.SendAsync(new IssueInput
            // {
            //     Amount = 1,
            //     Symbol = symbol,
            //     To = account.ConvertAddress()
            // });
            // issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            // var afterBalance = _tokenContract.GetUserBalance(account, symbol);
            // afterBalance.ShouldBe(1 + balance);
            // var tokenInfo = _tokenContract.GetTokenInfo(symbol);
            // tokenInfo.Symbol.ShouldBe(symbol);
            // Logger.Info(tokenInfo);
            IssueToken(InitAccount, symbol, 1);
            createInput.ExternalInfo.Value["__seed_owned_symbol"] = "JDK";
            CreateToken("CDE", 10, 0, "", "");
        }

        [TestMethod]
        // [DataRow("SEED-6", "SDK", "1688113800", "")]
        [DataRow("SEED-2", "AAAAAAAAAAA", "1688113800", "Invalid token symbol length")]
        [DataRow("SEED-2", "", "1688113800", "seed_owned_symbol is empty")]
        [DataRow("SEED-2", "< > ' % ( ) & + \' \"", "1688113800", "Invalid Symbol input")]
        [DataRow("SEED-3", "X%00Y%0dZ%0aA\rB%c0", "1688113800", "Invalid Symbol input")]
        [DataRow("SEED-5", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA-0", "1688113800", "Invalid NFT symbol length")]
        [DataRow("SEED-5", "-0", "1688113800", "Invalid Symbol input")]
        [DataRow("SEED-5", "< > ' % ( ) & + \' \"-0", "1688113800", "Invalid Symbol input")]
        [DataRow("SEED-5", "DGF-0", "1688113800", "")]
        [DataRow("SEED-6", "X%00Y%0dZ%0aA\rB%c0-0", "1688113800", "Invalid Symbol input")]
        [DataRow("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA-6", "SDK", "1688113800", "NFT collection not exist")]
        [DataRow("", "SDK", "1688113800", "Invalid Symbol input")]
        [DataRow("-6", "SDK", "1688113800", "Invalid Symbol input")]
        [DataRow("< > ' % ( ) & + \' \"-6", "SDK", "1688113800", "Invalid Symbol input")]
        [DataRow("X%00Y%0dZ%0aA\rB%c0-6", "SDK", "1688113800", "Invalid Symbol input")]
        public async Task parameterTest(string symbol, string ownedSymbol, string expirationTime, string errorMsg)
        {
            var createInput = new CreateInput
            {
                Issuer = InitAccount.ConvertAddress(),
                Symbol = $"{symbol}",
                Decimals = 0,
                IsBurnable = true,
                TokenName = $"{symbol} symbol",
                TotalSupply = 1,
                IssueChainId = 0,
                LockWhiteList = { _tokenContract.Contract },
                ExternalInfo = new AElf.Contracts.MultiToken.ExternalInfo()
                {
                    Value =
                    {
                        {
                            "__seed_owned_symbol", $"{ownedSymbol}"
                        },
                        {
                            "__seed_exp_time", expirationTime
                        }
                    }
                }
            };
            CreateInput nullCreateInput = null;
            var result = await _tokenStub.Create.SendAsync(createInput);
            result.TransactionResult.Error.ShouldContain(errorMsg);
        }
        
        [TestMethod]
        public async Task TestCrossContractCreateToken()
        {
            var fee = await _tokenStub.GetMethodFee.CallAsync(new StringValue { Value = "Create" });
            _tokenContract.TransferBalance(InitAccount, _basicFunctionAddress, 100_0000000, "ELF");
            var externalInfo = new AElf.Contracts.TestContract.BasicFunction.ExternalInfo()
            {
                
            };
            var createTokenInput = new CreateTokenThroughMultiTokenInput
            {
                Symbol = "SEED-0",
                Decimals = 0,
                TokenName = "SEED-0 token",
                Issuer = InitAccount.ConvertAddress(),
                IsBurnable = true,
                TotalSupply = 1,
                // LockWhiteList = { _tokenContract.Contract },
                ExternalInfo = externalInfo
            };
            if (_basicFunctionStub.CreateTokenThroughMultiToken != null)
            {
                var result =
                    await _basicFunctionStub.CreateTokenThroughMultiToken.SendAsync(
                        createTokenInput);
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                var logs = result.TransactionResult.Logs.Where(l => l.Name.Equals("TransactionFeeCharged")).ToList();
                foreach (var log in logs)
                {
                    Logger.Info(log.Address);
                    var feeCharged = TransactionFeeCharged.Parser.ParseFrom(log.NonIndexed);
                    Logger.Info(feeCharged.Amount);
                    Logger.Info(feeCharged.Symbol);
                    var feeChargedSender = TransactionFeeCharged.Parser.ParseFrom(log.Indexed.First());
                    Logger.Info(feeChargedSender.ChargingAddress);
                }

                var blockHeight = result.TransactionResult.BlockNumber;
                Logger.Info(blockHeight);

                var checkBlock =
                    AsyncHelper.RunSync(() => NodeManager.ApiClient.GetBlockByHeightAsync(blockHeight + 1, true));
                var transactionList =
                    AsyncHelper.RunSync(() => NodeManager.ApiClient.GetTransactionResultsAsync(checkBlock.BlockHash));
                var transaction = transactionList.Find(t => t.Transaction.MethodName.Equals("ClaimTransactionFees"));
                CheckLogFee(transaction);
            }
        }
        
        private void CheckLogFee(TransactionResultDto txResult)
        {
            Logger.Info(" ==== Check Log Fee ====");
            var logs = txResult.Logs;
            foreach (var log in logs)
            {
                var name = log.Name;
                switch (name)
                {
                    case "Burned":
                        Logger.Info("Burned");
                        var burnedNoIndexed = Burned.Parser.ParseFrom(ByteString.FromBase64(log.NonIndexed));
                        foreach (var indexed in log.Indexed)
                        {
                            var burnedIndexed = Burned.Parser.ParseFrom(ByteString.FromBase64(indexed));
                            Logger.Info(burnedIndexed.Symbol.Equals("")
                                ? $"Burner: {burnedIndexed.Burner}"
                                : $"Symbol: {burnedIndexed.Symbol}");
                        }

                        Logger.Info($"Amount: {burnedNoIndexed.Amount}");
                        // burnedNoIndexed.Amount.ShouldBe(feeAmount.Div(10));
                        break;
                    case "DonationReceived":
                        Logger.Info("DonationReceived");
                        var donationReceivedNoIndexed =
                            DonationReceived.Parser.ParseFrom(ByteString.FromBase64(log.NonIndexed));
                        Logger.Info($"From: {donationReceivedNoIndexed.From}");
                        Logger.Info($"Amount: {donationReceivedNoIndexed.Amount}");
                        Logger.Info($"Symbol: {donationReceivedNoIndexed.Symbol}");
                        Logger.Info($"PoolContract: {donationReceivedNoIndexed.PoolContract}");
                        // donationReceivedNoIndexed.Amount.ShouldBe(feeAmount.Div(90));
                        break;
                    case "Transferred":
                        Logger.Info("Transferred");
                        var transferredNoIndexed = Transferred.Parser.ParseFrom(ByteString.FromBase64(log.NonIndexed));
                        foreach (var indexed in log.Indexed)
                        {
                            var transferredIndexed = Transferred.Parser.ParseFrom(ByteString.FromBase64(indexed));
                            if (transferredIndexed.Symbol.Equals(""))
                            {
                                Logger.Info(transferredIndexed.From == null
                                    ? $"To: {transferredIndexed.To}"
                                    : $"From: {transferredIndexed.From}");
                            }
                            else
                                Logger.Info($"Symbol: {transferredIndexed.Symbol}");
                        }

                        Logger.Info($"Amount: {transferredNoIndexed.Amount}");
                        // transferredNoIndexed.Amount.ShouldBe(feeAmount.Div(90));

                        break;
                    case "Approved":
                        Logger.Info("Approved");
                        var approvedNoIndexed = Approved.Parser.ParseFrom(ByteString.FromBase64(log.NonIndexed));
                        foreach (var indexed in log.Indexed)
                        {
                            var approvedIndexed = Approved.Parser.ParseFrom(ByteString.FromBase64(indexed));
                            if (approvedIndexed.Symbol.Equals(""))
                            {
                                Logger.Info(approvedIndexed.Owner == null
                                    ? $"To: {approvedIndexed.Spender}"
                                    : $"From: {approvedIndexed.Owner}");
                            }
                            else
                                Logger.Info($"Symbol: {approvedIndexed.Symbol}");
                        }

                        Logger.Info($"Amount: {approvedNoIndexed.Amount}");
                        // approvedNoIndexed.Amount.ShouldBe(feeAmount.Div(90));

                        break;
                }
            }
        }

        // [TestMethod]
        // public async Task ChangeIssuerTest()
        // {
        //     ChangeIssuer("SEED-0", "2LNg7aSwwigGWaisUzKjSGdijV9Y6jdtJqrD2PWX3ZQQ2HqsSa");
        // }
        
        // public async Task ChangeIssuer(string Symbol, string newIssuer)
        // {
        //     var symbol = Symbol;
        //     var tokenInfo = _tokenContract.GetTokenInfo(symbol);
        //
        //     var result = await _tokenStub.ChangeTokenIssuer.SendAsync(new ChangeTokenIssuerInput
        //     {
        //         NewTokenIssuer = newIssuer.ConvertAddress(),
        //         Symbol = tokenInfo.Symbol
        //     });
        //     result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        // }

        [TestMethod]
        [DataRow("SEED-0")]
        public async Task GetTokenInfo(string symbol)
        {
            var result = _tokenContract.GetTokenInfo(symbol);
            Logger.Info(result);
        }
}