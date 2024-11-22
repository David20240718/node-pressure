using System.Text;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ProxyAccountContract;
using AElf.Contracts.TokenAdapterContract;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Forest;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Portkey.Contracts.CA;
using Shouldly;
using CreateInput = AElf.Contracts.MultiToken.CreateInput;

namespace FeatureTest;

[TestClass]
public class ForestTest2
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
    private CAContract _caContract;

    private string InitAccount { get; } = "2CpKfnoWTk69u6VySHMeuJvrX2hGrMw9pTyxcD4VM6Q28dJrhk";
    private string BuyerAccount { get; } = "2U28wge7nJqc35DEX4V9j9ndL7UJkhuv8PNBgmeEoE7pwvGCAZ";

    private string SellerAccount { get; } =
        "2F4LMecoz7GSRtmequHFdFuYT8Czw9EEwfbJs9JaSCTdLDkhVo"; //"2oEkQHdMXjn5RkwPKyYR33GE5Q4Yo4QKHjAMUiob9kMfLin2d9";//"5MjQGbZysnE9ivbFkM1z1T7KCRpo7PorisdL7mSjcgxewyu3N";

    private string CAAccount { get; } = "2cpqSG5ooyVyyxBEg6XeUgKzoHFkGDQF4Q2KkXrcGnqaGtf1Xd";
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
    private string CA = "2u6Dd139bHvZJdZ835XnNKL5y6cxqzV9PEWD5fZdQXdFZLgevc";

    //private string collectionSymbol = "HUANHUANE-0";
    private string seedSymbol = "SEED-4042";
    private string collectionSymbol = "MCOUVHXC-0";
    private string nftSymbol = "FKVVSTBI-1";
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
        if (CA.Equals(""))
            _caContract = new CAContract(NodeManager, InitAccount);
        else
            _caContract = new CAContract(NodeManager, InitAccount, CA);

        _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
        _tokenContract = _genesisContract.GetTokenContract(InitAccount);

        _genesisContractSide = GenesisContract.GetGenesisContract(NodeManagerSide, InitAccount);
        _tokenContractSide = _genesisContractSide.GetTokenContract(InitAccount);
    }


    [TestMethod]
    public void ApproveSeed(string symbol)

    {
        var totalSupply = 1;

        _tokenContract.SetAccount(SellerAccount);
        var result = _tokenContract.ApproveToken(CAAccount, _tokenAdapterContract.ContractAddress,
            totalSupply, symbol);
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
    }

    [TestMethod]
    public void ApproveSeed_EOA(string seed)

    {
        var totalSupply = 1;
        _tokenContract.SetAccount(SellerAccount);
        var result = _tokenContract.ApproveToken(SellerAccount, _tokenAdapterContract.ContractAddress,
            totalSupply, seed);
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
    }


    [TestMethod]
    public void ApproveSeed1()

    {
        var totalSupply = 1;
        

        string symbol = "SEED-296";
        _tokenContract.SetAccount(SellerAccount);

        var approveInput = new AElf.Contracts.MultiToken.ApproveInput()
        {
            Spender = _tokenAdapterContract.Contract,
            Symbol = symbol,
            Amount = 1
        };


        _caContract.SetAccount(SellerAccount);
        var transferResult = _caContract.ExecuteMethodWithResult(CAMethod.ManagerForwardCall,
            new ManagerForwardCallInput
            {
                CaHash = Hash.LoadFromHex("f36b25ec892719d8e15c648204acc741c8612bf948ecb7ed2c2daca11eae4a08"),
                ContractAddress = _tokenContract.Contract,
                MethodName = nameof(TokenContractContainer.TokenContractStub.Approve),
                Args = approveInput.ToByteString()
            });
        transferResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
    }


    [TestMethod]
    public void BalanceTest()
    {
        var balance = _tokenContract.GetUserBalance(SellerAccount, "SEED-292");
        Logger.Info($"balance:{balance}");
    }

    [TestMethod]
    public (Address Owner, Address Issuer) CreateCollectionTest(string collectionSymbol, string SeedSymbol)
    {
        _tokenAdapterContract.SetAccount(SellerAccount);
        var result = _tokenAdapterContract.ExecuteMethodWithResult(TokenAdapterContractAddressMethod.CreateToken,
            new ManagerCreateTokenInput
            {
                Symbol = collectionSymbol,
                SeedSymbol = SeedSymbol,
                Amount = 10000,
                Memo = "create token",
                TokenName = collectionSymbol + "token",
                TotalSupply = 10000,
                Decimals = 0,
                Issuer = SellerAccount.ConvertAddress(),
                IsBurnable = true,
                // LockWhiteList = { },
                IssueChainId = _chainIdSide,
                ExternalInfo = new ExternalInfos()
                {
                    Value =
                    {
                        {
                            "__nft_image_url",
                            $"https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/{collectionSymbol}.jpg"
                        }
                    }
                },
                Owner = SellerAccount.ConvertAddress()
            }
        );

        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        var tokenInfo = _tokenContract.GetTokenInfo(collectionSymbol);
        Logger.Info($"tokenInfo.Owner:{tokenInfo.Owner}" +
                    $"\ntokenInfo.Issuer:{tokenInfo.Issuer}");

        return (tokenInfo.Owner, tokenInfo.Issuer);
    }


    [TestMethod]
    public (Hash, Hash) GetProxyAccountAddress(Address owner, Address issue)

    {
        var proxyAccount = _proxyAccountContract.CallViewMethod<ProxyAccount>(
            ProxyMethod.GetProxyAccountByProxyAccountAddress, owner);
        Logger.Info($"proxyAccount:{proxyAccount}");
        var proxyAccount1 = _proxyAccountContractSide.CallViewMethod<ProxyAccount>(
            ProxyMethod.GetProxyAccountByProxyAccountAddress, issue);
        Logger.Info($"proxyAccount:{proxyAccount1}");
        return (proxyAccount.ProxyAccountHash, proxyAccount1.ProxyAccountHash);
    }

    [TestMethod]
    public void CreateCollection()
    {
        //step creat and mint seed in Forest Test2
        //step 2 approve and create collection

        ApproveSeed_EOA(seedSymbol);

        var data = CreateCollectionTest(collectionSymbol, seedSymbol);
        Logger.Info($"owner{data.Owner}");
        Logger.Info($"owner{data.Issuer}");

        
        var hashdata = GetProxyAccountAddress(data.Owner, data.Issuer);
        Logger.Info($"hash:{hashdata}");
        Logger.Info($"hash:{hashdata.Item1}");
        Logger.Info($"hash:{hashdata.Item1.ToString()}");
    }


    [TestMethod]
    public void CreateNft()
    {
        //step 3:create nft
        var tokenInfo = _tokenContract.GetTokenInfo(collectionSymbol);
        Logger.Info($"tokenInfo:{tokenInfo}");

        Address Owner = tokenInfo.Owner;
        Address Issuer = tokenInfo.Issuer;


        var hashdata = GetProxyAccountAddress(Owner, Issuer);

        var createInput = new AElf.Contracts.MultiToken.CreateInput()
        {
            Symbol = nftSymbol,
            TokenName = nftSymbol + "token",
            TotalSupply = 10000,
            Decimals = 0,
            Issuer = Issuer,
            IsBurnable = true,
            // LockWhiteList = { },
            IssueChainId = _chainIdSide,
            ExternalInfo = new ExternalInfo()
            {
                Value =
                {
                    {
                        "__nft_image_url",
                        $"https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/{nftSymbol}.jpg"
                    }
                }
            },
            Owner = Owner
        };
        _proxyAccountContract.SetAccount(SellerAccount);
        var result = _proxyAccountContract.ExecuteMethodWithResult(ProxyMethod.ForwardCall,
            new ForwardCallInput
            {
                ProxyAccountHash = hashdata.Item1,
                ContractAddress = _tokenContract.Contract,
                MethodName = nameof(TokenContractContainer.TokenContractStub.Create),
                Args = createInput.ToByteString()
            }
        );

        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
    }

    [TestMethod]
    public void IssueNftSide()
    {
        var tokenInfo = _tokenContractSide.GetTokenInfo(collectionSymbol);

        Address Owner = tokenInfo.Owner;

        Address Issuer = tokenInfo.Issuer;

        var hashdata = GetProxyAccountAddress(Owner, Issuer);

        var createInput = new AElf.Contracts.MultiToken.IssueInput()
        {
            Symbol = nftSymbol,
            To = SellerAccount.ConvertAddress(),
            Amount = 100,
            Memo = "issue"
        };

        _proxyAccountContractSide.SetAccount(SellerAccount);
        var result = _proxyAccountContractSide.ExecuteMethodWithResult(ProxyMethod.ForwardCall,
            new ForwardCallInput
            {
                ProxyAccountHash = hashdata.Item2,
                ContractAddress = _tokenContractSide.Contract,
                MethodName = nameof(TokenContractContainer.TokenContractStub.Issue),
                Args = createInput.ToByteString()
            }
        );

        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
    }

    [TestMethod]
    public TokenInfo GetTokenInfo()
    {
        var tokenInfo = _tokenContractSide.GetTokenInfo(collectionSymbol);
        Logger.Info($"tokenInfo:{tokenInfo}");

        return tokenInfo;
    }
}