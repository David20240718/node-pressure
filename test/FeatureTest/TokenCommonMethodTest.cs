using AElf.Client.Service;
using AElf.Contracts.MultiToken;
using AElf.Standards.ACS1;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class TokenCommonMethodTest
    {
        private ILog Logger { get; set; }
        private int _chainId;
        private INodeManager NodeManagerMain { get; set; }
        private INodeManager NodeManagerSide1 { get; set; }
        private GenesisContract _genesisContractMain;
        private TokenContract _tokenContractMain;
        private TokenContractContainer.TokenContractStub _tokenSubMain;

        private GenesisContract _genesisContractSide;
        private TokenContract _tokenContractSide;
        private TokenContractContainer.TokenContractStub _tokenSubSide;
        public INodeManager NodeManager { get; set; }

        private string InitAccount { get; } = "23GxsoW9TRpLqX1Z5tjrmcRMMSn5bhtLAf4HtPj8JX9BerqTqp";

        private static string RpcUrlMain { get; } = "http://192.168.67.72:8000";
        private static string RpcUrlSide { get; } = "http://192.168.66.47:8000";//http://192.168.66.113:8000
        private string Symbol { get; } = "ELF";
        private AuthorityManager AuthorityManager { get; set; }
        private CrossChainManager CrossChainManager { get; set; }

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("TokenContractTest");
            Logger = Log4NetHelper.GetLogger();
            // NodeInfoHelper.SetConfig("nodes-env2-main");

            NodeManagerMain = new NodeManager(RpcUrlMain);
            NodeManagerSide1 = new NodeManager(RpcUrlSide);
            _genesisContractMain = GenesisContract.GetGenesisContract(NodeManagerMain, InitAccount);
            _tokenContractMain = _genesisContractMain.GetTokenContract(InitAccount);
            _tokenSubMain = _genesisContractMain.GetTokenStub(InitAccount);
            AuthorityManager = new AuthorityManager(NodeManagerMain, InitAccount);

            _genesisContractSide = GenesisContract.GetGenesisContract(NodeManagerSide1, InitAccount);
            _tokenContractSide = _genesisContractSide.GetTokenContract(InitAccount);
            _tokenSubSide = _genesisContractSide.GetTokenStub(InitAccount);

            CrossChainManager = new CrossChainManager(NodeManagerMain, NodeManagerSide1, InitAccount);
        }

        [TestMethod]
        public void GetTokenBalance()
        {
            var address = InitAccount;
            var symbol = "ELF";

            var initBefore = _tokenContractMain.GetUserBalance(address, symbol);
            var toBefore = _tokenContractSide.GetUserBalance(address, symbol);
            Logger.Info($"\ninitBefore:{initBefore}" +
                        $"\ntoBefore:{toBefore}");
        }

        [TestMethod]
        public void TokenBalanceMain()
        {
            var fromAddress = InitAccount;
            var toAddress = "7WZ9c95um6jgGwYkr6RRaKQcHCAV4V21VoUgjzC6yexxXDAC9";
            var symbol = "ELF"; //ELF WRITE READ CPU
            var transferAmount = 1000_00000000;

            var transfer = _tokenContractMain.TransferBalance(fromAddress, toAddress, transferAmount, symbol);
            transfer.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var initAfter = _tokenContractMain.GetUserBalance(fromAddress, symbol);
            var toAfter = _tokenContractMain.GetUserBalance(toAddress, symbol);
            Logger.Info($"\ninitAfter:{initAfter}" +
                        $"\ntoAfter:{toAfter}");
        }


        // [TestMethod]
        // public void TransactionFeeClaimedLogTest()
        // {
        //     var LogStr = "CgNFTEYQuNGcDBoiCiAnkemSpX8o51oR8TrywK7IsOs10vBI1C66iQHJLgN43A==";
        //   var Logs = TransactionFeeClaimed.Parser.ParseFrom(ByteString.FromBase64(LogStr));
        //   Logger.Info($"TransactionFeeClaimedLog:({Logs})");
        // }

        [TestMethod]
        public void ResourceTokenClaimedLogTest()
        {
            var LogStr =
                "CgVXUklURRCQThoiCiARGWK0FlMgXjGNYNky95Dd8652TfkeFdtfYa9alspEbSIiCiAXixnHql3bsWKhE9gPjlVJXQMbMWBHp8kZsgoH1eoW8Q==";
         //   var Logs = ResourceTokenClaimed.Parser.ParseFrom(ByteString.FromBase64(LogStr));
         //   Logger.Info($"ResourceTokenClaimedLog:({Logs})");
        }

        [TestMethod]
        public void RentalChargedLogTest()
        {
            var LogStr =
                "CgNORVQQgBAaIgogtmEeND5WbIwmRQRHQSR5ImutsYmB2nZViQp3mXJXEfsiIgogF4sZx6pd27FioRPYD45VSV0DGzFgR6fJGbIKB9XqFvE=";
            var Logs = RentalCharged.Parser.ParseFrom(ByteString.FromBase64(LogStr));
            Logger.Info($"RentalChargedLog:({Logs})");
        }

        [TestMethod]
        public void RentalAccountBalanceInsufficientLogTest()
        {
            var LogStr = "CgNORVQQgBA=";
            var Logs = RentalAccountBalanceInsufficient.Parser.ParseFrom(ByteString.FromBase64(LogStr));
            Logger.Info($"RentalAccountBalanceInsufficientLog:({Logs})");
        }

        [TestMethod]
        public async Task CreateTest()
        {
            _chainId = ChainHelper.ConvertBase58ToChainId(NodeManagerMain.GetChainId());
            
            // Create
            var result = await _tokenSubMain.Create.SendAsync(new CreateInput
            {
                Symbol = "SEED-0",
                TokenName = "SEED",
                TotalSupply = 1000000000,
                Decimals = 0,
                Issuer = InitAccount.ConvertAddress(),
                IsBurnable = true,
                // LockWhiteList = ,
                IssueChainId = _chainId,
                // ExternalInfo = new ExternalInfo
                // {
                //     Value =
                //     {
                //         {"assemble", "metadata_assemble"}
                //     }
                // }
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var tokenInfo = _tokenContractMain.GetTokenInfo("SEED-0");
            Logger.Info($"tokenInfo.Symbol is {tokenInfo.Symbol}");
            Logger.Info($"tokenInfo.TokenName is {tokenInfo.TokenName}");
            Logger.Info($"tokenInfo.Supply is {tokenInfo.Supply}");
            Logger.Info($"tokenInfo.TotalSupply is {tokenInfo.TotalSupply}");
            Logger.Info($"tokenInfo.Decimals is {tokenInfo.Decimals}");
            Logger.Info($"tokenInfo.Issuer is {tokenInfo.Issuer}");
            Logger.Info($"tokenInfo.IsBurnable is {tokenInfo.IsBurnable}");
            Logger.Info($"tokenInfo.IssueChainId is {tokenInfo.IssueChainId}");
            Logger.Info($"tokenInfo.Issued is {tokenInfo.Issued}");
            Logger.Info($"tokenInfo.ExternalInfo is {tokenInfo.ExternalInfo}");
        }

        [TestMethod]
        public void IssueTest()
        {
            var symbol = "WH";
            var issueAmount = 10;
            var toAddress = "2pWGXdunSNkW9ad8ZUsiKcoTu4UCQc91GZLmgVX6kfcm6zsgzt";

            var issueResult = _tokenContractMain.ExecuteMethodWithResult(TokenMethod.Issue, new IssueInput
            {
                Symbol = symbol,
                To = toAddress.ConvertAddress(),
                Amount = issueAmount
            });
            issueResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var initBefore = _tokenContractMain.GetUserBalance(toAddress, symbol);
            Logger.Info($"\ninitBefore:{initBefore}");
        }
        
        [TestMethod]
        public void SetTokenContractCreateMethodFee()
        {
            var fee = _tokenContractMain.CallViewMethod<MethodFees>(TokenMethod.GetMethodFee, new StringValue
            {
                Value = nameof(TokenMethod.Create)
            });
            Logger.Info(fee);

            // var organization =
            //     _tokenContractMain.CallViewMethod<AuthorityInfo>(TokenMethod.GetMethodFeeController, new Empty())
            //         .OwnerAddress;
            // var input = new MethodFees
            // {
            //     MethodName = nameof(TokenMethod.Create),
            //     Fees =
            //     {
            //         
            //     },
            //     IsSizeFeeFree = false
            // };
            // var result = AuthorityManager.ExecuteTransactionWithAuthority(_tokenContractMain.ContractAddress,
            //     "SetMethodFee", input,
            //     InitAccount, organization);
            // result.Status.ShouldBe(TransactionResultStatus.Mined);
        }
    }
}