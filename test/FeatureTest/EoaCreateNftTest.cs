using System.Text;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ProxyAccountContract;
using AElf.Contracts.TokenAdapterContract;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using CreateInput = AElf.Contracts.MultiToken.CreateInput;

namespace FeatureTest;

[TestClass]
public class EoaCreateNftTest2
{
    private ILog Logger { get; set; }
    private INodeManager NodeManager { get; set; }
    private INodeManager NodeManagerSide { get; set; }
    private int _chainId;
    private int _chainIdSide;
    private GenesisContract _genesisContract;
    private GenesisContract _genesisContractSide;
    private TokenContract _tokenContract;
    private TokenContract _tokenContractSide;
    private ForestContract _forestContractSide;
    private WhiteListContract _whiteListContract;
    private WhiteListContract _whiteListContractSide;
    private TokenAdapterContract _tokenAdapterContract;
    private ProxyAccountContract _proxyAccountContract;
    private ProxyAccountContract _proxyAccountContractSide;

    private string InitAccount { get; } = "2CpKfnoWTk69u6VySHMeuJvrX2hGrMw9pTyxcD4VM6Q28dJrhk";
    private string BuyerAccount { get; } = "2U28wge7nJqc35DEX4V9j9ndL7UJkhuv8PNBgmeEoE7pwvGCAZ";
    private string SellerAccount { get; } = "5MjQGbZysnE9ivbFkM1z1T7KCRpo7PorisdL7mSjcgxewyu3N";
    private string WhiteListAccount { get; } = "23ArvvfWxykyXkNfwAo5T3FUcZHp37m1cbeVART6tXYyHfAiys";
    private string ServiceFeeReceiver { get; } = "2oELKyYeFguErdNU2Hd9xv4pzUwR56RuWnt6HwS5UaGVL47SuH";
    private AuthorityManager AuthorityManager { get; set; }
    private AuthorityManager AuthorityManagerSide { get; set; }

    private static string RpcUrl { get; } = "192.168.67.18:8000";
    private static string SideRpcUrl { get; } = "192.168.66.106:8000";
    private string ForestContractAddress = "2ZpYFeE4yWjrcKLBoj1iwbfYnbo9hK7exvfGTdqcq77QSxpzNH";

    private string WhiteListContractAddress = "aceGtyU2fVcBkViZcaqZXHHjd7eNAJ6NPwbuFwhqv6He49BS1";
    private string TokenAdapterContractAddress = "2sFCkQs61YKVkHpN3AT7887CLfMvzzXnMkNYYM431RK5tbKQS9";//"2TXvtjgTiMwjvEyWGEvfbeQ9P6zVK55pTPcmzvLFBDCMLNUYXV";
    private string ProxyAccountContractAddress = "2M24EKAecggCnttZ9DUUMCXi4xC67rozA87kFgid9qEwRUMHTs";
    private string ProxyAccountContractAddressSide = "2jjLR4N9ZvMAPvEEQTipqpUU7bFSaUxzqF1ADYhv6EmGszSC1r";

    private string seedSymbol = "SEED-411";
    private string collectionSymbol = "AYTVSATOVM-0";
    private string nftSymbol = "AYTVSATOVM-1";
    private string nativeTokenSymbol = "ELF";

    [TestInitialize]
    public void Initialize()
    {
        Log4NetHelper.LogInit("ForestTest");
        Logger = Log4NetHelper.GetLogger();
        // NodeInfoHelper.SetConfig("nodes-env2-main_test2");

        NodeManager = new NodeManager(RpcUrl);
        AuthorityManager = new AuthorityManager(NodeManager, InitAccount);
        _chainId = ChainHelper.ConvertBase58ToChainId(NodeManager.GetChainId());

        NodeManagerSide = new NodeManager(SideRpcUrl);
        AuthorityManagerSide = new AuthorityManager(NodeManagerSide, InitAccount);
        _chainIdSide = ChainHelper.ConvertBase58ToChainId(NodeManagerSide.GetChainId());

        if (ForestContractAddress.Equals(""))
            _forestContractSide = new ForestContract(NodeManagerSide, InitAccount);
        else
            _forestContractSide = new ForestContract(NodeManagerSide, InitAccount, ForestContractAddress);

        if (WhiteListContractAddress.Equals(""))
            _whiteListContractSide = new WhiteListContract(NodeManagerSide, InitAccount);
        else
            _whiteListContractSide = new WhiteListContract(NodeManagerSide, InitAccount, WhiteListContractAddress);

        if (TokenAdapterContractAddress.Equals(""))
            _tokenAdapterContract = new TokenAdapterContract(NodeManager, InitAccount);
        else
            _tokenAdapterContract =
                new TokenAdapterContract(NodeManager, InitAccount, TokenAdapterContractAddress);

        if (ProxyAccountContractAddress.Equals(""))
            _proxyAccountContract = new ProxyAccountContract(NodeManager, InitAccount);
        else
            _proxyAccountContract = new ProxyAccountContract(NodeManager, InitAccount, ProxyAccountContractAddress);

        if (ProxyAccountContractAddressSide.Equals(""))
            _proxyAccountContractSide = new ProxyAccountContract(NodeManagerSide, InitAccount);
        else
            _proxyAccountContractSide =
                new ProxyAccountContract(NodeManagerSide, InitAccount, ProxyAccountContractAddressSide);

        _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
        _tokenContract = _genesisContract.GetTokenContract(InitAccount);

        _genesisContractSide = GenesisContract.GetGenesisContract(NodeManagerSide, InitAccount);
        _tokenContractSide = _genesisContractSide.GetTokenContract(InitAccount);
    }

    [TestMethod]
    public void CreateSeed()
    {
        for (int i = 400; i < 450; i++)
        {
            // create
            var symbol = "SEED-" + i;
            var tokenInfo = _tokenContract.GetTokenInfo(symbol);
            Logger.Info($"create seed:{i}" +
                        $"\ntokenInfo:{tokenInfo}");

            // skip if exists
            if (tokenInfo.Symbol != "")
            {
                continue;
            }

            // random create collectionSymbol
            Random random = new Random();
            StringBuilder stringBuilder = new StringBuilder();

            for (int j = 0; j < 10; j++)
            {
                char letter = (char)random.Next('A', 'Z' + 1);
                stringBuilder.Append(letter);
            }

            var collectionSymbol = stringBuilder.ToString();
            Console.WriteLine(collectionSymbol);

            var name = $"{symbol} token";
            var totalSupply = 1;
            var seedOwnedSymbol = collectionSymbol + "-0";
            var seedExpTime = "1720590463";

            var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
            {
                Symbol = symbol,
                TokenName = name,
                TotalSupply = totalSupply,
                Decimals = 0,
                Issuer = InitAccount.ConvertAddress(),
                IssueChainId = _chainId,
                IsBurnable = true,
                LockWhiteList = { _tokenContract.ContractAddress.ConvertAddress() },
                ExternalInfo = new ExternalInfo()
                {
                    Value =
                    {
                        {
                            "__seed_owned_symbol",
                            seedOwnedSymbol
                        },
                        {
                            "__seed_exp_time",
                            seedExpTime
                        }
                    }
                }
            });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            tokenInfo = _tokenContract.GetTokenInfo(symbol);
            Logger.Info($"tokenInfo:{tokenInfo}");

            MintTest(symbol);
            ApproveSeed(symbol);
            break;
        }
    }

    [TestMethod]
    public void MintTest(string symbol)
    {
        //var symbol = "SEED-83";
        var totalSupply = 1;

        var result = _tokenContract.IssueBalance(InitAccount, SellerAccount,
            totalSupply, symbol);
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
    }

    [TestMethod]
    public void ApproveSeed(string symbol)
    {
        var totalSupply = 1;

        _tokenContract.SetAccount(SellerAccount);
        var result = _tokenContract.ApproveToken(SellerAccount, _tokenAdapterContract.ContractAddress,
            totalSupply, symbol);
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
    }

    [TestMethod]
    public void BalanceTest()
    {
        var balance = _tokenContract.GetUserBalance(SellerAccount, "SEED-139");
        Logger.Info($"balance:{balance}");
    }

    [TestMethod]
    public void CreateCollectionTest()
    {
        var symbol = collectionSymbol;
        var seedSymbol = this.seedSymbol;

        _tokenAdapterContract.SetAccount(SellerAccount);

        var input = new ManagerCreateTokenInput
        {
            Symbol = symbol,
            SeedSymbol = seedSymbol,
            Amount = 10000,
            Memo = "create token",
            TokenName = symbol + "token",
            TotalSupply = 10000,
            Decimals = 0,
            Issuer = SellerAccount.ConvertAddress(),
            IsBurnable = true,
            LockWhiteList = { _tokenContract.ContractAddress.ConvertAddress() },
            IssueChainId = _chainIdSide,
            ExternalInfo = new ExternalInfos()
            {
                Value =
                {
                    {
                        "__nft_image_url",
                        $"https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/{symbol}.jpg"
                    }
                }
            },
            Owner = SellerAccount.ConvertAddress()
        };
        Logger.Info($"input:{input}");

        var result = _tokenAdapterContract.ExecuteMethodWithResult(TokenAdapterContractAddressMethod.CreateToken,
            input
        );

        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
    }

    [TestMethod]
    public void CreateNft()
    {
        var symbol = nftSymbol;
        var tokenInfo = GetTokenInfo();
        var owner = tokenInfo.Owner.ToBase58();
        var issuer = tokenInfo.Issuer.ToBase58();
        var ownerProxyAccount = GetSellerProxyAccountAddress(_proxyAccountContract, owner);
        Logger.Info($"owner:{owner}" +
                    $"\nissuer:{issuer}" +
                    $"\nownerProxyAccount:{ownerProxyAccount}");

        var createInput = new AElf.Contracts.MultiToken.CreateInput()
        {
            Symbol = symbol,
            TokenName = symbol + "token",
            TotalSupply = 10000,
            Decimals = 0,
            Issuer = issuer.ConvertAddress(),
            IsBurnable = true,
            // LockWhiteList = { },
            IssueChainId = _chainIdSide,
            ExternalInfo = new ExternalInfo()
            {
                Value =
                {
                    {
                        "__nft_image_url",
                        $"https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/{symbol}.jpg"
                    }
                }
            },
            Owner = owner.ConvertAddress()
        };

        _proxyAccountContract.SetAccount(SellerAccount);
        var result = _proxyAccountContract.ExecuteMethodWithResult(ProxyMethod.ForwardCall,
            new ForwardCallInput
            {
                ProxyAccountHash = Hash.LoadFromHex(ownerProxyAccount.ToString()),
                ContractAddress = _tokenContract.Contract,
                MethodName = nameof(TokenContractContainer.TokenContractStub.Create),
                Args = createInput.ToByteString()
            }
        );

        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
    }

    [TestMethod]
    public void IssueNftSideProxyAccount()
    {
        var symbol = nftSymbol;
        var tokenInfo = GetTokenInfo();
        var owner = tokenInfo.Owner.ToBase58();
        var issuer = tokenInfo.Issuer.ToBase58();
        var issuerProxyAccount = GetSellerProxyAccountAddress(_proxyAccountContractSide, issuer);
        Logger.Info($"owner:{owner}" +
                    $"\nissuer:{issuer}" +
                    $"\nissuerProxyAccount:{issuerProxyAccount}");

        var issueInput = new AElf.Contracts.MultiToken.IssueInput()
        {
            Symbol = symbol,
            To = SellerAccount.ConvertAddress(),
            Amount = 100,
            Memo = "issue"
        };

        _proxyAccountContractSide.SetAccount(SellerAccount);
        var result = _proxyAccountContractSide.ExecuteMethodWithResult(ProxyMethod.ForwardCall,
            new ForwardCallInput
            {
                ProxyAccountHash = Hash.LoadFromHex(issuerProxyAccount.ToString()),
                ContractAddress = _tokenContractSide.Contract,
                MethodName = nameof(TokenContractContainer.TokenContractStub.Issue),
                Args = issueInput.ToByteString()
            }
        );

        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
    }

    [TestMethod]
    public void IssueNftSideRealAccount()
    {
        var symbol = nftSymbol;

        _tokenContractSide.SetAccount(SellerAccount);
        var result = _tokenContractSide.IssueBalance(SellerAccount, SellerAccount, 1, symbol);
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        var balance = _tokenContractSide.GetUserBalance(SellerAccount, symbol);
        Logger.Info($"balance:{balance}");
    }

    [TestMethod]
    public TokenInfo GetTokenInfo()
    {
        var tokenInfo = _tokenContractSide.GetTokenInfo(collectionSymbol);
        Logger.Info($"tokenInfo:{tokenInfo}");

        return tokenInfo;
    }

    [TestMethod]
    public ProxyAccount GetSellerProxyAccountAddress(ProxyAccountContract contract, string account)
    {
        var owner = account;

        var proxyAccount = contract.CallViewMethod<ProxyAccount>(
            ProxyMethod.GetProxyAccountByProxyAccountAddress, owner.ConvertAddress());
        Logger.Info($"proxyAccount:{proxyAccount}");

        return proxyAccount;
    }
}