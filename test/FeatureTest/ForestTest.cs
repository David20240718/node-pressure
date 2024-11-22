using System.Text;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Forest;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using StringList = Forest.StringList;

namespace FeatureTest;

[TestClass]
public class ForestTest
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
    private WhiteListContract _whiteListContractSide;
    private WhiteListContract _newWhiteListContractSide;

    private string InitAccount { get; } = "2CpKfnoWTk69u6VySHMeuJvrX2hGrMw9pTyxcD4VM6Q28dJrhk";
    private string BuyerAccount { get; } = "2djZpW7uhneMC9ncB9BMrttyjgeCTecgDV4VcFUx7Xq5KBzTGX";
    private string SellerAccount { get; } = "4FHi2nS1MkmJL7N9WHPsNEjnSVqGgwghszfC6JMXy2KL7LNcv";
    private string WhiteListAccount { get; } = "2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV";
    private string ServiceFeeReceiver { get; } = "2HxX36oXZS89Jvz7kCeUyuWWDXLTiNRkAzfx3EuXq4KSSkH62W";
    private string AdminAccount { get; } = "2K6vev9D49RnpUgs6tQpAAytYtyVnMvWB4BcQar53rB7Feq8DC";
    private string NewAdminAccount { get; } = "2L3wYucDtAw9dfW3MLFG2GJREm5JNnjTH9MetWUG7g6xcYgruk";
    private string NotAdminAccount { get; } = "2Mf79jsGh748tK8wc75RdPqMYFWBaruU354oKJ31faFYxCpQr";
    private AuthorityManager AuthorityManager { get; set; }
    private AuthorityManager AuthorityManagerSide { get; set; }

    private static string RpcUrl { get; } = "192.168.67.18:8000";
    private static string SideRpcUrl { get; } = "192.168.66.106:8000";
    private string ForestContractAddress = "2ZpYFeE4yWjrcKLBoj1iwbfYnbo9hK7exvfGTdqcq77QSxpzNH";

    private string WhiteListContractAddress = "aceGtyU2fVcBkViZcaqZXHHjd7eNAJ6NPwbuFwhqv6He49BS1";
    private string NewWhiteListContractAddress = "2RHf2fxsnEaM3wb6N1yGqPupNZbcCY98LgWbGSFWmWzgEs5Sjo";

    private string collectionSymbol = "VSUFMNQFBT-0";
    private string nftSymbol = "VSUFMNQFBT-666";

    private string ElfSymbol = "ELF";

    [TestInitialize]
    public void Initialize()
    {
        Log4NetHelper.LogInit("ForestTest");
        Logger = Log4NetHelper.GetLogger();
        NodeInfoHelper.SetConfig("nodes");

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

        if (NewWhiteListContractAddress.Equals(""))
            _newWhiteListContractSide = new WhiteListContract(NodeManagerSide, InitAccount);
        else
            _newWhiteListContractSide =
                new WhiteListContract(NodeManagerSide, InitAccount, NewWhiteListContractAddress);

        _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
        _tokenContract = _genesisContract.GetTokenContract(InitAccount);

        _genesisContractSide = GenesisContract.GetGenesisContract(NodeManagerSide, InitAccount);
        _tokenContractSide = _genesisContractSide.GetTokenContract(InitAccount);
    }

    [TestMethod]
    public void ContractInitialize()
    {
        var serviceFeeReceiver = ServiceFeeReceiver;
        var serviceFeeRate = 10;
        var serviceFee = 1000_00000000;

        // Initialize ForestContract
        var result = _forestContractSide.Initialize(
            AdminAccount,
            serviceFeeRate,
            serviceFeeReceiver,
            serviceFee,
            _whiteListContractSide.ContractAddress
        );
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        GetAdministrator(AdminAccount);
        GetBizConfig(60, 60, 20);
    }

    [TestMethod]
    public void CreateSeed_0()
    {
        var symbol = "SEED-0";
        var name = $"{symbol} token";
        var totalSupply = 1;
        var seedOwnedSymbol = "Collection-0";
        var seedExpTime = "1720590463";

        var tokenInfo = _tokenContract.GetTokenInfo(symbol);
        Logger.Info($"create seed-0" +
                    $"\ntokenInfo:{tokenInfo}");

        var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
        {
            Symbol = symbol,
            TokenName = name,
            TotalSupply = totalSupply,
            Decimals = 0,
            Issuer = InitAccount.ConvertAddress(),
            IssueChainId = _chainId,
            IsBurnable = true,
            LockWhiteList = { _tokenContract.ContractAddress.ConvertAddress() }
        });
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        tokenInfo = _tokenContract.GetTokenInfo(symbol);
        Logger.Info($"tokenInfo:{tokenInfo}");

        MintTest(symbol);
    }


    [TestMethod]
    public void CreateSeed()
    {
        for (int i = 1; i < 100; i++)
        {
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

            var symbol = "SEED-" + i;
            var name = $"{symbol} token";
            var totalSupply = 1;
            var seedOwnedSymbol = collectionSymbol + "-0";
            var seedExpTime = "1691992920";

            var tokenInfo = _tokenContract.GetTokenInfo(symbol);
            Logger.Info($"create seed:{i}" +
                        $"\ntokenInfo:{tokenInfo}");

            if (tokenInfo.Symbol != "")
            {
                continue;
            }

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

            break;
        }
    }

    [TestMethod]
    public void BatchCreateSeed()
    {
        var totalAmount = 0;

        for (int i = 100; i < 150; i++)
        {
            var symbol = "SEED-" + i;
            var name = $"{symbol} token";
            var tokenInfo = _tokenContract.GetTokenInfo(symbol);
            Logger.Info($"create seed:{i}" +
                        $"\ntokenInfo:{tokenInfo}");

            // skip if token already exists
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

            totalAmount += 1;
            if (totalAmount == 10)
            {
                break;
            }

            tokenInfo = _tokenContract.GetTokenInfo(symbol);
            Logger.Info($"tokenInfo:{tokenInfo}");

            MintTest(symbol);
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
    public void CreateCollection()
    {
        InitializeUserBalanceMain(SellerAccount);
        var symbol = collectionSymbol;
        var name = "Collection_" + symbol;
        var totalSupply = 10000;
        var issuer = SellerAccount;

        _tokenContract.SetAccount(SellerAccount);
        var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
        {
            Symbol = symbol,
            TokenName = name,
            TotalSupply = totalSupply,
            Decimals = 0,
            Issuer = issuer.ConvertAddress(),
            IssueChainId = _chainIdSide,
            IsBurnable = true,
            ExternalInfo = new ExternalInfo()
            {
                Value =
                {
                    {
                        "__nft_image_url",
                        $"https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/{name}.jpg"
                    }
                }
            }
        });
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        var tokenInfo = _tokenContract.GetTokenInfo(symbol);
        Logger.Info($"tokenInfo:{tokenInfo}");
    }

    [TestMethod]
    public void CreateNFT()
    {
        var symbol = nftSymbol;
        var name = "NFT_" + symbol;
        var totalSupply = 10000000;
        var issuer = SellerAccount;


        _tokenContract.SetAccount(issuer);
        InitializeUserBalanceMain(issuer);
        var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
        {
            Symbol = symbol,
            TokenName = name,
            TotalSupply = totalSupply,
            Decimals = 0,
            Issuer = issuer.ConvertAddress(),
            IssueChainId = _chainIdSide,
            IsBurnable = true,
            ExternalInfo = new ExternalInfo()
            {
                Value =
                {
                    {
                        "__nft_image_url",
                        $"https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/NFT_{symbol}.jpg"
                    }
                }
            }
        });
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        var tokenInfo = _tokenContract.GetTokenInfo(symbol);
        Logger.Info($"tokenInfo:{tokenInfo}");
    }

    [TestMethod]
    public void MintNftTest()
    {
        //var symbol = "SEED-83";
        var totalSupply = 100;

        var result = _tokenContract.IssueBalance(SellerAccount, SellerAccount,
            totalSupply, nftSymbol);
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
    }

    [TestMethod]
    public void GetTokenInfoTest()
    {
        var collectionInfo = _tokenContract.GetTokenInfo("SEED-109");
        Logger.Info($"collectionInfo:{collectionInfo}");

        var collectionInfoSide = _tokenContractSide.GetTokenInfo("");
        Logger.Info($"collectionInfoSide:{collectionInfoSide}");
    }

    [TestMethod]
    public void GetBalanceTest()
    {
        var balance = _tokenContract.GetUserBalance("2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV", "SEED-109");
        Logger.Info($"balance:{balance}");
    }

    [TestMethod]
    public void MintNFTOnSideChainTest()
    {
        var symbol = nftSymbol;
        var totalSupply = 100000;

        _forestContractSide.SetAccount(SellerAccount);
        var result = _tokenContractSide.IssueBalance(SellerAccount, SellerAccount, totalSupply, symbol);
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
    }

    [TestMethod]
    public void ListWithFixedPriceTest()
    {
        // offerPrice < whitePrice < sellPrice
        var sellPrice = Elf(1_00000000);

        var quantity = 1;

        // start 5min ago
        var startTime = Timestamp.FromDateTime(DateTime.UtcNow);
        // public 10min after
        var publicTime = Timestamp.FromDateTime(DateTime.UtcNow);

        var user = NodeManager.AccountManager.NewAccount("12345678");
        Logger.Info($"user:{user}");

        _forestContractSide.SetAccount(SellerAccount);
        var result =
            _forestContractSide.ExecuteMethodWithResult(ForestContractMethod.ListWithFixedPrice,
                new ListWithFixedPriceInput
                {
                    Symbol = nftSymbol,
                    Quantity = quantity,
                    IsWhitelistAvailable = true,
                    Price = sellPrice,
                    // Whitelists = new WhitelistInfoList()
                    // {
                    //     Whitelists =
                    //     {
                    //         new WhitelistInfo()
                    //         {
                    //             PriceTag = new PriceTagInfo()
                    //             {
                    //                 TagName = "WHITELIST_WH",
                    //                 Price = whitePrice
                    //             },
                    //             AddressList = new AddressList()
                    //             {
                    //                 Value =
                    //                 {
                    //                     user.ConvertAddress()
                    //                 }
                    //             }
                    //         }
                    //     }
                    // },
                    Duration = new ListDuration()
                    {
                        // start 1sec ago
                        StartTime = startTime,
                        // public 10min after
                        PublicTime = publicTime,
                        DurationHours = 300
                    }
                });

        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        var listedNftInfo = _forestContractSide.GetListedNFTInfoList(nftSymbol, SellerAccount);
        Logger.Info("listedNftInfo:" + listedNftInfo);
    }

    [TestMethod]
    public void ListWithFixedPriceMaxCountTest()
    {
        InitializeUserBalanceSide(SellerAccount);

        for (int i = 0; i < 60; i++)
        {
            ListWithFixedPriceTest();
        }
    }

    [TestMethod]
    public void GetAllowance()
    {
        var allowance = _tokenContractSide.GetAllowance("2oiaV1BgiZyXBb2F98JQ9ynizo467DqmVTDMa46Q6mUMEQR4au",
            "2ZpYFeE4yWjrcKLBoj1iwbfYnbo9hK7exvfGTdqcq77QSxpzNH", "ELF");
        Logger.Info("allowance:" + allowance);
    }


    [TestMethod]
    public void MakeOfferTest()
    {
        InitializeUserBalanceSide(BuyerAccount);
        _tokenContractSide.ApproveToken(SellerAccount, _forestContractSide.ContractAddress, 100, nftSymbol);
        _tokenContractSide.ApproveToken(BuyerAccount, _forestContractSide.ContractAddress, 500_0000_0000, "ELF");
        var allowance = _tokenContract.GetAllowance(SellerAccount, _forestContractSide.ContractAddress, "ELF");
        Logger.Info("allowance:" + allowance);

        var balance2 = _tokenContractSide.GetUserBalance(BuyerAccount, "ELF");
        Logger.Info($"\nbalance2:{balance2}");

        var whitePrice = Elf(100); //59770000
        var quantity = 60;
        var balance1 = whitePrice.Amount * quantity;
        Logger.Info($"balance1:{balance1}" +
                    $"\nbalance2:{balance2}");

        _forestContractSide.SetAccount(BuyerAccount);
        var result =
            _forestContractSide.ExecuteMethodWithResult(ForestContractMethod.MakeOffer, new MakeOfferInput()
            {
                Symbol = nftSymbol,
                // OfferTo = SellerAccount.ConvertAddress(),
                Quantity = quantity,
                Price = whitePrice,
                ExpireTime = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(30))
            });

        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        var offerList = _forestContractSide.GetOfferList(nftSymbol, BuyerAccount);
        Logger.Info($"offerList:{offerList}");

        var balanceAfter = _tokenContractSide.GetUserBalance(BuyerAccount, "ELF");
        Logger.Info($"\nbalanceAfter:{balanceAfter}");
    }

    [TestMethod]
    public void MakeOfferMaxCountTest()
    {
        // var result = _tokenContractSide.TransferBalance(InitAccount, BuyerAccount, 100_00000000);
        // result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        for (int i = 0; i < 60; i++)
        {
            MakeOfferTest();
        }
    }

    [TestMethod]
    public void DelistTest()
    {
        var delistQuantity = 1;
        var sellPrice = Elf(5_0000_0000);

        _forestContractSide.SetAccount(SellerAccount);
        var result =
            _forestContractSide.ExecuteMethodWithResult(ForestContractMethod.Delist, new DelistInput()
            {
                Symbol = nftSymbol,
                Quantity = delistQuantity,
                Price = sellPrice
            });

        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        GetListedNftInfoListTest(nftSymbol, SellerAccount);
    }

    private static Price Elf(long amount)
    {
        return new Price()
        {
            Symbol = "ELF",
            Amount = amount
        };
    }

    private void GetListedNftInfoListTest(string token, string sellerAccount)
    {
        var listedNftInfo = _forestContractSide.GetListedNFTInfoList(token, sellerAccount);
        Logger.Info($"listedNftInfo:{listedNftInfo}");
    }

    [TestMethod]
    public void Key()
    {
        var user = NodeManager.AccountManager.GetPublicKey("5MjQGbZysnE9ivbFkM1z1T7KCRpo7PorisdL7mSjcgxewyu3N");
        Logger.Info($"p:{user}");
    }

    [TestMethod]
    public void CreateAccount()
    {
        var totalAccount = "";
        for (int i = 0; i < 100; i++)
        {
            var user = NodeManager.AccountManager.NewAccount("12345678");

            totalAccount += "\"ELF" + user + "_tDVW\",";
            Logger.Info($"totalAccount:{totalAccount}");
        }
    }

    [TestMethod]
    public void GetServerFee()
    {
        var serverFee = _forestContractSide.GetServiceFeeInfo();
        Logger.Info($"serverFee:{serverFee}");
    }

    [TestMethod]
    public void SetAdministratorTest()
    {
        ContractInitialize();
        InitializeUserBalanceSide(NotAdminAccount);
        InitializeUserBalanceSide(AdminAccount);
        InitializeUserBalanceSide(NewAdminAccount);

        {
            _forestContractSide.SetAccount(NotAdminAccount);
            var result = _forestContractSide.ExecuteMethodWithResult(ForestContractMethod.SetAdministrator,
                NewAdminAccount.ConvertAddress());
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.Error.ShouldContain("No permission.");
        }

        {
            _forestContractSide.SetAccount(AdminAccount);
            var result = _forestContractSide.ExecuteMethodWithResult(ForestContractMethod.SetAdministrator,
                "".ConvertAddress());
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            // result.Error.ShouldContain("Empty Address");
        }

        {
            _forestContractSide.SetAccount(AdminAccount);
            var result = _forestContractSide.SetAdministratorEmpty();
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            // result.Error.ShouldContain("Empty Address");
        }

        {
            _forestContractSide.SetAccount(AdminAccount);
            var newAccount = "2iPumXuyXzhNfbb8Qi9zR2vXAFQVe1EKHZ8LadcpEgR8CtUAAt";
            var result = _forestContractSide.ExecuteMethodWithResult(ForestContractMethod.SetAdministrator,
                NewAdminAccount.ConvertAddress());
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            GetAdministrator(NewAdminAccount);
        }

        {
            _forestContractSide.SetAccount(AdminAccount);
            var result = _forestContractSide.ExecuteMethodWithResult(ForestContractMethod.SetAdministrator,
                new Address());
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            GetAdministrator(NewAdminAccount);
        }
    }

    [TestMethod]
    public void ContractInitializeTest()
    {
        var serviceFeeReceiver = ServiceFeeReceiver;
        var serviceFeeRate = 10;
        var serviceFee = 1000_00000000;

        var result = _forestContractSide.ExecuteMethodWithResult(ForestContractMethod.Initialize,
            new InitializeInput
            {
                AdminAddress = AdminAccount.ConvertAddress(),
                ServiceFeeRate = serviceFeeRate,
                ServiceFeeReceiver = serviceFeeReceiver.ConvertAddress(),
                ServiceFee = serviceFee
            });
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
        result.Error.ShouldContain("Empty Address");
    }

    [TestMethod]
    public void SetWhitelistContractTest()
    {
        Initialize();
        _forestContractSide.SetAccount(AdminAccount);
        var result = _forestContractSide.ExecuteMethodWithResult(ForestContractMethod.SetWhitelistContract,
            "".ConvertAddress());
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
        result.Error.ShouldContain("Empty Address");
    }

    [TestMethod]
    public void SetGlobalTokenWhiteListTest_InvalidSymbol()
    {
        ContractInitialize();
        _forestContractSide.SetAccount(AdminAccount);
        var result = _forestContractSide.ExecuteMethodWithResult(ForestContractMethod.SetGlobalTokenWhiteList,
            new StringList()
            {
                Value = { "AAA" }
            });
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
        result.Error.ShouldContain("Invalid token :");
    }

    [TestMethod]
    public void ListWithFixedPriceTest_InvalidSymbol()
    {
        ContractInitialize();
        _forestContractSide.SetAccount(AdminAccount);
        var result = _forestContractSide.ExecuteMethodWithResult(ForestContractMethod.ListWithFixedPrice,
            new ListWithFixedPriceInput
            {
                Symbol = "AAAAA-1",
                Quantity = 1,
                IsWhitelistAvailable = true,
                Price = Elf(5_00000000)
            });

        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
        result.Error.ShouldContain("this NFT Info not exists.");
    }

    [TestMethod]
    public void SetTokenWhiteListTest_InvalidSymbol()
    {
        // ContractInitialize();
        // CreateSeed();
        // CreateCollection();
        // InitializeUserBalanceMain(SellerAccount);

        _forestContractSide.SetAccount(SellerAccount);
        var result = _forestContractSide.ExecuteMethodWithResult(ForestContractMethod.SetTokenWhiteList,
            new SetTokenWhiteListInput
            {
                Symbol = collectionSymbol,
                TokenWhiteList = new StringList()
                {
                    Value = { "AAA" }
                }
            });

        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
        result.Error.ShouldContain("Invalid token :");
    }

    [TestMethod]
    public void SetTokenWhiteListTest()
    {
        // ContractInitialize();
        _forestContractSide.SetAccount(SellerAccount);
        var result = _forestContractSide.ExecuteMethodWithResult(ForestContractMethod.SetTokenWhiteList,
            new SetTokenWhiteListInput
            {
                Symbol = collectionSymbol,
                TokenWhiteList = new StringList()
                {
                    Value = { "ELF" }
                }
            });

        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
    }

    [TestMethod]
    private void GetAdministrator(string expectAdmin)
    {
        var admin = _forestContractSide.GetAdministrator();
        Logger.Info($"admin:{admin}");
        admin.ShouldBe(expectAdmin.ConvertAddress());
    }

    [TestMethod]
    private void GetBizConfig(int expectMaxListCount, int expectMaxOfferCount, int expectMaxTokenWhitelistCount)
    {
        var bizConfig = _forestContractSide.GetBizConfig();
        bizConfig.MaxListCount.ShouldBe(expectMaxListCount);
        bizConfig.MaxOfferCount.ShouldBe(expectMaxOfferCount);
        bizConfig.MaxTokenWhitelistCount.ShouldBe(expectMaxTokenWhitelistCount);
    }

    private void InitializeUserBalanceMain(string account)
    {
        var balance = _tokenContract.GetUserBalance(account);

        if (balance <= 0)
        {
            var result = _tokenContract.TransferBalance(InitAccount, account, 100_00000000);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        balance = _tokenContract.GetUserBalance(account);
        Logger.Info($"\nbalance:{balance}");
    }

    [TestMethod]
    public void SetBizConfig()
    {
        // ContractInitialize();

        InitializeUserBalanceMain(AdminAccount);
        _forestContractSide.SetAccount(AdminAccount);
        var result = _forestContractSide.ExecuteMethodWithResult(ForestContractMethod.SetBizConfig,
            new BizConfig()
            {
                MaxListCount = 60,
                MaxOfferCount = 60,
                MaxTokenWhitelistCount = 20
            });

        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        GetBizConfig(60, 60, 20);
    }

    private void InitializeUserBalanceSide(string account)
    {
        var balance = _tokenContractSide.GetUserBalance(account);

        if (balance <= 0)
        {
            var result = _tokenContractSide.TransferBalance(InitAccount, account, 100_00000000);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        balance = _tokenContractSide.GetUserBalance(account);
        Logger.Info($"\nbalance:{balance}");
    }

    [TestMethod]
    public void GetTokenInfo()
    {
        var symbol = "SEED-0";

        var tokenInfo = _tokenContract.GetTokenInfo(symbol);
        Logger.Info($"tokenInfo:{tokenInfo}");
    }
}