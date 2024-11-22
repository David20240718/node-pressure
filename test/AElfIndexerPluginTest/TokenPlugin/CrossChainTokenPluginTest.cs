using AElf;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestContract.TransactionFees;
using AElf.CSharp.Core;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using Google.Protobuf;
using Shouldly;
using Volo.Abp.Threading;

namespace AElfIndexerPluginTest;

[TestClass]
public class CrossChainTokenPluginTest : TokenPluginCheckData
{
    [TestInitialize]
    public void InitializeTest()
    {
        Initialize();
    }

    [TestMethod]
    public void CreateToken()
    {
        var checkSEED0 = _tokenContract.GetTokenInfo("SEED-0");
        var owner = checkSEED0.Owner;
        var newAccount = "4ZVx7Ry3MtKUMEkET9itUtKZQKkXTCszDKQGxCN3MPfue3cxK";
        var issueChainId = ChainHelper.ConvertBase58ToChainId(SideNodeManagers.First().GetChainId());
        _tokenContract.TransferBalance(InitAccount, newAccount, 1000000000);
        _tokenContract.SetAccount(owner.ToBase58());
        _tokenContract.CheckToken("AELFWSFTAB", newAccount, newAccount, 0);
    }
    
    [TestMethod]
    public void CreateNFTCollectionToken()
    {
        var checkSEED0 = _tokenContract.GetTokenInfo("SEED-0");
        var owner = checkSEED0.Owner;
        var newAccount = "4ZVx7Ry3MtKUMEkET9itUtKZQKkXTCszDKQGxCN3MPfue3cxK";
        var issueChainId = ChainHelper.ConvertBase58ToChainId(SideNodeManagers.First().GetChainId());
        _tokenContract.TransferBalance(InitAccount, newAccount, 1000000000);
        _tokenContract.SetAccount(owner.ToBase58());
        _tokenContract.CheckNFTCollectionToken("AAAAAAA-0", InitAccount, InitAccount, issueChainId);
    }

    [TestMethod]
    public void CreateNFTItemToken()
    {
        var checkCollection = _tokenContract.GetTokenInfo("AAAAAAA-0");
        var owner = checkCollection.Owner;
        var newAccount = "4ZVx7Ry3MtKUMEkET9itUtKZQKkXTCszDKQGxCN3MPfue3cxK";
        var issueChainId = ChainHelper.ConvertBase58ToChainId(SideNodeManagers.First().GetChainId());
        _tokenContract.TransferBalance(InitAccount, newAccount, 1000000000);
        _tokenContract.SetAccount(owner.ToBase58());
        for (int i = 1;  i< 2; i++)
        {
            _tokenContract.CreateToken(newAccount, owner.ToBase58(), 1000, $"AAAAAAA-{i}", 0, true, issueChainId, new ExternalInfo
            {
                Value =
                {
                    {
                        "__test__", $"{i} ====  !@#$%$#%$%^$%^&*(:?<>/|'|"
                    },
                    {
                        "__1234__", "^&^*&)  *)(_+){}DS"
                    }
                }
            });
        }
        
    }

    [TestMethod]
    public void CrossChainRegister()
    {
        var sideToMainManager = _sideToMainManagers.First(s => s.FromNoeNodeManager.GetChainId().Equals("tDVV"));
        var result = sideToMainManager.ValidateTokenAddress(InitAccount, out var raw);
        sideToMainManager.CheckMainChainIndexSideChain(result.BlockNumber, sideToMainManager.ToChainNodeManager,
            sideToMainManager.FromNoeNodeManager, sideToMainManager.ToChainCrossChain,
            sideToMainManager.FromChainCrossChain);
        var registerInput = sideToMainManager.RegisterTokenAddressInput(result.BlockNumber, result.TransactionId, raw);
        _authorityManager.ExecuteTransactionWithAuthority(_tokenContract.ContractAddress,
            nameof(TokenMethod.RegisterCrossChainTokenContractAddress), registerInput, InitAccount);
    }

    //SSJ-0
    //ZZY-0
    [TestMethod]
    public async Task CrossChainCreate()
    {
        var mainToSideManager = _mainToSideManagers.First(s => s.ToChainNodeManager.GetChainId().Equals("tDVV"));
        
        var symbol = "SSJ-0";
        var validate = mainToSideManager.ValidateTokenSymbol(symbol, out var rawTx);
        mainToSideManager.CheckSideChainIndexMainChain(validate.BlockNumber);
        var crossChainCreate = mainToSideManager.CrossChainCreate(validate, rawTx);

        await Task.Delay(10000);
        var toTokenInfo = mainToSideManager.ToChainToken.GetTokenInfo(symbol);
        var originMainTokenInfo = await _token.GetTokenInfo(mainToSideManager.ToChainNodeManager.GetChainId(), symbol);
        CheckTokenInfo(originMainTokenInfo.First(), toTokenInfo).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Check()
    {
        var mainChainId = NodeManager.GetChainId();
        var symbol = "ELF";
        var originMainTokenInfo = await _token.GetTokenInfo(mainChainId, symbol);
        var mainTokenInfo = _tokenContract.GetTokenInfo(symbol);
        CheckTokenInfo(originMainTokenInfo.First(), mainTokenInfo).ShouldBeTrue();
    }

    [TestMethod]
    public async Task CrossChainTransferTestTotDVW()
    {
        var mainToSideManager = _mainToSideManagers.First(s => s.ToChainNodeManager.GetChainId().Equals("tDVV"));
        var fromAccount = InitAccount;
        var address = "2ZpYFeE4yWjrcKLBoj1iwbfYnbo9hK7exvfGTdqcq77QSxpzNH";

        var fromChainId = mainToSideManager.FromNoeNodeManager.GetChainId();
        var toChainId = mainToSideManager.ToChainNodeManager.GetChainId();

        // _tokenContract.TransferBalance(InitAccount, fromAccount, amount, symbol);
        var ResourceToken = new List<string> { "ELF" };
        foreach (var symbol in ResourceToken)
        {
            var crossChainTransfer =
                mainToSideManager.CrossChainTransfer(symbol, 1000000_00000000, InitAccount, InitAccount, out var raw);
            mainToSideManager.CheckSideChainIndexMainChain(crossChainTransfer.BlockNumber);
            var crossChainReceive =
                mainToSideManager.CrossChainReceive(crossChainTransfer.BlockNumber,crossChainTransfer.TransactionId,  raw);
        }
        
    }

    [TestMethod]
    public void BuyResourceToken()
    {
        var tokenConverter = _genesisContract.GetTokenConverterContract(InitAccount);
        var ResourceToken = new List<string> { "CPU", "RAM", "WRITE", "READ", "DISK", "NET", "STORAGE", "TRAFFIC" };
        foreach (var token in ResourceToken)
        {
            tokenConverter.Buy(InitAccount, token, 2000_00000000);
        }
    }

    [TestMethod]
    public async Task IssueNFTCollectionToken()
    {
        var symbol = "SSJ-0";
        var newAccount = "4ZVx7Ry3MtKUMEkET9itUtKZQKkXTCszDKQGxCN3MPfue3cxK";
        var chainId = NodeManager.GetChainId();

        var originTokenInfo = await _token.GetTokenInfo(chainId, symbol);

        _tokenContract.IssueBalance(newAccount, InitAccount, 1, symbol);
        await Task.Delay(10000);
        var tokenInfo = await _token.GetTokenInfo(chainId, symbol);
        tokenInfo.First().HolderCount.ShouldBe(0);
        tokenInfo.First().TransferCount.ShouldBe(originTokenInfo.First().TransferCount.Add(1));
    }

    [TestMethod]
    public async Task IssueToken()
    {
        var mainToSideManager = _mainToSideManagers.First(s => s.ToChainNodeManager.GetChainId().Equals("tDVV"));

        var symbol = "AAAA";
        var newAccount = "4ZVx7Ry3MtKUMEkET9itUtKZQKkXTCszDKQGxCN3MPfue3cxK";
        var chainId = SideNodeManagers.First().GetChainId();

        var originTokenInfo = await _token.GetTokenInfo(chainId, symbol);

        mainToSideManager.ToChainToken.IssueBalance(newAccount, newAccount, originTokenInfo.First().TotalSupply,
            symbol);
        await Task.Delay(10000);
        var tokenInfo = await _token.GetTokenInfo(chainId, symbol);
        tokenInfo.First().HolderCount.ShouldBe(1);
        tokenInfo.First().TransferCount.ShouldBe(originTokenInfo.First().TransferCount.Add(1));
    }

    [TestMethod]
    public async Task IssueNFTItemToken()
    {
        var mainToSideManager = _mainToSideManagers.First(s => s.ToChainNodeManager.GetChainId().Equals("tDVV"));

        var symbol = "ZZY-1";
        var collection = "ZZY-0";

        var newAccount = "4ZVx7Ry3MtKUMEkET9itUtKZQKkXTCszDKQGxCN3MPfue3cxK";
        var chainId = NodeManager.GetChainId();

        var originTokenInfo = await _token.GetTokenInfo(chainId, symbol);
        var originCollectionTokenInfo = await _token.GetTokenInfo(chainId, collection);

        mainToSideManager.FromChainToken.IssueBalance(newAccount, newAccount, 1, symbol);
        await Task.Delay(10000);
        var tokenInfo = await _token.GetTokenInfo(chainId, symbol);
        var collectionTokenInfo = await _token.GetTokenInfo(chainId, collection);

        tokenInfo.First().HolderCount.ShouldBe(originTokenInfo.First().HolderCount.Add(1));
        tokenInfo.First().TransferCount.ShouldBe(originTokenInfo.First().TransferCount.Add(1));
        collectionTokenInfo.First().TransferCount.ShouldBe(originCollectionTokenInfo.First().TransferCount.Add(1));
        collectionTokenInfo.First().HolderCount.ShouldBe(originCollectionTokenInfo.First().HolderCount.Add(1));
    }

    [TestMethod]
    public async Task CheckTransferCount()
    {
        var address = "dEJ2haajmnXY5wbGgwqhM9EDPmsRYViXQvDeTepdGQYzj8EbK";
        var chainId = NodeManager.GetChainId();

        var transferInfo = await _token.GetTransferInfo(chainId, "", address, "", 0, 100);
        var accountInfo = await _token.GetAccountInfo(chainId, address);
        Logger.Info(transferInfo.Count);
        Logger.Info(accountInfo.First().TransferCount);
        (transferInfo.Count == accountInfo.First().TransferCount).ShouldBeTrue();
    }

    [TestMethod]
    [DataRow("ZZY-1", 1)]
    public async Task CrossChainTransferTest(string symbol, long amount)
    {
        var mainToSideManager = _mainToSideManagers.First(s => s.ToChainNodeManager.GetChainId().Equals("tDVV"));

        var fromAccount = "";
        var toAccount = "";

        var fromChainId = mainToSideManager.FromNoeNodeManager.GetChainId();
        var toChainId = mainToSideManager.ToChainNodeManager.GetChainId();

        // _tokenContract.TransferBalance(InitAccount, fromAccount, amount, symbol);
        await Task.Delay(20000);

        var originFromChainTokenInfo = await _token.GetTokenInfo(fromChainId, symbol);
        var fromChainTokenInfo = mainToSideManager.FromChainToken.GetTokenInfo(symbol);
        CheckTokenInfo(originFromChainTokenInfo.First(), fromChainTokenInfo).ShouldBeTrue();

        var originToChainTokenInfo = await _token.GetTokenInfo(toChainId, symbol);
        var toChainTokenInfo = mainToSideManager.ToChainToken.GetTokenInfo(symbol);
        CheckTokenInfo(originToChainTokenInfo.First(), toChainTokenInfo).ShouldBeTrue();

        var originFromChainFromAccountInfo = await _token.GetAccountInfo(fromChainId, fromAccount);
        var originFromChainToAccountInfo = await _token.GetAccountInfo(fromChainId, toAccount);
        var originToChainFromAccountInfo = await _token.GetAccountInfo(toChainId, fromAccount);
        var originToChainToAccountInfo = await _token.GetAccountInfo(toChainId, toAccount);

        var originFromChainFromAccountToken = await _token.GetAccountToken(fromChainId, fromAccount, symbol);
        var originFromChainToAccountToken = await _token.GetAccountToken(fromChainId, toAccount, symbol);
        var originToChainFromAccountToken = await _token.GetAccountToken(toChainId, fromAccount, symbol);
        var originToChainToAccountToken = await _token.GetAccountToken(toChainId, toAccount, symbol);

        var fromOriginBalance = _tokenContract.GetUserBalance(fromAccount, symbol);
        originFromChainFromAccountToken.First().Amount.ShouldBe(fromOriginBalance);
        originFromChainFromAccountToken.First().FormatAmount
            .ShouldBe(FormatAmount(fromOriginBalance, originFromChainFromAccountToken.First().Token.Decimals));

        var crossChainTransfer =
            mainToSideManager.CrossChainTransfer(symbol, amount, toAccount, fromAccount, out var raw);
        var fee = crossChainTransfer.GetResourceTokenFee();
        Logger.Info(crossChainTransfer.TransactionId);
        Logger.Info(raw);
        await Task.Delay(20000);

        var transferInfo = await _token.GetTransferInfo(fromChainId, "", fromAccount, crossChainTransfer.TransactionId);
        CheckTransferInfo(transferInfo.First(l => l.Method.Equals("CrossChainTransfer")), crossChainTransfer,
            "CrossChainTransfer").ShouldBeTrue();
        CheckTransferInfo(transferInfo.First(l => l.Method.Equals("Burn")), crossChainTransfer, "Burn").ShouldBeTrue();

        Logger.Info(" Check after ");

        var afterFromTokenInfo = await _token.GetTokenInfo(fromChainId, symbol);
        var afterGetFromTokenInfo = _tokenContract.GetTokenInfo(symbol);
        CheckTokenInfo(afterFromTokenInfo.First(), afterGetFromTokenInfo).ShouldBeTrue();
        if (fee.Keys.Contains(symbol))
        {
            afterFromTokenInfo.First().Supply
                .ShouldBe(originFromChainTokenInfo.First().Supply - fee[symbol].Div(10) - amount);
            afterFromTokenInfo.First().TransferCount.ShouldBe(originFromChainTokenInfo.First().TransferCount.Add(5));
        }
        else
        {
            afterFromTokenInfo.First().Supply.ShouldBe(originFromChainTokenInfo.First().Supply - amount);
            afterFromTokenInfo.First().TransferCount.ShouldBe(originFromChainTokenInfo.First().TransferCount.Add(2));
        }

        afterFromTokenInfo.First().HolderCount.ShouldBe(originFromChainTokenInfo.First().HolderCount);
        var afterFromBalance = _tokenContract.GetUserBalance(fromAccount, symbol);

        var afterFromChainFromAccountInfo = await _token.GetAccountInfo(fromChainId, fromAccount);
        afterFromChainFromAccountInfo.First().TransferCount
            .ShouldBe(originFromChainFromAccountInfo.First().TransferCount.Add(2));
        var afterFromChainToAccountInfo = await _token.GetAccountInfo(fromChainId, toAccount);
        afterFromChainToAccountInfo.ShouldBe(originFromChainToAccountInfo);
        var afterToChainFromAccountInfo = await _token.GetAccountInfo(toChainId, fromAccount);
        afterToChainFromAccountInfo.ShouldBe(originToChainFromAccountInfo);
        var afterToChainToAccountInfo = await _token.GetAccountInfo(toChainId, toAccount);
        afterToChainToAccountInfo.First().TransferCount.ShouldBe(originToChainToAccountInfo.First().TransferCount);

        var afterFromChainFromAccountToken = await _token.GetAccountToken(fromChainId, fromAccount, symbol);
        afterFromChainFromAccountToken.First().TransferCount
            .ShouldBe(originFromChainFromAccountToken.First().TransferCount.Add(2));
        if (fee.Keys.Contains(symbol))
            afterFromChainFromAccountToken.First().Amount
                .ShouldBe(originFromChainFromAccountToken.First().Amount - fee[symbol] - amount);
        else
            afterFromChainFromAccountToken.First().Amount
                .ShouldBe(originFromChainFromAccountToken.First().Amount - amount);
        afterFromChainFromAccountToken.First().Amount.ShouldBe(afterFromBalance);
        var afterFromChainToAccountToken = await _token.GetAccountToken(fromChainId, toAccount, symbol);
        afterFromChainToAccountToken.ShouldBe(originFromChainToAccountToken);

        var afterToChainFromAccountToken = await _token.GetAccountToken(toChainId, fromAccount, symbol);
        afterToChainFromAccountToken.ShouldBe(originToChainFromAccountToken);
        var afterToChainToAccountToken = await _token.GetAccountToken(toChainId, toAccount, symbol);
        afterToChainToAccountToken.ShouldBe(originToChainToAccountToken);
    }

    [TestMethod]
    [DataRow("2682f140cea832f8b2afb1b1190a05b291a7aa67078753ee2c1b7c42ecf17845", "ZZY-1")]
    public async Task CrossChainReceiveTest(string txId, string symbol)
    {
        var mainToSideManager = _mainToSideManagers.First(s => s.ToChainNodeManager.GetChainId().Equals("tDVV"));

        // var fromAccount = InitAccount;
        // var toAccount = InitAccount;
        var fromAccount = "4ZVx7Ry3MtKUMEkET9itUtKZQKkXTCszDKQGxCN3MPfue3cxK";
        var toAccount = "4ZVx7Ry3MtKUMEkET9itUtKZQKkXTCszDKQGxCN3MPfue3cxK";
        //4ZVx7Ry3MtKUMEkET9itUtKZQKkXTCszDKQGxCN3MPfue3cxK
        //mbSf2fm4on2oFQXT1nSjgdsdpDVNkQ6MDPNXyw9FNPrtDFVZM

        var fromChainId = mainToSideManager.FromNoeNodeManager.GetChainId();
        var toChainId = mainToSideManager.ToChainNodeManager.GetChainId();

        var originFromChainTokenInfo = await _token.GetTokenInfo(fromChainId, symbol);
        var fromChainTokenInfo = mainToSideManager.FromChainToken.GetTokenInfo(symbol);
        CheckTokenInfo(originFromChainTokenInfo.First(), fromChainTokenInfo).ShouldBeTrue();

        var originToChainTokenInfo = await _token.GetTokenInfo(toChainId, symbol);
        var toChainTokenInfo = mainToSideManager.ToChainToken.GetTokenInfo(symbol);
        CheckTokenInfo(originToChainTokenInfo.First(), toChainTokenInfo).ShouldBeTrue();

        var originFromChainFromAccountInfo = await _token.GetAccountInfo(fromChainId, fromAccount);
        var originFromChainToAccountInfo = await _token.GetAccountInfo(fromChainId, toAccount);
        var originToChainFromAccountInfo = await _token.GetAccountInfo(toChainId, fromAccount);
        var originToChainToAccountInfo = await _token.GetAccountInfo(toChainId, toAccount);

        var originFromChainFromAccountToken = await _token.GetAccountToken(fromChainId, fromAccount, symbol);
        var originFromChainToAccountToken = await _token.GetAccountToken(fromChainId, toAccount, symbol);
        var originToChainFromAccountToken = await _token.GetAccountToken(toChainId, fromAccount, symbol);
        var originToChainToAccountToken = await _token.GetAccountToken(toChainId, toAccount, symbol);

        var crossChainTransfer = mainToSideManager.FromNoeNodeManager.CheckTransactionResult(txId);
        var crossChainReceive =
            mainToSideManager.CrossChainReceive(crossChainTransfer);
        
        await Task.Delay(20000);
        var transferInfo = await _token.GetTransferInfo(toChainId, "", "", crossChainReceive.TransactionId);
        CheckTransferInfo(transferInfo.First(l => l.Method.Equals("CrossChainReceive")), crossChainReceive,
            "CrossChainReceive").ShouldBeTrue();
        Logger.Info(" Check after ");

        var afterToChainTokenInfo = await _token.GetTokenInfo(toChainId, symbol);
        var afterGetToChainTokenInfo = mainToSideManager.ToChainToken.GetTokenInfo(symbol);
        CheckTokenInfo(afterToChainTokenInfo.First(), afterGetToChainTokenInfo).ShouldBeTrue();

        afterToChainTokenInfo.First().Supply
            .ShouldBe(originToChainTokenInfo.First().Supply.Add(transferInfo.First().Amount));
        afterToChainTokenInfo.First().TransferCount.ShouldBe(originToChainTokenInfo.First().TransferCount.Add(1));

        var afterToBalance = mainToSideManager.ToChainToken.GetUserBalance(toAccount, symbol);
        var afterFromChainFromAccountInfo = await _token.GetAccountInfo(fromChainId, fromAccount);
        afterFromChainFromAccountInfo.First().TransferCount
            .ShouldBe(originFromChainFromAccountInfo.First().TransferCount);
        var afterFromChainToAccountInfo = await _token.GetAccountInfo(fromChainId, toAccount);
        afterFromChainToAccountInfo.ShouldBe(originFromChainToAccountInfo);
        var afterToChainFromAccountInfo = await _token.GetAccountInfo(toChainId, fromAccount);
        afterToChainFromAccountInfo.ShouldBe(originToChainFromAccountInfo);
        var afterToChainToAccountInfo = await _token.GetAccountInfo(toChainId, toAccount);
        afterToChainToAccountInfo.First().TransferCount
            .ShouldBe(originToChainToAccountInfo.First().TransferCount.Add(1));
        afterToChainToAccountInfo.First().TokenHoldingCount
            .ShouldBe(originToChainToAccountInfo.First().TokenHoldingCount.Add(1));

        var afterFromChainFromAccountToken = await _token.GetAccountToken(fromChainId, fromAccount, symbol);
        afterFromChainFromAccountToken.First().Amount.ShouldBe(originFromChainFromAccountToken.First().Amount);
        var afterFromChainToAccountToken = await _token.GetAccountToken(fromChainId, toAccount, symbol);
        afterFromChainToAccountToken.ShouldBe(originFromChainToAccountToken);

        var afterToChainFromAccountToken = await _token.GetAccountToken(toChainId, fromAccount, symbol);
        afterToChainFromAccountToken.ShouldBe(originToChainFromAccountToken);
        var afterToChainToAccountToken = await _token.GetAccountToken(toChainId, toAccount, symbol);
        afterToChainToAccountToken.Count.ShouldBe(originToChainToAccountToken.Count.Add(1));
        afterToChainToAccountToken.First().TransferCount.ShouldBe(1);
        afterToChainToAccountToken.First().Amount.ShouldBe(afterToBalance);
        afterToChainToAccountToken.First().FormatAmount
            .ShouldBe(FormatAmount(afterToBalance, afterToChainTokenInfo.First().Decimals));
    }

    
    [TestMethod]
    [DataRow("ELF", 20000000)]
    public async Task CrossChainTransferTestSide(string symbol, long amount)
    {
        var sideToMainManager = _sideToMainManagers.First(s => s.FromNoeNodeManager.GetChainId().Equals("tDVV"));

        var fromAccount = "4ZVx7Ry3MtKUMEkET9itUtKZQKkXTCszDKQGxCN3MPfue3cxK";
        var toAccount = "4ZVx7Ry3MtKUMEkET9itUtKZQKkXTCszDKQGxCN3MPfue3cxK";

        var fromChainId = sideToMainManager.FromNoeNodeManager.GetChainId();
        var toChainId = sideToMainManager.ToChainNodeManager.GetChainId();

        await Task.Delay(20000);

        var originFromChainTokenInfo = await _token.GetTokenInfo(fromChainId, symbol);
        var fromChainTokenInfo = sideToMainManager.FromChainToken.GetTokenInfo(symbol);
        CheckTokenInfo(originFromChainTokenInfo.First(), fromChainTokenInfo).ShouldBeTrue();

        var originToChainTokenInfo = await _token.GetTokenInfo(toChainId, symbol);
        var toChainTokenInfo = sideToMainManager.ToChainToken.GetTokenInfo(symbol);
        CheckTokenInfo(originToChainTokenInfo.First(), toChainTokenInfo).ShouldBeTrue();

        var originFromChainFromAccountInfo = await _token.GetAccountInfo(fromChainId, fromAccount);
        var originFromChainToAccountInfo = await _token.GetAccountInfo(fromChainId, toAccount);
        var originToChainFromAccountInfo = await _token.GetAccountInfo(toChainId, fromAccount);
        var originToChainToAccountInfo = await _token.GetAccountInfo(toChainId, toAccount);

        var originFromChainFromAccountToken = await _token.GetAccountToken(fromChainId, fromAccount, symbol);
        var originFromChainToAccountToken = await _token.GetAccountToken(fromChainId, toAccount, symbol);
        var originToChainFromAccountToken = await _token.GetAccountToken(toChainId, fromAccount, symbol);
        var originToChainToAccountToken = await _token.GetAccountToken(toChainId, toAccount, symbol);

        var fromOriginBalance = sideToMainManager.FromChainToken.GetUserBalance(fromAccount, symbol);
        originFromChainFromAccountToken.First().Amount.ShouldBe(fromOriginBalance);
        originFromChainFromAccountToken.First().FormatAmount
            .ShouldBe(FormatAmount(fromOriginBalance, originFromChainFromAccountToken.First().Token.Decimals));

        var crossChainTransfer =
            sideToMainManager.CrossChainTransfer(symbol, amount, toAccount, fromAccount, out var raw);
        var fee = crossChainTransfer.GetResourceTokenFee();
        Logger.Info(crossChainTransfer.TransactionId);
        Logger.Info(raw);
        await Task.Delay(20000);

        var transferInfo = await _token.GetTransferInfo(fromChainId, "", fromAccount, crossChainTransfer.TransactionId);
        CheckTransferInfo(transferInfo.First(l => l.Method.Equals("CrossChainTransfer")), crossChainTransfer,
            "CrossChainTransfer").ShouldBeTrue();
        CheckTransferInfo(transferInfo.First(l => l.Method.Equals("Burn")), crossChainTransfer, "Burn").ShouldBeTrue();

        Logger.Info(" Check after ");

        var afterFromTokenInfo = await _token.GetTokenInfo(fromChainId, symbol);
        var afterGetFromTokenInfo = sideToMainManager.FromChainToken.GetTokenInfo(symbol);
        CheckTokenInfo(afterFromTokenInfo.First(), afterGetFromTokenInfo).ShouldBeTrue();
        if (fee.Keys.Contains(symbol))
        {
            afterFromTokenInfo.First().Supply
                .ShouldBe(originFromChainTokenInfo.First().Supply - fee[symbol].Div(10) - amount);
            afterFromTokenInfo.First().TransferCount.ShouldBe(originFromChainTokenInfo.First().TransferCount.Add(5));
        }
        else
        {
            afterFromTokenInfo.First().Supply.ShouldBe(originFromChainTokenInfo.First().Supply - amount);
            afterFromTokenInfo.First().TransferCount.ShouldBe(originFromChainTokenInfo.First().TransferCount.Add(2));
        }

        afterFromTokenInfo.First().HolderCount.ShouldBe(originFromChainTokenInfo.First().HolderCount);
        var afterFromBalance = sideToMainManager.FromChainToken.GetUserBalance(fromAccount, symbol);

        var afterFromChainFromAccountInfo = await _token.GetAccountInfo(fromChainId, fromAccount);
        afterFromChainFromAccountInfo.First().TransferCount
            .ShouldBe(originFromChainFromAccountInfo.First().TransferCount.Add(2));
        var afterFromChainToAccountInfo = await _token.GetAccountInfo(fromChainId, toAccount);
        afterFromChainToAccountInfo.ShouldBe(originFromChainToAccountInfo);
        var afterToChainFromAccountInfo = await _token.GetAccountInfo(toChainId, fromAccount);
        afterToChainFromAccountInfo.ShouldBe(originToChainFromAccountInfo);
        var afterToChainToAccountInfo = await _token.GetAccountInfo(toChainId, toAccount);
        afterToChainToAccountInfo.First().TransferCount.ShouldBe(originToChainToAccountInfo.First().TransferCount);

        var afterFromChainFromAccountToken = await _token.GetAccountToken(fromChainId, fromAccount, symbol);
        afterFromChainFromAccountToken.First().TransferCount
            .ShouldBe(originFromChainFromAccountToken.First().TransferCount.Add(2));
        if (fee.Keys.Contains(symbol))
            afterFromChainFromAccountToken.First().Amount
                .ShouldBe(originFromChainFromAccountToken.First().Amount - fee[symbol] - amount);
        else
            afterFromChainFromAccountToken.First().Amount
                .ShouldBe(originFromChainFromAccountToken.First().Amount - amount);
        afterFromChainFromAccountToken.First().Amount.ShouldBe(afterFromBalance);
        var afterFromChainToAccountToken = await _token.GetAccountToken(fromChainId, toAccount, symbol);
        afterFromChainToAccountToken.ShouldBe(originFromChainToAccountToken);

        var afterToChainFromAccountToken = await _token.GetAccountToken(toChainId, fromAccount, symbol);
        afterToChainFromAccountToken.ShouldBe(originToChainFromAccountToken);
        var afterToChainToAccountToken = await _token.GetAccountToken(toChainId, toAccount, symbol);
        afterToChainToAccountToken.ShouldBe(originToChainToAccountToken);
    }

    [TestMethod]
    [DataRow("a65fea120ef9b3a1f638064d5df0c411066e4a439c87707496f0996c7bde8798", "ELF")]
    public async Task CrossChainReceiveTestSide(string txId, string symbol)
    {
        var sideToMainManager = _sideToMainManagers.First(s => s.FromNoeNodeManager.GetChainId().Equals("tDVV"));
        
        var fromAccount = "";
        var toAccount = "";

        var fromChainId = sideToMainManager.FromNoeNodeManager.GetChainId();
        var toChainId = sideToMainManager.ToChainNodeManager.GetChainId();

        var originFromChainTokenInfo = await _token.GetTokenInfo(fromChainId, symbol);
        var fromChainTokenInfo = sideToMainManager.FromChainToken.GetTokenInfo(symbol);
        CheckTokenInfo(originFromChainTokenInfo.First(), fromChainTokenInfo).ShouldBeTrue();

        var originToChainTokenInfo = await _token.GetTokenInfo(toChainId, symbol);
        var toChainTokenInfo = sideToMainManager.ToChainToken.GetTokenInfo(symbol);
        CheckTokenInfo(originToChainTokenInfo.First(), toChainTokenInfo).ShouldBeTrue();

        var originFromChainFromAccountInfo = await _token.GetAccountInfo(fromChainId, fromAccount);
        var originFromChainToAccountInfo = await _token.GetAccountInfo(fromChainId, toAccount);
        var originToChainFromAccountInfo = await _token.GetAccountInfo(toChainId, fromAccount);
        var originToChainToAccountInfo = await _token.GetAccountInfo(toChainId, toAccount);

        var originFromChainFromAccountToken = await _token.GetAccountToken(fromChainId, fromAccount, symbol);
        var originFromChainToAccountToken = await _token.GetAccountToken(fromChainId, toAccount, symbol);
        var originToChainFromAccountToken = await _token.GetAccountToken(toChainId, fromAccount, symbol);
        var originToChainToAccountToken = await _token.GetAccountToken(toChainId, toAccount, symbol);

        var crossChainTransfer = sideToMainManager.FromNoeNodeManager.CheckTransactionResult(txId);
        var crossChainReceive =
            sideToMainManager.CrossChainReceive(crossChainTransfer);
        
        await Task.Delay(20000);
        var transferInfo = await _token.GetTransferInfo(toChainId, "", "", crossChainReceive.TransactionId);
        CheckTransferInfo(transferInfo.First(l => l.Method.Equals("CrossChainReceive")), crossChainReceive,
            "CrossChainReceive").ShouldBeTrue();
        Logger.Info(" Check after ");

        var afterToChainTokenInfo = await _token.GetTokenInfo(toChainId, symbol);
        var afterGetToChainTokenInfo = sideToMainManager.ToChainToken.GetTokenInfo(symbol);
        CheckTokenInfo(afterToChainTokenInfo.First(), afterGetToChainTokenInfo).ShouldBeTrue();

        afterToChainTokenInfo.First().Supply
            .ShouldBe(originToChainTokenInfo.First().Supply.Add(transferInfo.First().Amount));
        afterToChainTokenInfo.First().TransferCount.ShouldBe(originToChainTokenInfo.First().TransferCount.Add(1));

        var afterToBalance = sideToMainManager.ToChainToken.GetUserBalance(toAccount, symbol);
        var afterFromChainFromAccountInfo = await _token.GetAccountInfo(fromChainId, fromAccount);
        afterFromChainFromAccountInfo.First().TransferCount
            .ShouldBe(originFromChainFromAccountInfo.First().TransferCount);
        var afterFromChainToAccountInfo = await _token.GetAccountInfo(fromChainId, toAccount);
        afterFromChainToAccountInfo.ShouldBe(originFromChainToAccountInfo);
        var afterToChainFromAccountInfo = await _token.GetAccountInfo(toChainId, fromAccount);
        afterToChainFromAccountInfo.ShouldBe(originToChainFromAccountInfo);
        var afterToChainToAccountInfo = await _token.GetAccountInfo(toChainId, toAccount);
        afterToChainToAccountInfo.First().TransferCount
            .ShouldBe(originToChainToAccountInfo.First().TransferCount.Add(1));
        afterToChainToAccountInfo.First().TokenHoldingCount
            .ShouldBe(originToChainToAccountInfo.First().TokenHoldingCount.Add(1));

        var afterFromChainFromAccountToken = await _token.GetAccountToken(fromChainId, fromAccount, symbol);
        afterFromChainFromAccountToken.First().Amount.ShouldBe(originFromChainFromAccountToken.First().Amount);
        var afterFromChainToAccountToken = await _token.GetAccountToken(fromChainId, toAccount, symbol);
        afterFromChainToAccountToken.ShouldBe(originFromChainToAccountToken);

        var afterToChainFromAccountToken = await _token.GetAccountToken(toChainId, fromAccount, symbol);
        afterToChainFromAccountToken.ShouldBe(originToChainFromAccountToken);
        var afterToChainToAccountToken = await _token.GetAccountToken(toChainId, toAccount, symbol);
        afterToChainToAccountToken.Count.ShouldBe(originToChainToAccountToken.Count.Add(1));
        afterToChainToAccountToken.First().TransferCount.ShouldBe(1);
        afterToChainToAccountToken.First().Amount.ShouldBe(afterToBalance);
        afterToChainToAccountToken.First().FormatAmount
            .ShouldBe(FormatAmount(afterToBalance, afterToChainTokenInfo.First().Decimals));
    }
    
    [TestMethod]
    public async Task CheckTransfer()
    {
        var mainToSideManager = _mainToSideManagers.First(s => s.ToChainNodeManager.GetChainId().Equals("tDVV"));
        var fromAccount = "dEJ2haajmnXY5wbGgwqhM9EDPmsRYViXQvDeTepdGQYzj8EbK";
        var mainChainId = NodeManager.GetChainId();
        var toChainId = mainToSideManager.ToChainNodeManager.GetChainId();


        var txId = "76674e3d456f3bd8a80417dd11bf2ebf82ce17cf1852a70e89030cec45d5d355";
        var crossChainTransfer = await mainToSideManager.ToChainNodeManager.ApiClient.GetTransactionResultAsync(txId);
        var transferInfo = await _token.GetTransferInfo(toChainId, "", "", crossChainTransfer.TransactionId);
        CheckTransferInfo(transferInfo.First(l => l.Method.Equals("CrossChainReceive")), crossChainTransfer,
            "CrossChainReceive").ShouldBeTrue();
    }

    [TestMethod]
    public async Task BurnNFTItemFTToken()
    {
        var address = "4ZVx7Ry3MtKUMEkET9itUtKZQKkXTCszDKQGxCN3MPfue3cxK";
        var symbol = "ZZY-1";
        var chainId = NodeManager.GetChainId();
        var originAccountInfo = await _token.GetAccountInfo(chainId, address);

        
        _tokenContract.SetAccount(address);
        var balance = _tokenContract.GetUserBalance(address, symbol);
        _tokenContract.Burn(address, balance, symbol);
        await Task.Delay(10000);
        var checkAccountToken = await _token.GetAccountToken(chainId, address, symbol);
        checkAccountToken.First(l => l.Token.Symbol.Equals(symbol)).Amount.ShouldBe(0);
        var accountInfo = await _token.GetAccountInfo(chainId, address);
        
        accountInfo.First().TransferCount.ShouldBe(originAccountInfo.First().TransferCount.Add(1));
        accountInfo.First().TokenHoldingCount.ShouldBe(originAccountInfo.First().TokenHoldingCount.Sub(1));
    }

    [TestMethod]
    public async Task CheckRental()
    {
        var txId = "22ddaee438ab8bb5771f628ab1c84763938071f9d483d5ef4c1d7fa28e55f909";
        var manager = _sideToMainManagers.First(s => s.FromNoeNodeManager.GetChainId().Equals("tDVW"));
        var result = await manager.FromNoeNodeManager.ApiClient.GetTransactionResultAsync(txId);
        var logs = result.Logs.Where(l => l.Name.Equals("RentalCharged")).ToList();
        foreach (var log in logs)
        {
            var charged = RentalCharged.Parser.ParseFrom(ByteString.FromBase64(log.NonIndexed));
            Logger.Info(charged.Amount);
            Logger.Info(charged.Payer);
            Logger.Info(charged.Receiver);
            Logger.Info(charged.Symbol);
        }
    }

    [TestMethod]
    public void CheckBalance()
    {
        var account = "23GxsoW9TRpLqX1Z5tjrmcRMMSn5bhtLAf4HtPj8JX9BerqTqp";
        var ResourceToken = new List<string> { "CPU", "RAM", "DISK", "NET" };
        var mainToSideManager = _mainToSideManagers.First(l => l.ToChainNodeManager.GetChainId().Equals("tDVW"));
        foreach (var resource in ResourceToken)
        {
            var balance = mainToSideManager.ToChainToken.GetUserBalance(account, resource);
            Logger.Info($"{resource}, {balance}");
        }
    }

    [TestMethod]
    public void ACS8Test()
    {
        var mainToSide = _mainToSideManagers.First(s => s.ToChainNodeManager.GetChainId().Equals("tDVW"));
        var address = "2ZpYFeE4yWjrcKLBoj1iwbfYnbo9hK7exvfGTdqcq77QSxpzNH";
        var acs8 = new TransactionFeesContract(mainToSide.ToChainNodeManager, InitAccount, address);
        acs8.SetAccount(InitAccount);
        // acs8.InitializeTxFees(address.ConvertAddress());
        var result = acs8.ExecuteMethodWithResult(TxFeesMethod.ComplexCountTest, new ReadWriteInput
        {
            Read = 10,
            Write = 100
        });
        Logger.Info(result.BlockNumber);
        var claimedBlock =
            AsyncHelper.RunSync(() => mainToSide.ToChainNodeManager.ApiClient.GetBlockByHeightAsync(result.BlockNumber + 1, true));
        var transactionResultDtos = AsyncHelper.RunSync(() => mainToSide.ToChainNodeManager.ApiClient.GetTransactionResultsAsync(claimedBlock.BlockHash));
        var claimedTransaction =
            transactionResultDtos.Find(l => l.Transaction.MethodName.Equals("DonateResourceToken"));
        Logger.Info(claimedTransaction?.TransactionId);
    }

    [TestMethod]
    public async Task CheckAccountInfo()
    {
        var accountInfo = await _token.GetAccountInfo("AELF", "", 0, 1000);
        Logger.Info(accountInfo.Count);
        var mainToSide = _mainToSideManagers.First(s => s.ToChainNodeManager.GetChainId().Equals("tDVW"));
        var newAccount = mainToSide.FromNoeNodeManager.NewAccount("12345678");
        mainToSide.FromChainToken.TransferBalance(InitAccount, newAccount, 100000000,"CPU");
        await Task.Delay(10000);
        
        accountInfo = await _token.GetAccountInfo("AELF", "", 0, 1000);
        Logger.Info(accountInfo.Count);
    }

}