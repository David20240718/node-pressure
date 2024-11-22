using AElf;
using AElf.Contracts.MultiToken;
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
public class EoaCreateNftTestOnline
{
    private ILog Logger { get; set; }
    private INodeManager NodeManager { get; set; }
    private int _chainId;
    private GenesisContract _genesisContract;
    private TokenContract _tokenContract;
    private AuthorityManager AuthorityManager { get; set; }

    private string InitAccount { get; } = "";
    private static string RpcUrl { get; } = "";

    [TestInitialize]
    public void Initialize()
    {
        Log4NetHelper.LogInit("create token");
        Logger = Log4NetHelper.GetLogger();

        NodeManager = new NodeManager(RpcUrl);
        AuthorityManager = new AuthorityManager(NodeManager, InitAccount);
        _chainId = ChainHelper.ConvertBase58ToChainId(NodeManager.GetChainId());

        _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
        _tokenContract = _genesisContract.GetTokenContract(InitAccount);
    }

    [TestMethod]
    public void InputToBase64()
    {
        var createInput = new AElf.Contracts.MultiToken.CreateInput()
        {
            Symbol = "SEED-100000003",
            TokenName = "SEED-FINTST",
            TotalSupply = 1,
            Decimals = 0,
            Issuer = "2ouCNM5MR76iJ1YN1V21Uv5idXKAeMugVrtnLFDMmbYp9E8bdJ".ConvertAddress(),
            IsBurnable = true,
            LockWhiteList = { "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE".ConvertAddress() },
            IssueChainId = 9992731,
            ExternalInfo = new ExternalInfo()
            {
                Value =
                {
                    {
                        "__seed_owned_symbol",
                        "FINTST"
                    },
                    {
                        "__seed_exp_time",
                        "1754275326"
                    },
                    {
                        "__nft_image_url",
                        "https://seed-collecion.s3.ap-southeast-1.amazonaws.com/SEED-100000003.svg"
                    }
                }
            },
            Owner = "ER6zCGM4snUyob6f9eB5oELAPzmLNe3nhga4oxto1KXTqaRP2".ConvertAddress()
        };
        Logger.Info($"createInput:{createInput.ToByteString().ToBase64()}");
    }

    [TestMethod]
    public void CreateCollection()
    {
        var createInput = new CreateInput
        {
            Symbol = "ARUNXT-0",
            TokenName = "ARUNXT collection",
            TotalSupply = 1,
            Decimals = 0,
            Issuer = InitAccount.ConvertAddress(),
            IssueChainId = _chainId,
            IsBurnable = true,
            // LockWhiteList = { _tokenContract.ContractAddress.ConvertAddress() },
            ExternalInfo = new ExternalInfo()
            {
                Value =
                {
                    {
                        "__nft_image_url",
                        "https://aelf-app.s3.ap-northeast-1.amazonaws.com/img/NFT1.svg"
                    }
                }
            },
            Owner = InitAccount.ConvertAddress()
        };
        var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create, createInput);
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
    }

    [TestMethod]
    public void CreateNFT()
    {
        var createInput = new CreateInput
        {
            Symbol = "ARUNXT-1",
            TokenName = "ARUNXT-1 item",
            TotalSupply = 1000,
            Decimals = 0,
            Issuer = InitAccount.ConvertAddress(),
            IssueChainId = _chainId,
            IsBurnable = true,
            // LockWhiteList = { _tokenContract.ContractAddress.ConvertAddress() },
            ExternalInfo = new ExternalInfo()
            {
                Value =
                {
                    {
                        "__nft_image_url",
                        "https://aelf-app.s3.ap-northeast-1.amazonaws.com/img/NFT2.svg"
                    }
                }
            },
            Owner = InitAccount.ConvertAddress()
        };
        var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create, createInput);
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
    }

    [TestMethod]
    public void CreateFT()
    {
        var createInput = new CreateInput
        {
            Symbol = "FINTST",
            TokenName = "FINTST token",
            TotalSupply = 1000000000_00000000,
            Decimals = 8,
            Issuer = InitAccount.ConvertAddress(),
            IssueChainId = _chainId,
            IsBurnable = true,
            // LockWhiteList = { _tokenContract.ContractAddress.ConvertAddress() },
            // ExternalInfo = new ExternalInfo()
            // {
            //     Value =
            //     {
            //         {
            //             "__nft_image_url",
            //             "https://aelf-app.s3.ap-northeast-1.amazonaws.com/img/NFT1.svg"
            //         }
            //     }
            // },
            Owner = InitAccount.ConvertAddress()
        };
        var result = _tokenContract.ExecuteMethodWithResult(TokenMethod.Create, createInput);
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
    }

    [TestMethod]
    public void GetTokenInfo()
    {
        var tokenInfo = _tokenContract.GetTokenInfo("FINTST");
        Logger.Info($"tokenInfo:{tokenInfo}");
    }
}