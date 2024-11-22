using AElf;
using AElf.Contracts.MultiToken;
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
using log4net.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Portkey.Contracts.CA;
using Secp256k1Net;
using Shouldly;

namespace FeatureTest;

[TestClass]
public class UnitTest2
{
    private ILog Logger { get; set; }
    private INodeManager NodeManager { get; set; }
    private INodeManager SideNodeManager { get; set; }
    private int _chainId;
    private int _sidechainId;
    private GenesisContract _genesisContract;
    private TokenContract _tokenContract;
    private ForestContract _forestContract;
    private WhiteListContract _whiteListContract;
    private TokenAdapterContract _tokenAdapterContract;
    private ProxyAccountContract _proxyAccountContract;
    private ProxyAccountContract _proxyAccountContractSide;
    private CAContract _caContract;
    private CAContract _caContractSide;

    private string InitAccount { get; } =
        "2CpKfnoWTk69u6VySHMeuJvrX2hGrMw9pTyxcD4VM6Q28dJrhk"; // "2CpKfnoWTk69u6VySHMeuJvrX2hGrMw9pTyxcD4VM6Q28dJrhk"; //"2KQWh5v6Y24VcGgsx2KHpQvRyyU5DnCZ4eAUPqGQbnuZgExKaV";
    //"2qD6TkC1VWUg5z1qWnf3WXknv8LmN5mCdWGsm7sga5KREhg4BA"; // "2PKdu2LKMNv55CUyhQJ7J6xX8rk9G21tZvUvXukQRhMsWJES63";

    private string BuyerAccount { get; } = "2HEuewuh5KfjGSZ3VVyUnG7kHXWFcNymYSc27q5QL6pdPX1TNM";

    private string SellerAccount { get; } =
        "ekYNs9FijhfzUqaivsS6dLp9mnrdoCtXMqXoCZLPQeaJzC8GU"; //"21WSN7eWp8wNC5Hu7PDBYXJ4bYC9pAun2wvjPqry284hSQbK8A";//"21k5PYBftofVeEphhSRDFabbEPfEKj33J1hjnbsrw6NWNzJb3r";//"ekYNs9FijhfzUqaivsS6dLp9mnrdoCtXMqXoCZLPQeaJzC8GU";//"2F4LMecoz7GSRtmequHFdFuYT8Czw9EEwfbJs9JaSCTdLDkhVo";//"2oEkQHdMXjn5RkwPKyYR33GE5Q4Yo4QKHjAMUiob9kMfLin2d9";//"UyFdS4n6UFo9k8GUKddCtEoqtntYsHXucf4j9tMBJqQTpqpd5";//"ucxifHkQcqob3zL3c2WppfEQn46oADEmm6bJdHsfNgX8ye2tw";//"25CYb3bVT8fFYjS9SvTTE8J8WRJkQEky34EbCgaCAFHHq74UpW";//"2ekTauVWtKWVdBJgE5KZuRrRbj2T6FRFWTWgxGbFQhqcgaX1AT";//"2qD6TkC1VWUg5z1qWnf3WXknv8LmN5mCdWGsm7sga5KREhg4BA";
    //   "2beD6La6iMMT46t5YakkhviEpdiVGsZBbcjcDj6B3THjTJfr6u"; //"H5tr8o9wnf8qKkpFa5CoWHFGAwEBBCjFSsAyhjDNcHGG1wcLf";ELF_rgyDpY63TRmFHhzGwNTthxq9ZNMqi5jNnmngU4q6cvbLarva6_tDVW

    private string WhiteListAccount { get; } = "2PKdu2LKMNv55CUyhQJ7J6xX8rk9G21tZvUvXukQRhMsWJES63";
    //"2atQSZ2tHFsiKppT75VK2sPM71aTAt4gk2Jvo1Q3Qmw5GkmLrH"; //"ip2bFXdqNnEtCzvRfjKBREab5oXNKDr52ZrLnS3u9wm8ASESa";

    private string ServiceFeeReceiver { get; } = "2CgBFJkSq59upK1buGHgYAPfSCGGXB5dZaxukxHDwtU8Hv4KEU";

    private string CAAccount { get; } =
        "2cpqSG5ooyVyyxBEg6XeUgKzoHFkGDQF4Q2KkXrcGnqaGtf1Xd"; // "PDQyX9Nn9rX6WneduPfproxUUHLCfgmfHLT7FWty1GpJxqxAH";//"2cpqSG5ooyVyyxBEg6XeUgKzoHFkGDQF4Q2KkXrcGnqaGtf1Xd";

    private string CASIDE { get; } = "2oiaV1BgiZyXBb2F98JQ9ynizo467DqmVTDMa46Q6mUMEQR4au";

    private AuthorityManager AuthorityManager { get; set; }
    private string AgentInterfaceContractAddress = "2UM9eusxdRyCztbmMZadGXzwgwKfFdk8pF4ckw58D769ehaPSR";
    private string ProxyAccountContractAddress = "2M24EKAecggCnttZ9DUUMCXi4xC67rozA87kFgid9qEwRUMHTs";
    private string ProxyAccountContractAddressSide = "2jjLR4N9ZvMAPvEEQTipqpUU7bFSaUxzqF1ADYhv6EmGszSC1r";
    private string CA = "2u6Dd139bHvZJdZ835XnNKL5y6cxqzV9PEWD5fZdQXdFZLgevc";
    private string CASide = "2ptQUF1mm1cmF3v8uwB83iFCD46ynHLt4fxYoPNpCWRSBXwAEJ";


    private static string RpcUrl { get; } = "192.168.67.18:8000";
    private static string SideRpcUrl { get; } = "192.168.66.106:8000";

    private string
        ForestContractAddress =
            "225ajURvev5rgX8HnMJ8GjbPnRxUrCHoD7HUjhWQqewEJ5GAv1"; //"2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS";

    private string
        WhiteListContractAddress =
            "GwsSp1MZPmkMvXdbfSCDydHhZtDpvqkFpmPvStYho288fb7QZ"; //"2WHXRoLRjbUTDQsuqR5CntygVfnDb125qdJkudev4kVNbLhTdG";

    private string NftSymbol = "TANGCHEN-4";
    private string ElfSymbol = "ELF";
    private string CaHash = "f36b25ec892719d8e15c648204acc741c8612bf948ecb7ed2c2daca11eae4a08";
    private string seedSymbol = "SEED-357";
    private string collectionSymbol = "KHHWLUFM-0";

    [TestInitialize]
    public void Initialize()
    {
        Log4NetHelper.LogInit("ForestTest");
        Logger = Log4NetHelper.GetLogger();
        // NodeInfoHelper.SetConfig("nodes");

        NodeManager = new NodeManager(RpcUrl);
        SideNodeManager = new NodeManager(SideRpcUrl);
        AuthorityManager = new AuthorityManager(NodeManager, InitAccount);
        _chainId = ChainHelper.ConvertBase58ToChainId(NodeManager.GetChainId());
        _sidechainId = ChainHelper.ConvertBase58ToChainId(SideNodeManager.GetChainId());

        if (ForestContractAddress.Equals(""))
            _forestContract = new ForestContract(NodeManager, InitAccount);

        else
            _forestContract = new ForestContract(NodeManager, InitAccount, ForestContractAddress);


        if (WhiteListContractAddress.Equals(""))
            _whiteListContract = new WhiteListContract(NodeManager, InitAccount);
        else
            _whiteListContract = new WhiteListContract(NodeManager, InitAccount, WhiteListContractAddress);
        Logger.Info("_whiteListContract:" + _whiteListContract.ContractAddress);
        Logger.Info("_forestContract:" + _forestContract.ContractAddress);

        _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
        _tokenContract = _genesisContract.GetTokenContract(InitAccount);
    }

    [TestMethod]
    public void ContractInitialize()
    {
        var serviceFeeReceiver = ServiceFeeReceiver;
        var serviceFeeRate = 10;
        var serviceFee = 1000_00000000;

        // Initialize ForestContract
        var result = _forestContract.Initialize(
            InitAccount,
            serviceFeeRate,
            serviceFeeReceiver,
            serviceFee,
            _whiteListContract.ContractAddress
        );
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        // Set WhitelistContract
        result = _forestContract.SetWhitelistContract(_whiteListContract.ContractAddress);
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
    }

    [TestMethod]
    public void CreateCollection(string seed, string seedsymbol)
    {
        var NftSymbol = seed;
        var name = "Nature leaf";
        var totalSupply = 1;

        var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
        {
            Symbol = NftSymbol,
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
                        // "__nft_image_url",
                        // "https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/Untitled/3.jpg"
                        "__seed_owned_symbol",
                        seedsymbol
                    },
                    {
                        "__seed_exp_time",
                        "1792145642"
                    }
                }
            }
        });
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        var tokenInfo = _tokenContract.GetTokenInfo(NftSymbol);
        //Logger.Info($"tokenInfo:{tokenInfo}");

        var result1 = _tokenContract.IssueBalance(InitAccount, SellerAccount, totalSupply, NftSymbol);
        result1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        var account = _tokenContract.GetUserBalance(CAAccount, NftSymbol);
        //  var account2 = _tokenContract.GetUserBalance(SellerAccount, "SEED-20");
        // Logger.Info("account  " + account);

        //  ApproveSeed(seed)
    }

    [TestMethod]
    public string GetRandomstring()
    {
        Random random = new Random();
        string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        int length = 4;
        int length1 = 3;
        int length2 = 1;
        string randomString = new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        string randomString1 = new string(Enumerable.Repeat(chars, length1)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        string randomString2 = new string(Enumerable.Repeat(chars, length2)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        string SeeDNftSymbol = randomString + randomString1 + randomString2 + "-0";

        return SeeDNftSymbol;
    }

    [TestMethod]
    public void BatchCreateSeed()
    {
        Dictionary<string, string> seedDictionary = new
            Dictionary<string, string>();

        for (int i = 1; i < 7; i++)
        {
            int gap = 4000;
            int sum = i + gap;
            string SEED = "SEED-" + sum;
            string SeeDNftSymbol;
            SeeDNftSymbol = GetRandomstring();

            try
            {
                CreateCollection(SEED, SeeDNftSymbol);
            }

            catch (Exception ex)
            {
                Logger.Info($"The function call failed, and the seed creation failed" + SEED + ex);
            }

            seedDictionary.Add(SEED, SeeDNftSymbol);
            Logger.Info($"SEED:" + SEED + "  " + SeeDNftSymbol);
        }

        foreach (KeyValuePair<string, string> pair in seedDictionary)
        {
            Console.WriteLine("Key: " + pair.Key + ", Value: " + pair.Value);
        }
    }

    [TestMethod]
    public void ApproveSeed_EOA()

    {
        string seed = seedSymbol;
        var totalSupply = 1;
        _tokenContract.SetAccount(SellerAccount);
        var result = _tokenContract.ApproveToken(SellerAccount, _tokenAdapterContract.ContractAddress,
            totalSupply, seed);
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
    public void CreateCollection2()
    {
        //var NftSymbol = "THIRDAY-1";

        var NftSymbol = "JINMINGTUESSTTTTTTTT-1";
        var name = "Nature leaf";
        var totalSupply = 100;


        _tokenContract.SetAccount(SellerAccount);
        var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
        {
            Symbol = NftSymbol,
            TokenName = name,
            TotalSupply = 1000,
            Decimals = 0,
            Issuer = SellerAccount.ConvertAddress(),
            IssueChainId = 1931928,
            IsBurnable = true,
            ExternalInfo = new ExternalInfo()
            {
                Value =
                {
                    {
                        "__nft_image_url",
                        "https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/Untitled/3.jpg"
                    },
                }
            }
        });
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        var tokenInfo = _tokenContract.GetTokenInfo(NftSymbol);
        Logger.Info($"tokenInfo:{tokenInfo}");
    }


    [TestMethod]
    public void PrepareNftData()
    {
        // var name = "Nature leafe";
        var totalSupply = 1;
        // var tokenInfo2 = _tokenContract.GetUserBalance(InitAccount, "ELF");
        //
        // var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
        // {
        //     Symbol = NftSymbol,
        //     TokenName = name,
        //     TotalSupply = totalSupply,
        //     Decimals = 0,
        //     Issuer = InitAccount.ConvertAddress(),
        //     IssueChainId = _chainId,
        //     IsBurnable = true,
        //     ExternalInfo = new ExternalInfo()
        //     {
        //         Value =
        //         {
        //             {
        //                 "__nft_image_url",
        //                 "https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/Untitled/3.jpg"
        //             }
        //         }
        //     }
        // });
        // result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        //
        // var tokenInfo = _tokenContract.GetTokenInfo(NftSymbol);
        // Logger.Info($"tokenInfo:{tokenInfo}");
        //


        var result = _tokenContract.IssueBalance(InitAccount, SellerAccount, totalSupply, "SEED-122");
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        var account = _tokenContract.GetUserBalance(SellerAccount, "SEED-122");
        //  var account2 = _tokenContract.GetUserBalance(SellerAccount, "SEED-20");
        Logger.Info("account  " + account);
    }

    [TestMethod]
    public void InitializeData()
    {
        var sellerBalance = _tokenContract.GetUserBalance(SellerAccount);
        var buyerBalance = _tokenContract.GetUserBalance(BuyerAccount);
        var sellerNFTBalance = _tokenContract.GetUserBalance(SellerAccount, NftSymbol);

        if (sellerBalance <= 0)
        {
            var result = _tokenContract.TransferBalance(InitAccount, SellerAccount, 100_00000000);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        if (buyerBalance <= 0)
        {
            var result = _tokenContract.TransferBalance(InitAccount, BuyerAccount, 100_00000000);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        if (sellerNFTBalance <= 0)
        {
            var result = _tokenContract.TransferBalance(InitAccount, SellerAccount, 10, NftSymbol);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        sellerBalance = _tokenContract.GetUserBalance(SellerAccount);
        buyerBalance = _tokenContract.GetUserBalance(BuyerAccount);
        sellerNFTBalance = _tokenContract.GetUserBalance(SellerAccount, NftSymbol);
        Logger.Info($"\nsellerBalance:{sellerBalance}" +
                    $"\nbuyerBalance:{buyerBalance}" +
                    $"\nsellerNFTBalance:{sellerNFTBalance}");
    }

    [TestMethod]
    public void ListWithFixedPriceTest()
    {
        // offerPrice < whitePrice < sellPrice
        var sellPrice = Elf(9_0000_0000);
        var whitePrice = Elf(5_0000_0000);
        var offerPrice = Elf(10_0000_0000);

        var quantity = 1;

        // start 5min ago
        var startTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(5));
        // public 10min after
        var publicTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(10));

        _forestContract.SetAccount(SellerAccount);
        var result =
            _forestContract.ExecuteMethodWithResult(ForestContractMethod.ListWithFixedPrice, new ListWithFixedPriceInput
            {
                //   Symbol = "FRIRE-1",
                // Symbol =NftSymbol,
                //  TANGCHEN-1
                Symbol = "TANGCHEN-2",
                Quantity = quantity,
                IsWhitelistAvailable = true,
                Price = sellPrice,
                Whitelists = new WhitelistInfoList()
                {
                    Whitelists =
                    {
                        new WhitelistInfo()
                        {
                            PriceTag = new PriceTagInfo()
                            {
                                TagName = "WHITELISTTY_TAG",
                                Price = whitePrice
                            },
                            AddressList = new AddressList()
                            {
                                Value =
                                {
                                    WhiteListAccount.ConvertAddress()
                                }
                            }
                        }
                    }
                },
                Duration = new ListDuration()
                {
                    // start 1sec ago
                    StartTime = startTime,
                    // public 10min after
                    PublicTime = publicTime,
                    DurationHours = 1,
                },
            });

        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        var listedNftInfo = _forestContract.GetListedNFTInfoList(NftSymbol, InitAccount);
        Logger.Info("listedNftInfo:" + listedNftInfo.Value);
    }

    [TestMethod]
    public void MakeOfferTest()
    {
        var whitePrice = Elf(2_0000_0000);
        var quantity = 2;

        _forestContract.SetAccount(SellerAccount);
        var result =
            _forestContract.ExecuteMethodWithResult(ForestContractMethod.MakeOffer, new MakeOfferInput()
            {
                // Symbol = NftSymbol,
                Symbol = "TANGCHEN-1",
                OfferTo = BuyerAccount.ConvertAddress(),
                Quantity = quantity,
                Price = whitePrice,
                ExpireTime = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(30))
            });

        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
    }

    [TestMethod]
    public void CancelOfferTest()
    {
        MakeOfferTest();
        _forestContract.SetAccount(BuyerAccount);
        var list0 = _forestContract.GetOfferList(NftSymbol, BuyerAccount);
        var count = list0.Value.Count;

        var result =
            _forestContract.ExecuteMethodWithResult(ForestContractMethod.CancelOffer, new CancelOfferInput()
            {
                Symbol = NftSymbol,
                OfferFrom = BuyerAccount.ConvertAddress(),
                IndexList = new Int32List()
                {
                    Value = { 0 }
                }
            });
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        var list = _forestContract.GetOfferList(NftSymbol, BuyerAccount);
        var count2 = list.Value.Count;
        //两次相减删除一个1
        (count - count2).ShouldBe(1);
        Logger.Info("list::" + list);
    }

    [TestMethod]
    public void TransferTest()
    {
        var token = _tokenContract.GetTokenInfo(NftSymbol);
        Logger.Info("token" + token);

        _chainId = ChainHelper.ConvertBase58ToChainId(NodeManager.GetChainId());

        Logger.Info("_chainId" + _chainId);


        //   var tokenInfo1 = _tokenContract.GetUserBalance(InitAccount, NftSymbol);
        var tokenInfo1 = _tokenContract.GetUserBalance(SellerAccount, "ELF");
        Logger.Info("tokenamount" + tokenInfo1);

        // var result = _tokenContract.TransferBalance(InitAccount, SellerAccount, 1, NftSymbol);
        var result = _tokenContract.TransferBalance(InitAccount, SellerAccount, 5000000_00000000, "ELF");

        //  var tokenInfo2 = _tokenContract.GetUserBalance(InitAccount, NftSymbol);
        var tokenInfo2 = _tokenContract.GetUserBalance(SellerAccount, "ELF");
        Logger.Info("tokenamount" + tokenInfo2);

        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
    }

    [TestMethod]
    public void DealTest()
    {
        var sellPrice = Elf(3_0000_0000);
        var whitePrice = Elf(2_0000_0000);
        var offerPrice = Elf(2_0000_0000);
        var ServiceFeeRate = 0.1;
        var offerQuantity = 2;
        var dealQuantity = 1;
        var serviceFee = dealQuantity * sellPrice.Amount * ServiceFeeRate / 10000;

        var tokenInfo = _tokenContract.GetUserBalance(BuyerAccount, "ELF");
        var tokenInfo0 = _tokenContract.GetUserBalance(SellerAccount, "ELF");
        var tokenInfo1 = _tokenContract.GetUserBalance(BuyerAccount, NftSymbol);
        var tokenInfo2 = _tokenContract.GetUserBalance(SellerAccount, NftSymbol);
        Logger.Info("BuyerAccountNELF::" + tokenInfo);
        Logger.Info("BuyerAccountNFT::" + tokenInfo1);
        Logger.Info("SellerAccountELF::" + tokenInfo0);
        Logger.Info("SellerAccountNFT::" + tokenInfo2);

        MakeOfferTest();
        var list = _forestContract.GetOfferList("TANGCHEN-1", SellerAccount);
        Logger.Info("list::" + list);


        //buy as whitePrice 
        //  _tokenContract.TransferBalance(InitAccount, BuyerAccount, 1, NftSymbol);
        _forestContract.SetAccount(BuyerAccount);
        _tokenContract.ApproveToken(BuyerAccount, _forestContract.ContractAddress, 1, "TANGCHEN-1");
        _tokenContract.ApproveToken(SellerAccount, _forestContract.ContractAddress, 500_0000_0000, "ELF");
        var allowance = _tokenContract.GetAllowance(SellerAccount, BuyerAccount, "ELF");
        Logger.Info("allowance::" + allowance);


        var result = _forestContract.ExecuteMethodWithResult(ForestContractMethod.Deal, new DealInput()
        {
            Symbol = NftSymbol,
            Price = offerPrice,
            OfferFrom = SellerAccount.ConvertAddress(),
            Quantity = dealQuantity
        });
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        var list1 = _forestContract.GetOfferList("TANGCHEN-1", SellerAccount);
        Logger.Info("list::" + list1);
    }

    [TestMethod]
    public void BurnTest()
    {
        _forestContract.SetAccount(InitAccount);
        var accountbefor = _tokenContract.GetUserBalance(InitAccount, NftSymbol);
        Logger.Info("NftSymbol:  " + NftSymbol);
        Logger.Info("accountbefor:  " + accountbefor);

        var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Burn, new BurnInput
            {
                Symbol = NftSymbol,
                Amount = 1
            }
        );
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        var accountafer = _tokenContract.GetUserBalance(InitAccount, NftSymbol);
        Logger.Info("accountafer:  " + accountafer);
        //verify after burn nfts is reduced by 1
        (accountbefor - accountafer).ShouldBe(1);
    }


    private static Price Elf(long amunt)
    {
        return new Price()
        {
            Symbol = "ELF",
            Amount = amunt
        };
    }
}