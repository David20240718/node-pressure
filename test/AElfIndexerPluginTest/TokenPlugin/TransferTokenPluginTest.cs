using AElf;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using Shouldly;

namespace AElfIndexerPluginTest;
[TestClass]
public class TransferTokenPluginTest : TokenPluginTestBase
{
    [TestInitialize]
    public void InitializeTest()
    {
        Initialize();
    }

    [TestMethod]
    public void CreateToken()
    {
        var result = _tokenContract.CreateSEED0Token();
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        Logger.Info($"Create SEED-0 token, height: {result.BlockNumber}");
    }

    [TestMethod]
    [DataRow("USDT")]
    [DataRow("ABC")]
    [DataRow("ETH")]
    [DataRow("BNB")]
    public void CreateFTToken(string symbol)
    {
        var issuer1 = NodeManager.NewAccount("12345678");
        var transfer = _tokenContract.TransferBalance(InitAccount, issuer1, 2000000000);
        Logger.Info($"Transfer height: {transfer.BlockNumber}, sender: {InitAccount}, to:{issuer1 }");

        var result1 = _tokenContract.CheckToken(symbol, issuer1, InitAccount);
        result1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        Logger.Info($"Create {symbol} token, height: {result1.BlockNumber}");
    }

    [TestMethod]
    [DataRow("TEST-0")]
    [DataRow("NFT-0")]
    public void CreateCollectionToken(string collection)
    {
        var issuer2 = NodeManager.NewAccount("12345678");
        var transfer2 = _tokenContract.TransferBalance(InitAccount, issuer2, 2000000000);
        Logger.Info($"Transfer height: {transfer2.BlockNumber}, sender: {InitAccount}, to:{issuer2}");

        var result2 = _tokenContract.CheckNFTCollectionToken(collection, issuer2, InitAccount);
        result2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        Logger.Info($"Create {collection} token, height: {result2.BlockNumber}");
    }

    [TestMethod]
    [DataRow("TEST")]
    [DataRow("NFT")]
    public void CreateNFTItemToken(string symbol)
    {
        var checkCollection = _tokenContract.GetTokenInfo($"{symbol}-0");
        var owner = checkCollection.Owner;
        var newAccount = "4ZVx7Ry3MtKUMEkET9itUtKZQKkXTCszDKQGxCN3MPfue3cxK";
        var transfer = _tokenContract.TransferBalance(InitAccount, newAccount, 1000000000);
        Logger.Info($"Transfer height: {transfer.BlockNumber}, sender: {InitAccount}, to:{newAccount }");
        _tokenContract.SetAccount(owner.ToBase58());
        for (int i = 1;  i<= 2; i++)
        {
            var result = _tokenContract.CreateToken(newAccount, owner.ToBase58(), 1000 * i, $"{symbol}-{i}", 0, true, 0, new ExternalInfo
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
            Logger.Info($"Create {symbol}-{i} token, height: {result.BlockNumber}");
        }
    }

    [TestMethod]
    public void BuyResource()
    {
        var newAccount = NodeManager.NewAccount("12345678");
        var transfer = _tokenContract.TransferBalance(InitAccount, newAccount, 20000000000);
        Logger.Info($"Transfer height: {transfer.BlockNumber}, sender: {InitAccount}, to:{newAccount }");
        var tokenConverter = _genesisContract.GetTokenConverterContract(InitAccount);
        var resourceToken = new List<string> { "CPU", "RAM", "WRITE", "READ", "DISK", "NET", "STORAGE", "TRAFFIC" };
        var i = 1;
        foreach (var token in resourceToken)
        {
            var buyResult =  tokenConverter.Buy(newAccount, token, 100000000 * i);
            Logger.Info($"Buy height: {buyResult.BlockNumber}, buyer: {buyResult}");
            i++;
        }
    }
    
    [TestMethod]
    public void SellResource()
    {
        var newAccount = NodeManager.NewAccount("12345678");
        var transfer = _tokenContract.TransferBalance(InitAccount, newAccount, 20000000000);
        Logger.Info($"Transfer height: {transfer.BlockNumber}, sender: {InitAccount}, to:{newAccount }");
        var tokenConverter = _genesisContract.GetTokenConverterContract(InitAccount);
        var resourceToken = new List<string> { "CPU", "RAM", "WRITE", "READ", "DISK", "NET", "STORAGE", "TRAFFIC" };
        var i = 1;
        foreach (var token in resourceToken)
        {
            var buyResult =  tokenConverter.Buy(newAccount, token, 100000000 * i);
            Logger.Info($"Buy height: {buyResult.BlockNumber}, buyer: {buyResult}");
            i++;
        }
        
        i = 1;
        foreach (var token in resourceToken)
        {
            var sellResult =  tokenConverter.Sell(newAccount, token, 1000000 * i);
            Logger.Info($"Buy height: {sellResult.BlockNumber}, seller: {newAccount}");
            i++;
        }
    }

    [TestMethod]
    public void Transfer()
    {
        var account = "3Ds8Ks8HBT9hsArgndZjTSWB3KTB6NaMooKacf3og7uFALuDn";
        _tokenContract.TransferBalance(InitAccount, account, 100000000);
    }


    [TestMethod]
    [DataRow("USDT")]
    [DataRow("ABC")]
    [DataRow("ETH")]
    [DataRow("BNB")]
    [DataRow("TEST-0")]
    [DataRow("NFT-0")]
    [DataRow("TEST-1")]
    [DataRow("NFT-1")]
    [DataRow("TEST-2")]
    [DataRow("NFT-2")]
    public void IssueToken(string symbol)
    {
        var tokenInfo = _tokenContract.GetTokenInfo(symbol);
        var issuer = tokenInfo.Issuer;
        var d = tokenInfo.Decimals;
        var account = NodeManager.NewAccount("12345678");
        var amount = CommonHelper.GenerateRandomNumber(1, 100);
        var issueAmount = (long)Math.Pow(10, d) * amount;

        var issuerResult = _tokenContract.IssueBalance(issuer.ToBase58(), account, issueAmount, symbol);
        Logger.Info($"Issue height: {issuerResult.BlockNumber}, sender: {issuer.ToBase58()}, to:{account }, amount {issueAmount}");
    }
    
    [TestMethod]
    [DataRow("USDT")]
    [DataRow("ABC")]
    [DataRow("ETH")]
    [DataRow("BNB")]
    [DataRow("TEST-0")]
    [DataRow("NFT-0")]
    [DataRow("TEST-1")]
    [DataRow("NFT-1")]
    [DataRow("TEST-2")]
    [DataRow("NFT-2")]
    public void BurnToken(string symbol)
    {
        var tokenInfo = _tokenContract.GetTokenInfo(symbol);
        var issuer = tokenInfo.Issuer;
        var d = tokenInfo.Decimals;
        var account = NodeManager.NewAccount("12345678");
        var amount = CommonHelper.GenerateRandomNumber(1, 100);
        var issueAmount = (long)Math.Pow(10, d) * amount;

        var issuerResult = _tokenContract.IssueBalance(issuer.ToBase58(), account, issueAmount, symbol);
        Logger.Info($"Issue height: {issuerResult.BlockNumber}, sender: {issuer.ToBase58()}, to:{account }, amount {issueAmount}");
        
        var burnResult = _tokenContract.Burn(account, issueAmount.Div(3), symbol);
        Logger.Info($"Burn height: {burnResult.BlockNumber}, burner: {account}, amount {issueAmount.Div(3)}");
    }
    
    
        
    [TestMethod]
    [DataRow("USDT")]
    [DataRow("ABC")]
    [DataRow("ETH")]
    [DataRow("BNB")]
    [DataRow("TEST-0")]
    [DataRow("NFT-0")]
    [DataRow("TEST-1")]
    [DataRow("NFT-1")]
    [DataRow("TEST-2")]
    [DataRow("NFT-2")]
    public void TransferToken(string symbol)
    {
        var tokenInfo = _tokenContract.GetTokenInfo(symbol);
        var issuer = tokenInfo.Issuer;
        var d = tokenInfo.Decimals;
        var account = NodeManager.NewAccount("12345678");
        var transferAccount = NodeManager.NewAccount("12345678");
        var amount = CommonHelper.GenerateRandomNumber(1, 100);
        var issueAmount = (long)Math.Pow(10, d) * amount;

        var issuerResult = _tokenContract.IssueBalance(issuer.ToBase58(), account, issueAmount, symbol);
        Logger.Info($"Issue height: {issuerResult.BlockNumber}, sender: {issuer.ToBase58()}, to:{account }, amount {issueAmount}");

        var transferResult = _tokenContract.TransferBalance(account, transferAccount, issueAmount.Div(2), symbol);
        Logger.Info($"Transfer height: {transferResult.BlockNumber}, from: {account}, to: {transferAccount}");
    }
    
    [TestMethod]
    [DataRow("USDT")]
    [DataRow("ABC")]
    [DataRow("ETH")]
    [DataRow("BNB")]
    [DataRow("TEST-0")]
    [DataRow("NFT-0")]
    [DataRow("TEST-1")]
    [DataRow("NFT-1")]
    [DataRow("TEST-2")]
    [DataRow("NFT-2")]
    public void CrossChainTransfer(string symbol)
    {
        var tokenInfo = _tokenContract.GetTokenInfo(symbol);
        var issuer = tokenInfo.Issuer;
        var d = tokenInfo.Decimals;
        var account = NodeManager.NewAccount("12345678");
        var transferAccount = NodeManager.NewAccount("12345678");
        var amount = CommonHelper.GenerateRandomNumber(1, 100);
        var issueAmount = (long)Math.Pow(10, d) * amount;

        var issuerResult = _tokenContract.IssueBalance(issuer.ToBase58(), account, issueAmount, symbol);
        Logger.Info($"Issue height: {issuerResult.BlockNumber}, sender: {issuer.ToBase58()}, to:{account }, amount {issueAmount}");

        var input = new CrossChainTransferInput
        {
            Amount = issueAmount.Div(4),
            IssueChainId = 0,
            ToChainId = ChainHelper.ConvertBase58ToChainId("tDVV"),
            Memo = "cross chain",
            Symbol = symbol,
            To = transferAccount.ConvertAddress()
        };

        _tokenContract.SetAccount(account);
        var transferResult = _tokenContract.ExecuteMethodWithResult(TokenMethod.CrossChainTransfer, input);
        Logger.Info($"CrossChainTransfer height: {transferResult.BlockNumber}, sender: {account}, to:{transferAccount }, amount {issueAmount.Div(4)}");

    }

    [TestMethod]
    public void GetBalance()
    {
        var accountList = NodeManager.ListAccounts();
        var resourceToken = new List<string> { "CPU", "RAM", "WRITE", "READ", "DISK", "NET", "STORAGE", "TRAFFIC" };
        var otherToken = new List<string>{"USDT","ABC","ETH","BNB","TEST-0","TEST-1","TEST-2","NFT-0","NFT-1","NFT-2"};
        resourceToken.AddRange(otherToken);
        
        foreach (var a in accountList)
        {
            foreach (var symbol in resourceToken)
            {
                var balance = _tokenContract.GetUserBalance(a, symbol);
                Logger.Info($"{a},{symbol},{balance}");
            }
           
        }
    }



}