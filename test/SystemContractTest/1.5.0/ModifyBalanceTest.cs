using System.Linq.Dynamic.Core.Tokenizer;
using System.Text;
using AElf;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.Client.Dto;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestContract.TransactionFees;
using AElf.Contracts.TokenConverter;
using AElf.Standards.ACS1;
using AElf.Standards.ACS10;
using AElf.Standards.ACS3;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Google.Protobuf.Collections;
using log4net;
using Shouldly;
using Volo.Abp.Threading;
using Approved = AElf.Contracts.MultiToken.Approved;
using Burned = AElf.Contracts.MultiToken.Burned;
using CreateInput = AElf.Contracts.MultiToken.CreateInput;
using TokenInfo = AElf.Client.MultiToken.TokenInfo;
using TransferFromInput = AElf.Contracts.MultiToken.TransferFromInput;
using TransferInput = AElf.Client.MultiToken.TransferInput;
using Transferred = AElf.Contracts.MultiToken.Transferred;

namespace SystemContractTest;

[TestClass]
public class ModifyBalanceTest
{
    private ILog Logger { get; set; }
    private INodeManager NodeManager { get; set; }
    private INodeManager SideNodeManager { get; set; }
    private AuthorityManager AuthorityManager { get; set; }
    private AuthorityManager SideAuthority { get; set; }
    private ContractManager MainContractManager { get; set; }
    private ContractManager SideContractManager { get; set; }
    
    private TokenContract _tokenContract;
    private TokenContract _sideTokenContract;
    private TokenConverterContract _tokenConverter;
    private ParliamentContract _parliament;
    private GenesisContract _genesisContract;
    private GenesisContract _sideGenesisContract;
    private TreasuryContract _treasury;
    private ProfitContract _profit;
    private CrossChainContract _sideCrossChainContract;

    private TransactionFeesContract _acs8ContractA;
    private TokenConverterContractContainer.TokenConverterContractStub _tokenConverterSub;
    private TokenContractImplContainer.TokenContractImplStub _tokenContractImpl;
    private TokenContractImplContainer.TokenContractImplStub _sideTokenContractImpl;
    private TokenContractImplContainer.TokenContractImplStub _tokenStub;
    private TokenContractImplContainer.TokenContractImplStub _sideTokenStub;
    private BasicFunctionContract _basicFunctionContract;
    private BasicFunctionContractContainer.BasicFunctionContractStub _basicFunctionStub;
    
    private string InitAccount { get; } = "Du9KDALrVrboq51NHDaFgrfPWUJbur2PMCjVgGt7iw2qdMuBk";
    private string TestAccount { get; } = "NCt7dRM9m7vFfbXcrhhN1cLZL8R4Yveur2uS8C7T8Pim2Umhk";
    private string MainAccount { get; } = "Du9KDALrVrboq51NHDaFgrfPWUJbur2PMCjVgGt7iw2qdMuBk";
    private string SideAccount { get; } = "NCt7dRM9m7vFfbXcrhhN1cLZL8R4Yveur2uS8C7T8Pim2Umhk";
    private string _basicFunctionAddress = "2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS";
    private string _acs8Address = "DHo2K7oUXXq3kJRs1JpuwqBJP56gqoaeSKFfuvr9x8svf3vEJ";
 
    
    private string Nft = "";

    // private static string RpcUrl { get; } = "http://127.0.0.1:8000";
    private static string RpcUrl { get; } = "http://127.0.0.1:8000";
    private static string SideRpcUrl { get; } = "https://tdvw-test-node.aelf.io";

    private long SymbolFee = 2_00000000;
    private bool isNeedSide = false;
    private string FeeSymbol = "USDT";
    private string NativeToken { get; } = "ELF";
    private string TestSymbol { get; } = "TEST";
    private List<string> _resourceSymbol = new List<string>
        { "READ", "WRITE", "STORAGE", "TRAFFIC" };
    
    [TestInitialize]
    public void Initialize()
    {
        Log4NetHelper.LogInit("ModifyBalanceTest");
        Logger = Log4NetHelper.GetLogger();
        NodeInfoHelper.SetConfig("nodes");

        if (isNeedSide)
        {
            SideNodeManager = new NodeManager(SideRpcUrl);
            SideContractManager = new ContractManager(SideNodeManager, InitAccount);
            SideAuthority = new AuthorityManager(SideNodeManager, InitAccount);
            _sideGenesisContract = GenesisContract.GetGenesisContract(SideNodeManager, InitAccount);
            _sideCrossChainContract = _sideGenesisContract.GetCrossChainContract(InitAccount);
            _sideTokenContract = _sideGenesisContract.GetTokenContract(InitAccount);
            _sideTokenStub = _sideGenesisContract.GetTokenImplStub(InitAccount);
        }

        NodeManager = new NodeManager(RpcUrl);
        MainContractManager = new ContractManager(NodeManager, InitAccount);
        _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
        _tokenContract = _genesisContract.GetTokenContract(InitAccount);
        // _tokenConverter = _genesisContract.GetTokenConverterContract(InitAccount);
        _parliament = _genesisContract.GetParliamentContract(InitAccount);
        _tokenStub = _genesisContract.GetTokenImplStub(InitAccount);
        AuthorityManager = new AuthorityManager(NodeManager, InitAccount);
        
        _basicFunctionContract = _basicFunctionAddress == ""
            ? new BasicFunctionContract(NodeManager, InitAccount)
            : new BasicFunctionContract(NodeManager, InitAccount, _basicFunctionAddress);
        _basicFunctionStub = _basicFunctionContract
            .GetTestStub<BasicFunctionContractContainer.BasicFunctionContractStub>(InitAccount);

        _acs8ContractA = new TransactionFeesContract(NodeManager, InitAccount,_acs8Address);
        _tokenConverterSub = _genesisContract.GetTokenConverterStub(InitAccount);
        _tokenContractImpl = _genesisContract.GetTokenImplStub();
        CreateAndIssueToken(1000000_00000000, FeeSymbol);
    }
    
    [TestMethod]
    public async Task InitCreateSeed()
    {
        var symbol = "SEED-0";
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

    [TestMethod]
    public void TransactionFeeChargedLog()
    {
        var LogStr = "CgRVU0RUEICEr18=";
        var Logs = TransactionFeeCharged.Parser.ParseFrom(ByteString.FromBase64(LogStr));
        Logger.Info($"TransactionFeeChargedLog:({Logs})");
    }

    [TestMethod]
    public async Task IssueTest()
    {
        long amount = 1;
        long timeStamp = GetValidTimeStamp();
        var account = InitAccount;
        var NftSymbols = new Dictionary<string, string>();
        NftSymbols.Add("SEED-1", FeeSymbol);
        // NftSymbols.Add("SEED-2", "BCD");
        foreach (var NftSymbol in NftSymbols)
        {
            await CreateToken(NftSymbol.Key, amount, 0, NftSymbol.Value, timeStamp.ToString());
            var beforeBalance = _tokenContract.GetUserBalance(account, FeeSymbol);
            var to = InitAccount;
            var toBeforeBalance = _tokenContract.GetUserBalance(to, NftSymbol.Key);
            Logger.Info("beforeBalance:" + beforeBalance);
            var issueResult = await _tokenStub.Issue.SendAsync(new IssueInput
            {
                Amount = amount,
                Symbol = NftSymbol.Key,
                To = to.ConvertAddress()
            });
            issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            Logger.Info("TransactionFee:" + issueResult.TransactionResult.GetDefaultTransactionFee());
            var afterBalance = _tokenContract.GetUserBalance(account, FeeSymbol);
            Logger.Info("afterBalance:" + afterBalance);
            issueResult.TransactionResult.GetDefaultTransactionFee().ShouldBe(beforeBalance - afterBalance);
            var tokenInfo = _tokenContract.GetTokenInfo(NftSymbol.Key);
            tokenInfo.Symbol.ShouldBe(NftSymbol.Key);
            Logger.Info(tokenInfo);
            var toAfterBalance = _tokenContract.GetUserBalance(to, NftSymbol.Key);
            toAfterBalance.ShouldBe(toBeforeBalance + amount);
        }
    }

    [TestMethod]
    public void IssuedLogTest()
    {
        var LogStr = "CgZTRUVELTIQAiIiCiAdSe91WMc5tCCkGdKfgwqVbFmuKMY63wWhG86vFcRPvQ==";
        var Logs = Issued.Parser.ParseFrom(ByteString.FromBase64(LogStr));
        Logger.Info($"IssuedLog:({Logs})");

    }

    [TestMethod]
    public async Task BurnTest()
    {
        var account = InitAccount;
        var NftSymbols = new Dictionary<string, string>();
        NftSymbols.Add("SEED-3", "XYZ");
        // NftSymbols.Add("SEED-2", "BCD");
        foreach (var nftSymbol in NftSymbols)
        {
            var amount = 1;
            var beforeBalance = _tokenContract.GetUserBalance(account, NativeToken);
            var toBeforeBalance = _tokenContract.GetUserBalance(account, nftSymbol.Key);
            var result = await _tokenStub.Burn.SendAsync(new BurnInput()
            {
                Symbol = nftSymbol.Key,
                Amount = amount
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var afterBalance = _tokenContract.GetUserBalance(account, NativeToken);
            result.TransactionResult.GetDefaultTransactionFee().ShouldBe(beforeBalance - afterBalance);
            var toAfterBalance = _tokenContract.GetUserBalance(account, nftSymbol.Key);
            toAfterBalance.ShouldBe(toBeforeBalance - amount);
            afterBalance.ShouldBe(beforeBalance - result.TransactionResult.GetDefaultTransactionFee());
        }
    }

    [TestMethod]
    public void BurnedLogTest()
    {
        var LogStr = "EgZTRUVELTI=";
        var Logs = Burned.Parser.ParseFrom(ByteString.FromBase64(LogStr));
        Logger.Info($"BurnedLog:({Logs})");
    }

    [TestMethod]
    public async Task TryToChargeTransactionFeeTest()
    {
        var account = InitAccount;
        var beforeBalance = _tokenContract.GetUserBalance(account, NativeToken);
        var result = await _tokenStub.ChargeTransactionFees.SendAsync(new ChargeTransactionFeesInput()
        {
            MethodName = "UserPlayBet",
            ContractAddress = _basicFunctionAddress.ConvertAddress(),
            TransactionSizeFee = 1
        });
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var afterBalance = _tokenContract.GetUserBalance(account, NativeToken);
        result.TransactionResult.GetDefaultTransactionFee().ShouldBe(beforeBalance - afterBalance);
    }

    [TestMethod]
    public async Task ClaimTransactionFeesTest()
    {
        var account = InitAccount;
        int blockHeight = 1838;
        var beforeBalance = _tokenContract.GetUserBalance(account, NativeToken);
        var result = await _tokenStub.ClaimTransactionFees.SendAsync(new TotalTransactionFeesMap()
        {
            Value =
            {
                { TestSymbol, 1 }
            },
            BlockHash = HashHelper.ComputeFrom(blockHeight),
            BlockHeight = blockHeight
        });
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var afterBalance = _tokenContract.GetUserBalance(account, NativeToken);
        result.TransactionResult.GetDefaultTransactionFee().ShouldBe(beforeBalance - afterBalance);
    }

    [TestMethod]
    public async Task PayResourceTokensTest()
    {
        var lib = await MainContractManager.NodeManager.ApiClient.GetChainStatusAsync();
        var account = InitAccount;
        var blockHeight = 20000;
        var beforeBalance = _tokenContract.GetUserBalance(account, NativeToken);
        Logger.Info("beforeBalance:" + beforeBalance);
        var result = await _tokenStub.DonateResourceToken.SendAsync(new TotalResourceTokensMaps()
        {
            BlockHash = Hash.LoadFromHex(lib.LongestChainHash),
            BlockHeight = lib.LongestChainHeight
        });
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        Logger.Info("TransactionFee:" + result.TransactionResult.GetDefaultTransactionFee());
        var afterBalance = _tokenContract.GetUserBalance(account, NativeToken);
        Logger.Info("afterBalance:" + afterBalance);
        result.TransactionResult.GetDefaultTransactionFee().ShouldBe(beforeBalance - afterBalance);
    }

    [TestMethod]
    public async Task TransferTest()
    {
        var account = InitAccount;
        var to = TestAccount;
        var amount = 1;
        var beforeBalance = _tokenContract.GetUserBalance(account, NativeToken);
        Logger.Info("beforeBalance:" + beforeBalance);
        var toBeforeBalance = _tokenContract.GetUserBalance(to, NativeToken);
        var result = await _tokenStub.Transfer.SendAsync(new AElf.Contracts.MultiToken.TransferInput()
        {
            To = to.ConvertAddress(),
            Symbol = NativeToken,
            Amount = amount,
            Memo = "transfer"
        });
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        Logger.Info("TransactionFee:" + result.TransactionResult.GetDefaultTransactionFee());
        var afterBalance = _tokenContract.GetUserBalance(account, NativeToken);
        var toAfterBalance = _tokenContract.GetUserBalance(to, NativeToken);
        Logger.Info("afterBalance:" + afterBalance);
        var transactionFee = result.TransactionResult.GetDefaultTransactionFee();
        transactionFee.ShouldBe(beforeBalance - afterBalance - amount);
        afterBalance.ShouldBe(beforeBalance - transactionFee - amount);
        toAfterBalance.ShouldBe(toBeforeBalance + amount);
    }

    [TestMethod]
    public void TransferredLogTest()
    {
        var LogStr = "GgZTRUVELTE=";
        var Logs = Transferred.Parser.ParseFrom(ByteString.FromBase64(LogStr));
        Logger.Info($"TransferredLog:({Logs})");
    }

    [TestMethod]
    public async Task TransferFromTest()
    {
        var from = InitAccount;
        var symbol = "SEED-4";
        var amount = 1;
        var to = TestAccount;
        ApproveTest(from, symbol, amount);
        var beforeBalance = _tokenContract.GetUserBalance(from, FeeSymbol);
        Logger.Info(beforeBalance);
        var beforeBalance1 = _tokenContract.GetUserBalance(from, NativeToken);
        Logger.Info(beforeBalance1);
        var fromBeforeBalance = _tokenContract.GetUserBalance(from, symbol);
        var toBeforeBalance = _tokenContract.GetUserBalance(to, symbol);
        var result = await _tokenStub.TransferFrom.SendAsync(new TransferFromInput()
        {
            From = from.ConvertAddress(),
            To = to.ConvertAddress(),
            Symbol = symbol,
            Amount = amount
        });
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var afterBalance = _tokenContract.GetUserBalance(from, FeeSymbol);
        var afterBalance1 = _tokenContract.GetUserBalance(from, NativeToken);
        var fromAfterBalance = _tokenContract.GetUserBalance(from, symbol);
        var toAfterBalance = _tokenContract.GetUserBalance(to, symbol);
        fromAfterBalance.ShouldBe(fromBeforeBalance - amount);
        toAfterBalance.ShouldBe(toBeforeBalance + amount);
        Logger.Info(afterBalance);
        Logger.Info(afterBalance1);
    }

    [TestMethod]
    public void TransferFromLogTest()
    {
        var LogStr = "IAE=";
        var Logs = Transferred.Parser.ParseFrom(ByteString.FromBase64(LogStr));
        Logger.Info($"TransferredLog:({Logs})");
    }

    [TestMethod]
    public void TransferToContractTest()
    {
        var amount = 1;
        var symbol = "SEED-2";
        ApproveTest(InitAccount, symbol, amount);
        ApproveTest(_basicFunctionAddress, symbol, amount);
        var beforeBalance = _tokenContract.GetUserBalance(InitAccount, NativeToken);
        var fromBeforeBalance = _tokenContract.GetUserBalance(InitAccount, symbol);
        var toBeforeBalance = _tokenContract.GetUserBalance(_basicFunctionAddress, symbol);
        Logger.Info(toBeforeBalance);
        var result = _basicFunctionContract.ExecuteMethodWithResult("TransferTokenToContract", new TransferTokenToContractInput()
        {
            Symbol = symbol,
            Amount = amount
        });
        var fromAfterBalance = _tokenContract.GetUserBalance(InitAccount, symbol);
        fromAfterBalance.ShouldBe(fromBeforeBalance - amount);
        var toAfterBalance = _tokenContract.GetUserBalance(_basicFunctionAddress, symbol);
        toAfterBalance.ShouldBe(toBeforeBalance + amount);
        var afterBalance = _tokenContract.GetUserBalance(InitAccount, NativeToken);
        afterBalance.ShouldBe(beforeBalance - result.GetDefaultTransactionFee());
    }

    [TestMethod]
    public void TransferToContractLog()
    {
        var LogStr = "GgZTRUVELTI=";
        var Logs = Transferred.Parser.ParseFrom(ByteString.FromBase64(LogStr));
        Logger.Info($"TransferredLog:({Logs})");
    }

    [TestMethod]
    public async Task AdvanceResourceTokenTest()
    {
        foreach (var symbol in _resourceSymbol)
        {
            var beforeBalance = _tokenContract.GetUserBalance(_acs8ContractA.ContractAddress, symbol);
            if (beforeBalance >= 1000_00000000) continue;
            var result = await _tokenConverterSub.Buy.SendAsync(new BuyInput
            {
                Symbol = symbol,
                Amount = 1000_00000000
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var fromBeforeBalance = _tokenContract.GetUserBalance(InitAccount, symbol);
            var feeBeforeBalance = _tokenContract.GetUserBalance(InitAccount, NativeToken);
            var transferResult = await _tokenContractImpl.AdvanceResourceToken.SendAsync(
                new AdvanceResourceTokenInput
                {
                    ContractAddress = _acs8ContractA.Contract,
                    ResourceTokenSymbol = symbol,
                    Amount = 1000_00000000
                });
            transferResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var rBalance = _tokenContract.GetUserBalance(_acs8ContractA.ContractAddress, symbol);
            rBalance.ShouldBe(beforeBalance + 1000_00000000);
            var fromAfterBalance = _tokenContract.GetUserBalance(InitAccount, symbol);
            fromAfterBalance.ShouldBe(fromBeforeBalance - 1000_00000000);
            var feeAfterBalance = _tokenContract.GetUserBalance(InitAccount, NativeToken);
            feeAfterBalance.ShouldBe(feeBeforeBalance - transferResult.TransactionResult.GetDefaultTransactionFee());
        }
    }

    [TestMethod]
    public void AdvanceResourceTokenLog()
    {
        var LogStr = "GgdUUkFGRklD";
        var Logs = Transferred.Parser.ParseFrom(ByteString.FromBase64(LogStr));
        Logger.Info($"TransferredLog:({Logs})");
    }

    [TestMethod]
    public async Task TakeResourceTokenBackTest()
    {
        var balance = _tokenContract.GetUserBalance(_acs8ContractA.ContractAddress, "READ");
        Logger.Info($"Contract A READ balance : {balance}");

//            _tokenContract.TransferBalance(InitAccount, TestAccount, 1000_00000000);
        var other = _genesisContract.GetTokenImplStub(InitAccount);
        var toBeforeBalance = _tokenContract.GetUserBalance(InitAccount, "READ");
        var feeBeforeBalance = _tokenContract.GetUserBalance(InitAccount, NativeToken);
        var takeBack = await _tokenContractImpl.TakeResourceTokenBack.SendAsync(new TakeResourceTokenBackInput
        {
            ContractAddress = _acs8ContractA.Contract,
            ResourceTokenSymbol = "READ",
            Amount = 10_00000000
        });
        takeBack.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var afterBalance = _tokenContract.GetUserBalance(_acs8ContractA.ContractAddress, "READ");
        afterBalance.ShouldBe(balance - 10_00000000);
        var toAfterBalance = _tokenContract.GetUserBalance(InitAccount, "READ");
        toAfterBalance.ShouldBe(toBeforeBalance + 10_00000000);
        var feeAfterBalance = _tokenContract.GetUserBalance(InitAccount, NativeToken);
        feeAfterBalance.ShouldBe(feeBeforeBalance - takeBack.TransactionResult.GetDefaultTransactionFee());
    }

    [TestMethod]
    public void TakeResourceTokenBackLog()
    {
        var LogStr = "GgRSRUFE";
        var Logs = Transferred.Parser.ParseFrom(ByteString.FromBase64(LogStr));
        Logger.Info($"TransferredLog:({Logs})");
    }

    [TestMethod]
    public async Task CrossChainReceiveTokenTest()
    {
        var symbol = NativeToken;
        var from = InitAccount;
        var to = InitAccount;
        var amount = 1;
        var transferInput = new CrossChainTransferInput()
        {
            To = to.ConvertAddress(),
            Symbol = symbol,
            Amount = amount,
            ToChainId = ChainHelper.ConvertBase58ToChainId(SideNodeManager.GetChainId()),
            IssueChainId = ChainHelper.ConvertBase58ToChainId(NodeManager.GetChainId())
        };
        var fromBeforeBalance = _tokenContract.GetUserBalance(from, symbol);
        Logger.Info(fromBeforeBalance);
        var result = await _tokenStub.CrossChainTransfer.SendAsync(transferInput);
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var fromAfterBalance = _tokenContract.GetUserBalance(from, symbol);
        Logger.Info(fromAfterBalance);
        while (result.TransactionResult.BlockNumber > _sideCrossChainContract.GetParentChainHeight())
        {
            Logger.Info("Block is not recorded ");
            Thread.Sleep(10000);
        }

        var toBeforeBalance = _sideTokenContract.GetUserBalance(to, symbol);
        var merklePath = GetMerklePath(result.TransactionResult.BlockNumber, result.TransactionResult.TransactionId.ToString(),
            out var root);
        var receiveResult = await _sideTokenStub.CrossChainReceiveToken.SendAsync(new CrossChainReceiveTokenInput()
        {
            FromChainId = ChainHelper.ConvertBase58ToChainId(NodeManager.GetChainId()),
            ParentChainHeight = result.TransactionResult.BlockNumber,
            MerklePath = merklePath,
            TransferTransactionBytes = result.Transaction.ToByteString()
        });
        receiveResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var toAfterBalance = _sideTokenContract.GetUserBalance(to, symbol); 
        toAfterBalance.ShouldBe(toBeforeBalance + amount);
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
            LockWhiteList = { _tokenContract.Contract,  },
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
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var tokenInfo = _tokenContract.GetTokenInfo(symbol);
        tokenInfo.Symbol.ShouldBe(symbol);
        Logger.Info(tokenInfo);
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

    private void ApproveTest(string address, string symbol, long amount)
    {
        var result = _tokenStub.Approve.SendAsync(new ApproveInput()
        {
            Spender = address.ConvertAddress(),
            Symbol = symbol,
            Amount = amount
        });
        result.Result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }

    /// <summary>
    /// Get after 1000 second timestamp (second level)
    /// </summary>
    /// <returns></returns>
    private long GetValidTimeStamp()
    {
        return DateTimeOffset.Now.ToUnixTimeSeconds() + 1000;
    }
    
    protected MerklePath GetMerklePath(long blockNumber, string txId, out Hash root)
    {
        var index = 0;
        var blockInfoResult =
            AsyncHelper.RunSync(() => NodeManager.ApiClient.GetBlockByHeightAsync(blockNumber, true));
        var transactionIds = blockInfoResult.Body.Transactions;
        var transactionStatus = new List<string>();

        foreach (var transactionId in transactionIds)
        {
            var txResult = AsyncHelper.RunSync(() =>
                NodeManager.ApiClient.GetTransactionResultAsync(transactionId));
            var resultStatus = txResult.Status.ConvertTransactionResultStatus();
            transactionStatus.Add(resultStatus.ToString());
        }

        var txIdsWithStatus = new List<Hash>();
        for (var num = 0; num < transactionIds.Count; num++)
        {
            var transactionId = Hash.LoadFromHex(transactionIds[num]);
            var txRes = transactionStatus[num];
            var rawBytes = transactionId.ToByteArray().Concat(Encoding.UTF8.GetBytes(txRes))
                .ToArray();
            var txIdWithStatus = HashHelper.ComputeFrom(rawBytes);
            txIdsWithStatus.Add(txIdWithStatus);
            if (!transactionIds[num].Equals(txId)) continue;
            index = num;
            Logger.Info($"The transaction index is {index}");
        }

        var bmt = BinaryMerkleTree.FromLeafNodes(txIdsWithStatus);
        root = bmt.Root;
        var merklePath = new MerklePath();
        merklePath.MerklePathNodes.AddRange(bmt.GenerateMerklePath(index).MerklePathNodes);
        return merklePath;
    }
    
    [TestMethod]
    public void SetTokenContractMethodFee()
    {
        var info = _tokenContract.GetTokenInfo(FeeSymbol);
        Logger.Info(info);
        var symbol = FeeSymbol;
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
                    BasicFee = 2_00000000,
                    Symbol = NativeToken
                }
            },
            IsSizeFeeFree = true
        };
        var result = AuthorityManager.ExecuteTransactionWithAuthority(_tokenContract.ContractAddress,
            "SetMethodFee", input,
            InitAccount, organization);
        result.Status.ShouldBe(TransactionResultStatus.Mined);
    }
    
    private void CreateAndIssueToken(long amount, string symbol)
    {
        if (!_tokenContract.GetTokenInfo(symbol).Equals(new AElf.Contracts.MultiToken.TokenInfo())) 
            return;

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
    
    [TestMethod]
    public void CheckFee()
    {
//            Transfer method
        Logger.Info(_tokenContract.GetUserBalance(InitAccount, NativeToken));
        Logger.Info(_tokenContract.GetUserBalance(InitAccount, FeeSymbol));
        var result = _tokenContract.TransferBalance(InitAccount, TestAccount, 10_00000000, FeeSymbol);
        Logger.Info(_tokenContract.GetUserBalance(InitAccount, NativeToken));
        Logger.Info(_tokenContract.GetUserBalance(InitAccount, FeeSymbol));
        var fee = result.GetDefaultTransactionFee();
        Logger.Info("fee: " + fee);
        var eventLogs = result.Logs;
        var baseFee = TransactionFeeCharged.Parser.ParseFrom(
            ByteString.FromBase64(eventLogs.First(n => n.Name.Equals(nameof(TransactionFeeCharged))).NonIndexed));
        Logger.Info("baseFee: " + baseFee);
        var symbol = baseFee.Symbol;
        symbol.ShouldBe(FeeSymbol);
    }

    [TestMethod]
    public void CheckFeeLog()
    {
        var LogStr = "CiIKIH0W0Smc26a7mqeociJFXobd5FsMYRT08qWI7y0KbLLj";
        var Logs = ProposalReleased.Parser.ParseFrom(ByteString.FromBase64(LogStr));
        Logger.Info($"ProposalReleasedLog:({Logs})");
    }
}