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
using Portkey.Contracts.CA;
using Shouldly;
using CreateInput = AElf.Contracts.MultiToken.CreateInput;

namespace FeatureTest;

[TestClass]
public class CACreateNftTest
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
    private CAContract _caContractSide;

    private string InitAccount { get; } = "2CpKfnoWTk69u6VySHMeuJvrX2hGrMw9pTyxcD4VM6Q28dJrhk";
    private string BuyerAccount { get; } = "2U28wge7nJqc35DEX4V9j9ndL7UJkhuv8PNBgmeEoE7pwvGCAZ";
    private string SellerAccount { get; } = "2oEkQHdMXjn5RkwPKyYR33GE5Q4Yo4QKHjAMUiob9kMfLin2d9";
    private string CaAccount { get; } = "2cpqSG5ooyVyyxBEg6XeUgKzoHFkGDQF4Q2KkXrcGnqaGtf1Xd";
    private string CaAccountSide = "2oiaV1BgiZyXBb2F98JQ9ynizo467DqmVTDMa46Q6mUMEQR4au";
    private string WhiteListAccount { get; } = "23ArvvfWxykyXkNfwAo5T3FUcZHp37m1cbeVART6tXYyHfAiys";
    private string ServiceFeeReceiver { get; } = "2oELKyYeFguErdNU2Hd9xv4pzUwR56RuWnt6HwS5UaGVL47SuH";
    private AuthorityManager AuthorityManager { get; set; }
    private AuthorityManager AuthorityManagerSide { get; set; }

    private static string RpcUrl { get; } = "192.168.67.18:8000";
    private static string SideRpcUrl { get; } = "192.168.66.106:8000";
    private string ForestContractAddress = "2ZpYFeE4yWjrcKLBoj1iwbfYnbo9hK7exvfGTdqcq77QSxpzNH";

    private string WhiteListContractAddress = "aceGtyU2fVcBkViZcaqZXHHjd7eNAJ6NPwbuFwhqv6He49BS1";
    private string TokenAdapterContractAddress = "2sFCkQs61YKVkHpN3AT7887CLfMvzzXnMkNYYM431RK5tbKQS9";
    private string ProxyAccountContractAddress = "2M24EKAecggCnttZ9DUUMCXi4xC67rozA87kFgid9qEwRUMHTs";
    private string ProxyAccountContractAddressSide = "2jjLR4N9ZvMAPvEEQTipqpUU7bFSaUxzqF1ADYhv6EmGszSC1r";
    private string CA = "2u6Dd139bHvZJdZ835XnNKL5y6cxqzV9PEWD5fZdQXdFZLgevc";
    private string CASide = "2ptQUF1mm1cmF3v8uwB83iFCD46ynHLt4fxYoPNpCWRSBXwAEJ";

    private string CaHash = "f36b25ec892719d8e15c648204acc741c8612bf948ecb7ed2c2daca11eae4a08";

    private string seedSymbol = "SEED-309";
    private string collectionSymbol = "DXIHMULN-0";
    private string nftSymbol = "DXIHMULN-1";
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

        if (CASide.Equals(""))
            _caContractSide = new CAContract(NodeManagerSide, InitAccount);
        else
            _caContractSide = new CAContract(NodeManagerSide, InitAccount, CASide);

        _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
        _tokenContract = _genesisContract.GetTokenContract(InitAccount);

        _genesisContractSide = GenesisContract.GetGenesisContract(NodeManagerSide, InitAccount);
        _tokenContractSide = _genesisContractSide.GetTokenContract(InitAccount);
    }

    [TestMethod]
    public void CreateSeed()
    {
        for (int i = 350; i < 400; i++)
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

        var result = _tokenContract.IssueBalance(InitAccount, CaAccount,
            totalSupply, symbol);
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
    }

    [TestMethod]
    public void ApproveSeed(string symbol)
    {
        var totalSupply = 1;
        var caHash = CaHash;

        var approveInput = new AElf.Contracts.MultiToken.ApproveInput()
        {
            Spender = _tokenAdapterContract.Contract,
            Symbol = symbol,
            Amount = totalSupply
        };

        var input = new ManagerForwardCallInput
        {
            CaHash = Hash.LoadFromHex(caHash),
            ContractAddress = _tokenContract.Contract,
            MethodName = nameof(TokenContractContainer.TokenContractStub.Approve),
            Args = approveInput.ToByteString()
        };

        _caContract.SetAccount(SellerAccount);
        var transferResult = _caContract.ExecuteMethodWithResult(CAMethod.ManagerForwardCall, input);
        transferResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
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
        var SeedSymbol = seedSymbol;
        var caHash = CaHash;

        var managerCreateTokenInput = new ManagerCreateTokenInput
        {
            Symbol = symbol,
            SeedSymbol = SeedSymbol,
            Amount = 10000,
            Memo = "create token",
            TokenName = symbol + "token",
            TotalSupply = 10000,
            Decimals = 0,
            Issuer = CaAccountSide.ConvertAddress(),
            IsBurnable = true,
            // LockWhiteList = { },
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
            Owner = CaAccount.ConvertAddress()
        };

        var input = new ManagerForwardCallInput
        {
            CaHash = Hash.LoadFromHex(caHash),
            ContractAddress = _tokenAdapterContract.Contract,
            MethodName = nameof(TokenAdapterContractContainer.TokenAdapterContractStub.CreateToken),
            Args = managerCreateTokenInput.ToByteString()
        };

        _caContract.SetAccount(SellerAccount);
        var transferResult = _caContract.ExecuteMethodWithResult(CAMethod.ManagerForwardCall, input);
        transferResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        var tokenInfo = _tokenContract.GetTokenInfo(symbol);
        Logger.Info($"tokenInfo.Owner:{tokenInfo.Owner}" +
                    $"\ntokenInfo.Issuer:{tokenInfo.Issuer}");
    }

    [TestMethod]
    public void CreateNft()
    {
        var caHash = CaHash;
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

        var forwardCallInput = new ForwardCallInput
        {
            ProxyAccountHash = ownerProxyAccount.ProxyAccountHash,
            ContractAddress = _tokenContract.Contract,
            MethodName = nameof(TokenContractContainer.TokenContractStub.Create),
            Args = createInput.ToByteString()
        };

        var input = new ManagerForwardCallInput
        {
            CaHash = Hash.LoadFromHex(caHash),
            ContractAddress = _proxyAccountContract.Contract,
            MethodName = nameof(ProxyAccountContractContainer.ProxyAccountContractStub.ForwardCall),
            Args = forwardCallInput.ToByteString()
        };

        _caContract.SetAccount(SellerAccount);
        var transferResult = _caContract.ExecuteMethodWithResult(CAMethod.ManagerForwardCall, input);
        transferResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
    }

    [TestMethod]
    public void IssueNftSideProxyAccount()
    {
        var caHash = CaHash;
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
            To = CaAccountSide.ConvertAddress(),
            Amount = 100,
            Memo = "issue"
        };

        var forwardCallInput = new ForwardCallInput
        {
            ProxyAccountHash = issuerProxyAccount.ProxyAccountHash,
            ContractAddress = _tokenContractSide.Contract,
            MethodName = nameof(TokenContractContainer.TokenContractStub.Issue),
            Args = issueInput.ToByteString()
        };

        var input = new ManagerForwardCallInput
        {
            CaHash = Hash.LoadFromHex(caHash),
            ContractAddress = _proxyAccountContractSide.Contract,
            MethodName = nameof(ProxyAccountContractContainer.ProxyAccountContractStub.ForwardCall),
            Args = forwardCallInput.ToByteString()
        };

        _caContractSide.SetAccount(SellerAccount);
        var transferResult = _caContractSide.ExecuteMethodWithResult(CAMethod.ManagerForwardCall, input);
        transferResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
    }

    [TestMethod]
    public void TokenBalance()
    {
        var account = CaAccountSide;
        var symbol = "INBZGSCYJR-1";
        var balance = _tokenContractSide.GetUserBalance(account, symbol);
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
    public void GetTokenInfoTest()
    {
        var tokenInfo = _tokenContractSide.GetTokenInfo("ZHCMETCHEC-2");
        Logger.Info($"tokenInfo:{tokenInfo}");
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

    [TestMethod]
    public void HexStringToByteArray()
    {
        var rawTx =
            "0a220a20ecaed5d39a4a3af1dcaba21aefe9c7daf62b7078df46151ad7394a7e9077624912220a20f9f90416670ec1a0f2d302c9474d1bc7a475cb08caa366bcca16e2f3d7e549f5189cbb98092204f550b7ac2a124d616e61676572466f727761726443616c6c32ce020a220a20f36b25ec892719d8e15c648204acc741c8612bf948ecb7ed2c2daca11eae4a0812220a20c1caadac4da208c5a41e32cbccf990b726102e90ef704cb44a659008aa1fd53c1a0b437265617465546f6b656e22f6010a0a46564343464455482d301208534545442d32303918012201332a0231313001380042220a20ecaed5d39a4a3af1dcaba21aefe9c7daf62b7078df46151ad7394a7e9077624948015898f5756282010a350a0f5f5f6e66745f66696c655f686173681222223838393032653036366538316666306361343462333836373335316163623664220a160a125f5f6e66745f666561747572655f6861736812000a1b0a145f5f6e66745f7061796d656e745f746f6b656e731203454c460a140a0e5f5f6e66745f6d6574616461746112025b5d6a220a20d50a61efbd1cbac4c2a51b9868b795a711d754444f87f28797f627f61a8e80c182f1044188ced05a0aecf6635c6b6b77cfa2050250f9d5a8ae9b55b325b5dc46670063fc59433851e3c6b46c1a63771445d1680a0d370193ed0b70cd03812cc34712878d00";
        var byteArr = ByteArrayHelper.HexStringToByteArray(rawTx);
        var trans = Transaction.Parser.ParseFrom(byteArr);

        Console.WriteLine(trans);
    }
}