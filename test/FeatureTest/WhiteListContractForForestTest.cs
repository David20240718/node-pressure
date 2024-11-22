using Forest.Whitelist;
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
using Nethereum.Hex.HexConvertors.Extensions;
using Shouldly;
using AddressList = Forest.Whitelist.AddressList;


namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class WhiteListContractForForestTest
    {
        private WhiteListContract _whiteListContract;

        private ILog Logger { get; set; }
        private INodeManager NodeManager { get; set; }
        private AuthorityManager AuthorityManager { get; set; }

        private string InitAccount { get; } = "2CpKfnoWTk69u6VySHMeuJvrX2hGrMw9pTyxcD4VM6Q28dJrhk";
        private string ManagersAddress { get; } = "2YYt1HaXdZHBycyHYctLgzdufC1MUpjxSfLsAezgTBBHcFoAto";
        private string ManagersAddress1 { get; } = "FHdcx45K5kovWsAKSb3rrdyNPFus8eoJ1XTQE7aXFHTgfpgzN";
        private string ManagersAddress2 { get; } = "2NKnGrarMPTXFNMRDiYH4hqfSoZw72NLxZHzgHD1Q3xmNoqdmR";
        private string ManagersAddress3 { get; } = "NUddzDNy8PBMUgPCAcFW7jkaGmofDTEmr5DUoddXDpdR6E85X";

        private string UserAddress { get; } = "WcAkZMw4kAt7NkzFdes5CA55BGumnnu8Gj5vbUz2gYeLS1tzd";
        private string UserAddress1 { get; } = "29qJkBMWU2Sv6mTqocPiN8JTjcsSkBKCC8oUt11fTE7oHmfG3n";
        private string UserAddress2 { get; } = "puEKG7zUqusWZRiULssPnwKDc2ZSL3q1oWFfatHisGnD9P1EL";
        private string UserAddress3 { get; } = "1DskqyVKjWQm6iev5GtSegv1bP8tz1ZTWQZC2MTogTQoMhv4q";
        private string UserAddress4 { get; } = "W6YQXwoGHM25DZgCB2dsB95Zzb7LbUkYdEe347q8J1okMgB9z";

        private static string RpcUrl { get; } = "http://192.168.67.18:8000";

        private string WhitelistAddress = "GwsSp1MZPmkMvXdbfSCDydHhZtDpvqkFpmPvStYho288fb7QZ";

        [TestInitialize]
        public void Initialize()
        {
            Log4NetHelper.LogInit("WhiteListContractTest");
            Logger = Log4NetHelper.GetLogger();
            NodeInfoHelper.SetConfig("nodes");

            NodeManager = new NodeManager(RpcUrl);
            AuthorityManager = new AuthorityManager(NodeManager, InitAccount);

            if (WhitelistAddress.Equals(""))
                _whiteListContract = new WhiteListContract(NodeManager, InitAccount);
            else
                _whiteListContract = new WhiteListContract(NodeManager, InitAccount, WhitelistAddress);
        }

        [TestMethod]
        public void InitializeTest()
        {
            _whiteListContract.SetAccount(InitAccount);
            var initialize =
                _whiteListContract.ExecuteMethodWithResult(WhiteListContractMethod.Initialize, new Empty());

            initialize.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void CreateWhitelist_ForForest()
        {
            var isCloneable = true;
            var remark = "this is mark2add tag hash once again，hsh";
            var creator = InitAccount.ConvertAddress();
            var info1 = new PriceTag { Symbol = "ELF", Amount = 2_0000000 }.ToByteString();
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");

            _whiteListContract.SetAccount(InitAccount);
            var createWhitelist = _whiteListContract.CreateWhitelist(
                new ExtraInfoList
                {
                    Value =
                    {
                        new ExtraInfo
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress.ConvertAddress() }
                            },
                            Info = new TagInfo
                            {
                                TagName = "WHITELIST_TAG",
                                Info = info1
                            }
                        }
                    }
                },
                isCloneable,
                remark,
                creator,
                new AddressList
                {
                    Value = { ManagersAddress.ConvertAddress(), ManagersAddress1.ConvertAddress() }
                },
                projectId,
                StrategyType.Price,
                out var output
            );
            Logger.Info($"output is {output}");
            createWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var id = HashHelper.ComputeFrom($"{output}{projectId}{"First"}");
            Logger.Info($"taghash is {id}");

            var whitelist = _whiteListContract.GetWhitelist(output);
            Logger.Info($"whitelist is {whitelist}");
            // whitelist.WhitelistId.ShouldBe(output);
            // whitelist.ExtraInfoIdList.Value[0].AddressList.Value[0].ShouldBe(UserAddress.ConvertAddress());
            // whitelist.ExtraInfoIdList.Value[0].Id.ShouldBe(id);
            // whitelist.IsAvailable.ShouldBe(true);
            // whitelist.IsCloneable.ShouldBe(isCloneable);
            // whitelist.Remark.ShouldBe(remark);
            // whitelist.CloneFrom.ShouldBeNull();
            // whitelist.Creator.ShouldBe(creator);
            // whitelist.Manager.Value[0].ShouldBe(ManagersAddress.ConvertAddress());
        }

        [TestMethod]
        public void AddExtraInfo()
        {
            var whitelistId = Hash.LoadFromHex("0e04061c768b6ce03584989900c84bb20c30518ff4a5f6a52a81a3a3dc8f8f79");
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");
            var info = new Price { Symbol = "ELF", Amount = 2_0000000 }.ToByteString();
            var owner = InitAccount.ConvertAddress();
            var id = HashHelper.ComputeFrom($"{whitelistId}{projectId}{"Three"}");
            var waddress = "2HEuewuh5KfjGSZ3VVyUnG7kHXWFcNymYSc27q5QL6pdPX1TNM";

            _whiteListContract.SetAccount(InitAccount);
            var addExtraInfo = _whiteListContract.AddExtraInfo
            (
                whitelistId,
                projectId,
                new TagInfo
                {
                    TagName = "WHITELISTNEWT_TAG",
                    Info = info
                },
                new AddressList
                {
                    Value = { Address.FromPublicKey("CCC".HexToByteArray()), waddress.ConvertAddress() }
                }
                //UserAddress4.ConvertAddress()
                //
            );
            addExtraInfo.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var getExtraInfoIdList = _whiteListContract.GetExtraInfoIdList(whitelistId, projectId);
            Logger.Info($"getExtraInfoIdList is {getExtraInfoIdList}");
            Logger.Info($"taghash is {id}");

            // var whitelist = _whiteListContract.GetWhitelist(whitelistId);
            // Logger.Info($"whitelist is {whitelist}");
            // var getTagInfoByHash = _whiteListContract.GetTagInfoByHash(id);
            // Logger.Info($"getTagInfoByHash is {getTagInfoByHash}");
            // getTagInfoByHash.TagName.ShouldBe("Three");
            // getTagInfoByHash.Info.ShouldBe(info);
        }
    }
}