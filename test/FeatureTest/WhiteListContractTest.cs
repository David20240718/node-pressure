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
using Shouldly;
using AddressList = Forest.Whitelist.AddressList;


namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class WhiteListContractTest
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
        private string UserAddress5 { get; } = "H5tr8o9wnf8qKkpFa5CoWHFGAwEBBCjFSsAyhjDNcHGG1wcLf";
        private string UserAddress6 { get; } = "ucxifHkQcqob3zL3c2WppfEQn46oADEmm6bJdHsfNgX8ye2tw";

        private static string RpcUrl { get; } = "http://192.168.66.106:8000";

        private string WhitelistAddress = "aceGtyU2fVcBkViZcaqZXHHjd7eNAJ6NPwbuFwhqv6He49BS1";

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
            var remark = "1234567";
            var creator = InitAccount.ConvertAddress();
            var info1 = new PriceTag { Symbol = "ELF", Amount = 2_00000000 }.ToByteString();
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
                                Value = { UserAddress2.ConvertAddress() }
                            },
                            Info = new TagInfo
                            {
                                TagName = "WHITELISTYT_AGFYY",
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
            var whitelist = _whiteListContract.GetWhitelist(output);
            Logger.Info($"whitelist is {whitelist}");
            whitelist.WhitelistId.ShouldBe(output);
//            whitelist.ExtraInfoIdList.Value[0].AddressList.Value[0].ShouldBe(UserAddress.ConvertAddress());
            whitelist.ExtraInfoIdList.Value[0].Id.ShouldBe(id);
            whitelist.IsAvailable.ShouldBe(true);
            whitelist.IsCloneable.ShouldBe(isCloneable);
            whitelist.Remark.ShouldBe(remark);
            whitelist.CloneFrom.ShouldBeNull();
            whitelist.Creator.ShouldBe(creator);
            whitelist.Manager.Value[0].ShouldBe(ManagersAddress.ConvertAddress());
        }

        [TestMethod]
        public void CreateWhitelist()
        {
            var isCloneable = true;
            var remark = "111111222";
            var creator = InitAccount.ConvertAddress();
            var info1 = new PriceTag { Symbol = "ELF", Amount = 10_00000000 }.ToByteString();
            var info2 = new PriceTag { Symbol = "ELF", Amount = 20_00000000 }.ToByteString();
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
                                TagName = "First",
                                Info = info1
                            }
                        },
                        new ExtraInfo
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress1.ConvertAddress() }
                            },
                            Info = new TagInfo
                            {
                                TagName = "TWO",
                                Info = info2
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
            var whitelist = _whiteListContract.GetWhitelist(output);
            Logger.Info($"whitelist is {whitelist}");
            whitelist.WhitelistId.ShouldBe(output);
            whitelist.ExtraInfoIdList.Value[0].AddressList.Value[0].ShouldBe(UserAddress.ConvertAddress());
            whitelist.ExtraInfoIdList.Value[0].Id.ShouldBe(id);
            whitelist.IsAvailable.ShouldBe(true);
            whitelist.IsCloneable.ShouldBe(isCloneable);
            whitelist.Remark.ShouldBe(remark);
            whitelist.CloneFrom.ShouldBeNull();
            whitelist.Creator.ShouldBe(creator);
            whitelist.Manager.Value[0].ShouldBe(ManagersAddress.ConvertAddress());
            whitelist.Manager.Value[1].ShouldBe(ManagersAddress1.ConvertAddress());

            var whitelistIdList = _whiteListContract.GetWhitelistByManager(InitAccount.ConvertAddress());
            Logger.Info($"whitelistIdList is {whitelistIdList}");
            whitelistIdList.WhitelistId[0].ShouldBe(output);

            var addressList = _whiteListContract.GetManagerList(output);
            Logger.Info($"addressList is {addressList}");
            addressList.Value[2].ShouldBe(InitAccount.ConvertAddress());
            addressList.Value[0].ShouldBe(ManagersAddress.ConvertAddress());
        }

        [TestMethod]
        public void AddExtraInfo()
        {
            var whitelistId = Hash.LoadFromHex("063fddb3cf116a181add33a06620cd2812967b861c094a56cbc9a7cfc2f907ef");
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");
            var info = new Price { Symbol = "ELF", Amount = 90_00000000 }.ToByteString();
            var owner = InitAccount.ConvertAddress();
            var id = HashHelper.ComputeFrom($"{whitelistId}{projectId}{"Three"}");

            _whiteListContract.SetAccount(InitAccount);
            var addExtraInfo = _whiteListContract.AddExtraInfo
            (
                whitelistId,
                projectId,
                new TagInfo
                {
                    TagName = "THETHIRD",
                    Info = info
                },
                new AddressList
                {
                    Value = { UserAddress3.ConvertAddress() }
                }
            );
            addExtraInfo.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var getExtraInfoIdList = _whiteListContract.GetExtraInfoIdList(whitelistId, projectId);
            Logger.Info($"getExtraInfoIdList is {getExtraInfoIdList}");

            var whitelist = _whiteListContract.GetWhitelist(whitelistId);
            Logger.Info($"whitelist is {whitelist}");
            var getTagInfoByHash = _whiteListContract.GetTagInfoByHash(id);
            Logger.Info($"getTagInfoByHash is {getTagInfoByHash}");
            getTagInfoByHash.TagName.ShouldBe("Three");
            getTagInfoByHash.Info.ShouldBe(info);
        }

        [TestMethod]
        public void AddAddressInfoListToWhitelist()
        {
            var info = new Price { Symbol = "ELF", Amount = 20_00000000 }.ToByteString();
            var whitelistId = Hash.LoadFromHex("73dcfc99f4766d74bbb9eaacec260cae106cb2cf7b7368b04348eaab231581bf");
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");
            var id = HashHelper.ComputeFrom($"{whitelistId}{projectId}{"TWO"}");

            _whiteListContract.SetAccount(InitAccount);
            var addAddressInfoListToWhitelist = _whiteListContract.AddAddressInfoListToWhitelist
            (
                whitelistId,
                new ExtraInfoIdList
                {
                    Value =
                    {
                        new ExtraInfoId
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress2.ConvertAddress() }
                            },
                            Id = id
                        },
                        new ExtraInfoId
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress3.ConvertAddress() }
                            },
                            Id = id
                        }
                    }
                }
            );
            addAddressInfoListToWhitelist.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var extraInfoList = _whiteListContract.GetWhitelistDetail(whitelistId);
            Logger.Info($"extraInfoList is {extraInfoList}");
            extraInfoList.Value.Count.ShouldBe(3);
            extraInfoList.Value[1].AddressList.Value[1].ShouldBe(UserAddress2.ConvertAddress());
            extraInfoList.Value[1].Info.TagName.ShouldBe("TWO");
            extraInfoList.Value[1].AddressList.Value[2].ShouldBe(UserAddress3.ConvertAddress());
            // Check event--WhitelistDisabledß
            var logs = addAddressInfoListToWhitelist.Logs.First(l => l.Name.Equals("WhitelistAddressInfoAdded"))
                .NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var whitelistAddressInfoAdded = WhitelistAddressInfoAdded.Parser.ParseFrom(byteString);
            Logger.Info($"whitelistAddressInfoAdded is {whitelistAddressInfoAdded}");
            whitelistAddressInfoAdded.WhitelistId.ShouldBe(whitelistId);
            whitelistAddressInfoAdded.ExtraInfoIdList.Value[0].ShouldBe
            (new ExtraInfoId
            {
                AddressList = new AddressList
                {
                    Value = { UserAddress2.ConvertAddress() }
                },
                Id = id
            });
            whitelistAddressInfoAdded.ExtraInfoIdList.Value[1].ShouldBe
            (new ExtraInfoId
            {
                AddressList = new AddressList
                {
                    Value = { UserAddress3.ConvertAddress() }
                },
                Id = id
            });
        }

        [TestMethod]
        public void RemoveAddressInfoListFromWhitelist()
        {
            var whitelistId = Hash.LoadFromHex("73dcfc99f4766d74bbb9eaacec260cae106cb2cf7b7368b04348eaab231581bf");
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");
            var id = HashHelper.ComputeFrom($"{whitelistId}{projectId}{"TWO"}");

            _whiteListContract.SetAccount(InitAccount);
            var removeAddressInfoListFromWhitelist = _whiteListContract.RemoveAddressInfoListFromWhitelist
            (
                whitelistId,
                new ExtraInfoIdList
                {
                    Value =
                    {
                        new ExtraInfoId
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress2.ConvertAddress() }
                            },
                            Id = id
                        },
                        new ExtraInfoId
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress3.ConvertAddress() }
                            },
                            Id = id
                        }
                    }
                }
            );
            removeAddressInfoListFromWhitelist.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            var extraInfoList = _whiteListContract.GetWhitelistDetail(whitelistId);
            Logger.Info($"extraInfoList is {extraInfoList}");
            extraInfoList.Value.Count.ShouldBe(3);
            // Check event--WhitelistDisabled
            var logs = removeAddressInfoListFromWhitelist.Logs.First(l => l.Name.Equals("WhitelistAddressInfoRemoved"))
                .NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var whitelistAddressInfoRemoved = WhitelistAddressInfoRemoved.Parser.ParseFrom(byteString);
            Logger.Info($"whitelistAddressInfoRemoved is {whitelistAddressInfoRemoved}");
            whitelistAddressInfoRemoved.WhitelistId.ShouldBe(whitelistId);
            whitelistAddressInfoRemoved.ExtraInfoIdList.Value[0].ShouldBe
            (new ExtraInfoId
            {
                AddressList = new AddressList
                {
                    Value = { UserAddress2.ConvertAddress() }
                },
                Id = id
            });
            whitelistAddressInfoRemoved.ExtraInfoIdList.Value[1].ShouldBe
            (new ExtraInfoId
            {
                AddressList = new AddressList
                {
                    Value = { UserAddress3.ConvertAddress() }
                },
                Id = id
            });
        }

        [TestMethod]
        public void RemoveTagInfo()
        {
            var whitelistId = Hash.LoadFromHex("73dcfc99f4766d74bbb9eaacec260cae106cb2cf7b7368b04348eaab231581bf");
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");
            var info = new Price { Symbol = "ELF", Amount = 30_00000000 }.ToByteString();
            var owner = InitAccount.ConvertAddress();
            var tagId = HashHelper.ComputeFrom($"{whitelistId}{projectId}{"Three"}");
            Logger.Info($"tagId is {tagId}");

            _whiteListContract.SetAccount(InitAccount);
            var removeAddressInfoFromWhitelist = _whiteListContract.RemoveAddressInfoListFromWhitelist
            (
                whitelistId,
                new ExtraInfoIdList
                {
                    Value =
                    {
                        new ExtraInfoId
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress4.ConvertAddress() }
                            },
                            Id = tagId
                        }
                    }
                }
            );
            removeAddressInfoFromWhitelist.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            //RemoveTagInfo
            var removeTagInfo = _whiteListContract.RemoveTagInfo
            (
                whitelistId,
                projectId,
                tagId
            );
            removeTagInfo.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            // Check event--WhitelistDisabled
            var logs = removeTagInfo.Logs.First(l => l.Name.Equals("TagInfoRemoved")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var tagInfoRemoved = TagInfoRemoved.Parser.ParseFrom(byteString);
            Logger.Info($"tagInfoRemoved is {tagInfoRemoved}");
            tagInfoRemoved.TagInfoId.ShouldBe(tagId);
            tagInfoRemoved.TagInfo.TagName.ShouldBe("Three");
            tagInfoRemoved.TagInfo.Info.ShouldBe(info);
            tagInfoRemoved.ProjectId.ShouldBe(projectId);
            tagInfoRemoved.WhitelistId.ShouldBe(whitelistId);
        }

        [TestMethod]
        public void DisableWhitelist()
        {
            var whitelistId = Hash.LoadFromHex("73dcfc99f4766d74bbb9eaacec260cae106cb2cf7b7368b04348eaab231581bf");
            _whiteListContract.SetAccount(InitAccount);
            var disableWhitelist = _whiteListContract.DisableWhitelist(whitelistId);
            disableWhitelist.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            // Check event--WhitelistDisabled
            var logs = disableWhitelist.Logs.First(l => l.Name.Equals("WhitelistDisabled")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var whiteListDisabled = WhitelistDisabled.Parser.ParseFrom(byteString);
            whiteListDisabled.WhitelistId.ShouldBe(whitelistId);
            whiteListDisabled.IsAvailable.ShouldBe(false);
        }

        [TestMethod]
        public void EnableWhitelist()
        {
            var whitelistId = Hash.LoadFromHex("73dcfc99f4766d74bbb9eaacec260cae106cb2cf7b7368b04348eaab231581bf");
            _whiteListContract.SetAccount(InitAccount);
            var enableWhitelist = _whiteListContract.EnableWhitelist(whitelistId);
            enableWhitelist.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            // Check event--WhitelistReenable
            var logs = enableWhitelist.Logs.First(l => l.Name.Equals("WhitelistReenable")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var whitelistReenable = WhitelistReenable.Parser.ParseFrom(byteString);
            whitelistReenable.WhitelistId.ShouldBe(whitelistId);
            whitelistReenable.IsAvailable.ShouldBe(true);
        }

        [TestMethod]
        public void ChangeWhitelistCloneable()
        {
            var whitelistId = Hash.LoadFromHex("73dcfc99f4766d74bbb9eaacec260cae106cb2cf7b7368b04348eaab231581bf");
            var isCloneable = false;
            var isCloneable1 = true;

            _whiteListContract.SetAccount(InitAccount);
            //例：关闭克隆
            var changeWhitelistCloneable = _whiteListContract.ChangeWhitelistCloneable
            (
                whitelistId,
                isCloneable
            );
            changeWhitelistCloneable.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            // Check event--IsCloneableChanged
            var logs = changeWhitelistCloneable.Logs.First(l => l.Name.Equals("IsCloneableChanged")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var isCloneableChanged = IsCloneableChanged.Parser.ParseFrom(byteString);
            isCloneableChanged.WhitelistId.ShouldBe(whitelistId);
            isCloneableChanged.IsCloneable.ShouldBe(false);


            //例：开启克隆
            var changeWhitelistCloneable1 = _whiteListContract.ChangeWhitelistCloneable
            (
                whitelistId,
                isCloneable1
            );
            changeWhitelistCloneable1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            // Check event
            logs = changeWhitelistCloneable1.Logs.First(l => l.Name.Equals("IsCloneableChanged")).NonIndexed;
            byteString = ByteString.FromBase64(logs);
            isCloneableChanged = IsCloneableChanged.Parser.ParseFrom(byteString);
            isCloneableChanged.WhitelistId.ShouldBe(whitelistId);
            isCloneableChanged.IsCloneable.ShouldBe(true);
        }

        [TestMethod]
        public void UpdateExtraInfo()
        {
            var whitelistId = Hash.LoadFromHex("063fddb3cf116a181add33a06620cd2812967b861c094a56cbc9a7cfc2f907ef");
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");
            var tagId = HashHelper.ComputeFrom($"{whitelistId}{projectId}{"Three"}");
            var tagId1 = HashHelper.ComputeFrom($"{whitelistId}{projectId}{"THETHIRD"}");

            _whiteListContract.SetAccount(InitAccount);
            var whitelistExtra = _whiteListContract.GetExtraInfoByAddress(whitelistId, UserAddress3.ConvertAddress());
            Logger.Info($"whitelistExtra is {whitelistExtra}");
            var updateExtraInfo = _whiteListContract.UpdateExtraInfo
            (
                whitelistId,
                new ExtraInfoId
                {
                    AddressList = new AddressList
                    {
                        Value = { UserAddress3.ConvertAddress() }
                    },
                    Id = tagId1
                }
            );

            updateExtraInfo.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var whitelistExtra1 = _whiteListContract.GetExtraInfoByAddress(whitelistId, UserAddress3.ConvertAddress());
            Logger.Info($"whitelistExtra1 is {whitelistExtra1}");
            var whitelist = _whiteListContract.GetWhitelist(whitelistId);
            Logger.Info($"whitelist is {whitelist}");
            whitelist.WhitelistId.ShouldBe(whitelistId);
            whitelist.ExtraInfoIdList.Value[1].AddressList.Value[1].ShouldBe(UserAddress.ConvertAddress());
            whitelist.ExtraInfoIdList.Value[1].Id.ShouldBe(tagId1);
            // Check event
            var logs = updateExtraInfo.Logs.First(l => l.Name.Equals("ExtraInfoUpdated")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var extraInfoUpdated = ExtraInfoUpdated.Parser.ParseFrom(byteString);
            Logger.Info($"extraInfoUpdated is {extraInfoUpdated}");
            extraInfoUpdated.WhitelistId.ShouldBe(whitelistId);
            extraInfoUpdated.ExtraInfoIdBefore.Id.ShouldBe(tagId);
            extraInfoUpdated.ExtraInfoIdBefore.AddressList.Value[0].ShouldBe(UserAddress.ConvertAddress());
            extraInfoUpdated.ExtraInfoIdAfter.Id.ShouldBe(tagId1);
            extraInfoUpdated.ExtraInfoIdAfter.AddressList.Value[0].ShouldBe(UserAddress.ConvertAddress());
        }

        [TestMethod]
        public void TransferManager()
        {
            var whitelistId = Hash.LoadFromHex("73dcfc99f4766d74bbb9eaacec260cae106cb2cf7b7368b04348eaab231581bf");
            var manager = UserAddress.ConvertAddress();
            var manager1 = ManagersAddress.ConvertAddress();
            /*
            //开启白名单
            _whiteListContract.SetAccount(InitAccount);
            var enableWhitelist = _whiteListContract.EnableWhitelist(whitelistId);
            enableWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            */

            _whiteListContract.SetAccount(ManagersAddress);
            var transferManager = _whiteListContract.TransferManager
            (
                whitelistId,
                manager
            );
            transferManager.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var getManagerList = _whiteListContract.GetManagerList(whitelistId);
            Logger.Info($"getManagerList is {getManagerList}");
            //getManagerList.Value[0].ShouldBe(UserAddress.ConvertAddress());
            // Check event--ManagerTransferred
            var logs = transferManager.Logs.First(l => l.Name.Equals("ManagerTransferred")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var managerTransferred = ManagerTransferred.Parser.ParseFrom(byteString);
            managerTransferred.WhitelistId.ShouldBe(whitelistId);
            managerTransferred.TransferFrom.ShouldBe(ManagersAddress.ConvertAddress());
            managerTransferred.TransferTo.ShouldBe(UserAddress.ConvertAddress());


            _whiteListContract.SetAccount(UserAddress);
            var transferManager1 = _whiteListContract.TransferManager
            (
                whitelistId,
                manager1
            );
            transferManager1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var getManagerList1 = _whiteListContract.GetManagerList(whitelistId);
            Logger.Info($"getManagerList1 is {getManagerList1}");
        }

        [TestMethod]
        public void AddManagers()
        {
            var whitelistId = Hash.LoadFromHex("73dcfc99f4766d74bbb9eaacec260cae106cb2cf7b7368b04348eaab231581bf");
            _whiteListContract.SetAccount(InitAccount);
            var addManagers = _whiteListContract.AddManagers
            (
                whitelistId,
                new AddressList
                {
                    Value = { ManagersAddress2.ConvertAddress(), ManagersAddress3.ConvertAddress() }
                }
            );
            addManagers.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var addressList = _whiteListContract.GetManagerList(whitelistId);
            Logger.Info($"addressList is {addressList}");
            addressList.Value.Count.ShouldBe(5);
            addressList.Value[3].ShouldBe(ManagersAddress2.ConvertAddress());
            addressList.Value[4].ShouldBe(ManagersAddress3.ConvertAddress());
            // Check event--ManagerAdded
            var logs = addManagers.Logs.First(l => l.Name.Equals("ManagerAdded")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var managerAdded = ManagerAdded.Parser.ParseFrom(byteString);
            managerAdded.WhitelistId.ShouldBe(whitelistId);
            managerAdded.ManagerList.Value[0].ShouldBe(ManagersAddress2.ConvertAddress());
            managerAdded.ManagerList.Value[1].ShouldBe(ManagersAddress3.ConvertAddress());
        }


        [TestMethod]
        public void RemoveManagers()
        {
            var whitelistId = Hash.LoadFromHex("73dcfc99f4766d74bbb9eaacec260cae106cb2cf7b7368b04348eaab231581bf");
            _whiteListContract.SetAccount(InitAccount);
            var removeManagers = _whiteListContract.RemoveManagers
            (
                whitelistId,
                new AddressList
                {
                    Value = { ManagersAddress2.ConvertAddress(), ManagersAddress3.ConvertAddress() }
                }
            );
            removeManagers.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var addressList = _whiteListContract.GetManagerList(whitelistId);
            Logger.Info($"addressList is {addressList}");
            addressList.Value.Count.ShouldBe(3);
            // Check event
            var logs = removeManagers.Logs.First(l => l.Name.Equals("ManagerRemoved")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var managerRemoved = ManagerRemoved.Parser.ParseFrom(byteString);
            Logger.Info($"managerRemoved is {managerRemoved}");
            managerRemoved.WhitelistId.ShouldBe(whitelistId);
            managerRemoved.ManagerList.Value[0].ShouldBe(ManagersAddress2.ConvertAddress());
            managerRemoved.ManagerList.Value[1].ShouldBe(ManagersAddress3.ConvertAddress());
        }

        [TestMethod]
        public void ResetWhitelist()
        {
            //ea663c76b3484a4c0622ea0b01f6bb50a842e3dfb795d39a2ad6351984f501ef_已删除
            var whitelistId = Hash.LoadFromHex("063fddb3cf116a181add33a06620cd2812967b861c094a56cbc9a7cfc2f907ef");
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");
            //开启白名单
            _whiteListContract.SetAccount(InitAccount);
            // var enableWhitelist = _whiteListContract.EnableWhitelist(whitelistId);
            // enableWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //  var disableWhitelist = _whiteListContract.DisableWhitelist(whitelistId);
            //  disableWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //  _whiteListContract.SetAccount(ManagersAddress);
            var manager = _whiteListContract.GetManagerList(whitelistId);
            Logger.Info($"manager is {manager}");


            var resetWhitelist = _whiteListContract.ResetWhitelist
            (
                whitelistId,
                projectId
            );
            resetWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var whitelist = _whiteListContract.GetWhitelist(whitelistId);
            Logger.Info($"whitelist is {whitelist}");
            var whitelistIdList = _whiteListContract.GetWhitelistByManager(InitAccount.ConvertAddress());
            Logger.Info($"whitelistIdList is {whitelistIdList}");

            // Check event
            var logs = resetWhitelist.Logs.First(l => l.Name.Equals("WhitelistReset")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var whitelistReset = WhitelistReset.Parser.ParseFrom(byteString);
            Logger.Info($"whitelistReset is {whitelistReset}");
            whitelistReset.WhitelistId.ShouldBe(whitelistId);
            whitelistReset.ProjectId.ShouldBe(projectId);
        }

        //Subscribers.
        [TestMethod]
        public void SubscribeWhitelist()
        {
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");
            var whitelistId = Hash.LoadFromHex("08901ebf2812af9e4f899212023bf44dab9a96487d4eb558edf7f36bea12617d");
            var id = HashHelper.ComputeFrom($"{whitelistId}{projectId}{"First"}");

            _whiteListContract.SetAccount(UserAddress2, "123456");
            var subscribeWhitelist = _whiteListContract.SubscribeWhitelist
            (
                projectId,
                whitelistId,
                out var output
            );
            Logger.Info($"output is {output}");
            subscribeWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var subscribeId = HashHelper.ComputeFrom($"{UserAddress2.ConvertAddress()}{projectId}{whitelistId}");
            Logger.Info($"subscribeId is {subscribeId}");
            var getSubscribeWhitelist = _whiteListContract.GetSubscribeWhitelist(output);
            Logger.Info($"getSubscribeWhitelist is {getSubscribeWhitelist}");
            getSubscribeWhitelist.SubscribeId.ShouldBe(output);
            getSubscribeWhitelist.WhitelistId.ShouldBe(whitelistId);
            getSubscribeWhitelist.ProjectId.ShouldBe(projectId);

            // Check event
            var logs = subscribeWhitelist.Logs.First(l => l.Name.Equals("WhitelistSubscribed")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var whiteListSubscribed = WhitelistSubscribed.Parser.ParseFrom(byteString);
            whiteListSubscribed.WhitelistId.ShouldBe(whitelistId);
            whiteListSubscribed.ProjectId.ShouldBe(projectId);
            whiteListSubscribed.SubscribeId.ShouldBe(output);
        }

        [TestMethod]
        public void ConsumeWhitelist()
        {
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");
            var whitelistId = Hash.LoadFromHex("2e399d3e913557ba3b032f4014f6523bf941e138df7413fcb3b06c628f98c232");
            var subscribeId = HashHelper.ComputeFrom($"{UserAddress2.ConvertAddress()}{projectId}{whitelistId}");
            Logger.Info($"subscribeId is {subscribeId}");
            var id = HashHelper.ComputeFrom($"{InitAccount.ConvertAddress()}{projectId}{"First"}");

            var getTagInfoByHash = _whiteListContract.GetTagInfoByHash(id);
            Logger.Info($"getTagInfoByHash is {getTagInfoByHash}");

            var getExtraInfoFromWhitelist = _whiteListContract.GetExtraInfoFromWhitelist
            (
                whitelistId,
                new ExtraInfoId
                {
                    AddressList = new AddressList
                    {
                        Value = { UserAddress.ConvertAddress() }
                    },
                    Id = id
                }
            );
            Logger.Info($"getSubscribeWhitelist is {getExtraInfoFromWhitelist}");

            var getAvailableWhitelist = _whiteListContract.GetAvailableWhitelist(subscribeId);
            Logger.Info($"getAvailableWhitelist is {getAvailableWhitelist}");

            //例：      
            _whiteListContract.SetAccount(UserAddress);
            var consumeWhitelist = _whiteListContract.ConsumeWhitelist
            (
                subscribeId,
                whitelistId,
                new ExtraInfoId
                {
                    AddressList = new AddressList
                    {
                        Value = { UserAddress2.ConvertAddress() }
                    },
                    Id = id
                }
            );
            consumeWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var getConsumedList = _whiteListContract.GetConsumedList(subscribeId);
            Logger.Info($"getConsumedList is {getConsumedList}");
            getConsumedList.SubscribeId.ShouldBe(subscribeId);
            getConsumedList.WhitelistId.ShouldBe(whitelistId);
            getConsumedList.ExtraInfoIdList.Value[0].AddressList.Value[0].ShouldBe(UserAddress.ConvertAddress());
            getConsumedList.ExtraInfoIdList.Value[0].Id.ShouldBe(id);

            // Check event
            var logs = consumeWhitelist.Logs.First(l => l.Name.Equals("ConsumedListAdded")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var consumedListAdded = ConsumedListAdded.Parser.ParseFrom(byteString);
            Logger.Info($"consumedListAdded is {consumedListAdded}");
            consumedListAdded.WhitelistId.ShouldBe(whitelistId);
            consumedListAdded.SubscribeId.ShouldBe(subscribeId);
            consumedListAdded.ExtraInfoIdList.Value[0].AddressList.Value[0].ShouldBe(UserAddress.ConvertAddress());
            consumedListAdded.ExtraInfoIdList.Value[0].Id.ShouldBe(id);
        }

        [TestMethod]
        public void UnsubscribeWhitelist()
        {
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");
            var whitelistId = Hash.LoadFromHex("2e399d3e913557ba3b032f4014f6523bf941e138df7413fcb3b06c628f98c232");
            var subscribeId = HashHelper.ComputeFrom($"{UserAddress2.ConvertAddress()}{projectId}{whitelistId}");
            Logger.Info($"subscribeId is {subscribeId}");

            _whiteListContract.SetAccount(UserAddress2, "123456");
            var unsubscribeWhitelist = _whiteListContract.UnsubscribeWhitelist
            (
                subscribeId
            );
            unsubscribeWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            // Check event
            var logs = unsubscribeWhitelist.Logs.First(l => l.Name.Equals("WhitelistUnsubscribed")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var whitelistUnsubscribed = WhitelistUnsubscribed.Parser.ParseFrom(byteString);
            whitelistUnsubscribed.WhitelistId.ShouldBe(whitelistId);
            whitelistUnsubscribed.ProjectId.ShouldBe(projectId);
            whitelistUnsubscribed.SubscribeId.ShouldBe(subscribeId);
        }

        [TestMethod]
        public void CloneWhitelist()
        {
            var whitelistId = Hash.LoadFromHex("2e399d3e913557ba3b032f4014f6523bf941e138df7413fcb3b06c628f98c232");
            var creator = UserAddress1.ConvertAddress();
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");

            _whiteListContract.SetAccount(UserAddress1);
            var cloneWhitelist = _whiteListContract.CloneWhitelist
            (
                whitelistId,
                creator,
                new AddressList
                {
                    Value = { ManagersAddress.ConvertAddress(), ManagersAddress1.ConvertAddress() }
                },
                out var output
            );
            Logger.Info($"output is {output}");
            cloneWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var id = HashHelper.ComputeFrom($"{whitelistId}{projectId}{"First"}");
            var whitelist = _whiteListContract.GetWhitelist(output);
            Logger.Info($"whitelist is {whitelist}");
            whitelist.WhitelistId.ShouldBe(output);
            whitelist.ExtraInfoIdList.Value[0].AddressList.Value[0].ShouldBe(UserAddress.ConvertAddress());
            whitelist.ExtraInfoIdList.Value[0].Id.ShouldBe(id);
            whitelist.IsAvailable.ShouldBe(true);
            // Check event
            var logs = cloneWhitelist.Logs.First(l => l.Name.Equals("WhitelistCreated")).NonIndexed;
            var byteString = ByteString.FromBase64(logs);
            var whitelistCreated = WhitelistCreated.Parser.ParseFrom(byteString);
            Logger.Info($"whitelistCloned is {whitelistCreated}");
            whitelistCreated.WhitelistId.ShouldBe(whitelistId);
            whitelistCreated.CloneFrom.ShouldBe(projectId);
        }

        [TestMethod]
        public void CreateWhitelistFail()
        {
            var isCloneable = true;
            var remark = "111111";
            var creator = InitAccount.ConvertAddress();
            var info1 = new PriceTag { Symbol = "ELF", Amount = 10_00000000 }.ToByteString();
            var info2 = new PriceTag { Symbol = "ELF", Amount = 20_00000000 }.ToByteString();
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");

            //whitelistId:a2bee3dbf5f148f6e6675e649673861b36fa0d885c41b755f11e812b3faf8a84


            //例：非message创建
            _whiteListContract.SetAccount(UserAddress);
            var createWhitelist1 = _whiteListContract.CreateWhitelist(
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
                                TagName = "First",
                                Info = info1
                            }
                        },
                        new ExtraInfo
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress1.ConvertAddress() }
                            },
                            Info = new TagInfo
                            {
                                TagName = "TWO",
                                Info = info2
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
                out _
            );
            createWhitelist1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);

            //例：添加message信息重复
            _whiteListContract.SetAccount(InitAccount);
            var createWhitelist2 = _whiteListContract.CreateWhitelist(
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
                                TagName = "First",
                                Info = info1
                            }
                        },
                        new ExtraInfo
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress1.ConvertAddress() }
                            },
                            Info = new TagInfo
                            {
                                TagName = "TWO",
                                Info = info2
                            }
                        }
                    }
                },
                isCloneable,
                remark,
                creator,
                new AddressList
                {
                    Value = { ManagersAddress.ConvertAddress(), ManagersAddress.ConvertAddress() }
                },
                projectId,
                StrategyType.Price,
                out var output
            );
            Logger.Info($"output is {output}");
            createWhitelist2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var addressList = _whiteListContract.GetManagerList(output);
            Logger.Info($"addressList is {addressList}");

            //例：添加message信息重复
            _whiteListContract.SetAccount(InitAccount);
            var createWhitelist3 = _whiteListContract.CreateWhitelist(
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
                                TagName = "First",
                                Info = info1
                            }
                        },
                        new ExtraInfo
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress1.ConvertAddress() }
                            },
                            Info = new TagInfo
                            {
                                TagName = "First",
                                Info = info2
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
                out var output1
            );
            Logger.Info($"output is {output1}");
            createWhitelist3.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var whitelist = _whiteListContract.GetWhitelist(output);
            Logger.Info($"whitelist11 is {whitelist}");
            //例：添加user信息重复
            _whiteListContract.SetAccount(InitAccount);
            var createWhitelist4 = _whiteListContract.CreateWhitelist(
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
                                TagName = "First",
                                Info = info1
                            }
                        },
                        new ExtraInfo
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress.ConvertAddress() }
                            },
                            Info = new TagInfo
                            {
                                TagName = "Two",
                                Info = info2
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
                out _
            );
            createWhitelist4.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            createWhitelist4.Error.ShouldContain("Duplicate address list.");
        }

        [TestMethod]
        public void AddExtraInfoFail()
        {
            var whitelistId = Hash.LoadFromHex("4197a028f863639cedf0ae1b2a2c8f9d87f67cd02c430f3519e7562f496c4c04");
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");
            var projectId1 = HashHelper.ComputeFrom($"{UserAddress4.ConvertAddress()}");
            var info = new Price { Symbol = "ELF", Amount = 30_00000000 }.ToByteString();
            //开启白名单
            _whiteListContract.SetAccount(InitAccount);
            var enableWhitelist = _whiteListContract.EnableWhitelist(whitelistId);
            enableWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //例：重复添加
            _whiteListContract.SetAccount(InitAccount);
            var addExtraInfo = _whiteListContract.AddExtraInfo
            (
                whitelistId,
                projectId,
                new TagInfo
                {
                    TagName = "First",
                    Info = info
                },
                new AddressList
                {
                    Value = { UserAddress2.ConvertAddress() }
                }
            );
            addExtraInfo.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addExtraInfo.Error.ShouldContain("The tag Info First already exists.");
            //例：错误的信息
            Thread.Sleep(60 * 1000);
            _whiteListContract.SetAccount(InitAccount);
            var addExtraInfo1 = _whiteListContract.AddExtraInfo
            (
                whitelistId,
                projectId1,
                new TagInfo
                {
                    TagName = "111",
                    Info = info
                },
                new AddressList
                {
                    Value = { UserAddress2.ConvertAddress() }
                }
            );
            addExtraInfo1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addExtraInfo1.Error.ShouldContain("Incorrect project id");
            //例：TagName下的用户重复
            Thread.Sleep(60 * 1000);
            _whiteListContract.SetAccount(ManagersAddress);
            var addExtraInfo2 = _whiteListContract.AddExtraInfo
            (
                whitelistId,
                projectId,
                new TagInfo
                {
                    TagName = "111",
                    Info = info
                },
                new AddressList
                {
                    Value = { UserAddress.ConvertAddress() }
                }
            );
            addExtraInfo2.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addExtraInfo2.Error.ShouldContain("Address already exists in whitelist.");
            //例：非ManagersAddress添加
            Thread.Sleep(60 * 1000);
            _whiteListContract.SetAccount(UserAddress);
            var addExtraInfo3 = _whiteListContract.AddExtraInfo
            (
                whitelistId,
                projectId,
                new TagInfo
                {
                    TagName = "111",
                    Info = info
                },
                new AddressList
                {
                    Value = { UserAddress2.ConvertAddress() }
                }
            );
            addExtraInfo3.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addExtraInfo3.Error.ShouldContain("is not the manager of the whitelist.");
            //例：白名单关闭后添加
            _whiteListContract.SetAccount(InitAccount);
            var disableWhitelist = _whiteListContract.DisableWhitelist(whitelistId);
            disableWhitelist.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            var addExtraInfo4 = _whiteListContract.AddExtraInfo
            (
                whitelistId,
                projectId,
                new TagInfo
                {
                    TagName = "111",
                    Info = info
                },
                new AddressList
                {
                    Value = { UserAddress2.ConvertAddress() }
                }
            );
            addExtraInfo4.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addExtraInfo4.Error.ShouldContain("Whitelist is not available");
        }

        [TestMethod]
        public void AddAddressInfoListToWhitelistFail()
        {
            var whitelistId = Hash.LoadFromHex("e8cb6378f5c0a58c6ec3465ff1324f9003f11fae5d1f6200413b0931246a499f");
            var whitelistId1 = Hash.LoadFromHex("b340da2947eab4a08718e62328e92f100daaeb4917bcddbc7d45167daa09ac52");
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");
            var id = HashHelper.ComputeFrom($"{whitelistId}{projectId}{"First"}");
            var id1 = HashHelper.ComputeFrom($"{whitelistId}{projectId}{"TWO"}");
            var id2 = HashHelper.ComputeFrom($"{whitelistId}{projectId}{"TWO000000"}");
            Logger.Info($"id is {id}");
            /*
            //开启白名单
            _whiteListContract.SetAccount(InitAccount);
            var enableWhitelist = _whiteListContract.EnableWhitelist(whitelistId);
            enableWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            */

            //添加在标签下白名单的多用户
            _whiteListContract.SetAccount(InitAccount);
            var addAddressInfoListToWhitelist = _whiteListContract.AddAddressInfoListToWhitelist
            (
                whitelistId,
                new ExtraInfoIdList
                {
                    Value =
                    {
                        new ExtraInfoId
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress.ConvertAddress() }
                            },
                            Id = id
                        },
                        new ExtraInfoId
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress3.ConvertAddress() }
                            },
                            Id = id
                        }
                    }
                }
            );
            addAddressInfoListToWhitelist.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addAddressInfoListToWhitelist.Error.ShouldContain("Duplicate address.");

            //添加在其他标签下已存在的白名单的多用户
            _whiteListContract.SetAccount(InitAccount);
            var addAddressInfoListToWhitelist1 = _whiteListContract.AddAddressInfoListToWhitelist
            (
                whitelistId,
                new ExtraInfoIdList
                {
                    Value =
                    {
                        new ExtraInfoId
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress.ConvertAddress() }
                            },
                            Id = id1
                        },
                        new ExtraInfoId
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress3.ConvertAddress() }
                            },
                            Id = id1
                        }
                    }
                }
            );
            addAddressInfoListToWhitelist1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addAddressInfoListToWhitelist1.Error.ShouldContain("Duplicate address.");
            //whitelistId输入错误
            _whiteListContract.SetAccount(InitAccount);
            var addAddressInfoListToWhitelist2 = _whiteListContract.AddAddressInfoListToWhitelist
            (
                whitelistId1,
                new ExtraInfoIdList
                {
                    Value =
                    {
                        new ExtraInfoId
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress2.ConvertAddress() }
                            },
                            Id = id
                        },
                        new ExtraInfoId
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress3.ConvertAddress() }
                            },
                            Id = id
                        }
                    }
                }
            );
            addAddressInfoListToWhitelist2.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addAddressInfoListToWhitelist2.Error.ShouldContain("Whitelist not found.");
            //Id输入错误
            _whiteListContract.SetAccount(InitAccount);
            var addAddressInfoListToWhitelist5 = _whiteListContract.AddAddressInfoListToWhitelist
            (
                whitelistId,
                new ExtraInfoIdList
                {
                    Value =
                    {
                        new ExtraInfoId
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress2.ConvertAddress() }
                            },
                            Id = id2
                        },
                        new ExtraInfoId
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress3.ConvertAddress() }
                            },
                            Id = id2
                        }
                    }
                }
            );
            addAddressInfoListToWhitelist5.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addAddressInfoListToWhitelist5.Error.ShouldContain("Incorrect TagId");
            //非管理员添加白名单
            _whiteListContract.SetAccount(UserAddress);
            var addAddressInfoListToWhitelist3 = _whiteListContract.AddAddressInfoListToWhitelist
            (
                whitelistId,
                new ExtraInfoIdList
                {
                    Value =
                    {
                        new ExtraInfoId
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress2.ConvertAddress() }
                            },
                            Id = id
                        },
                        new ExtraInfoId
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress3.ConvertAddress() }
                            },
                            Id = id
                        }
                    }
                }
            );
            addAddressInfoListToWhitelist3.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addAddressInfoListToWhitelist3.Error.ShouldContain("is not the manager of the whitelist.");
            //关闭白名单，管理员添加白名单
            _whiteListContract.SetAccount(InitAccount);
            var disableWhitelist = _whiteListContract.DisableWhitelist(whitelistId);
            disableWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _whiteListContract.SetAccount(InitAccount);
            var addAddressInfoListToWhitelist4 = _whiteListContract.AddAddressInfoListToWhitelist
            (
                whitelistId,
                new ExtraInfoIdList
                {
                    Value =
                    {
                        new ExtraInfoId
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress2.ConvertAddress() }
                            },
                            Id = id
                        },
                        new ExtraInfoId
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress3.ConvertAddress() }
                            },
                            Id = id
                        }
                    }
                }
            );
            addAddressInfoListToWhitelist4.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addAddressInfoListToWhitelist4.Error.ShouldContain("Whitelist is not available.");
        }

        [TestMethod]
        public void RemoveAddressInfoListFromWhitelistFail()
        {
            var whitelistId = Hash.LoadFromHex("137d5d0c239e623944335532f40f2087ae10d1870269defb7e24b332140d95e1");
            var whitelistId1 = Hash.LoadFromHex("b340da2947eab4a08718e62328e92f100daaeb4917bcddbc7d45167daa09ac52");
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");
            var id = HashHelper.ComputeFrom($"{whitelistId}{projectId}{"First"}");
            var id1 = HashHelper.ComputeFrom($"{whitelistId}{projectId}{"TWO"}");
            var id2 = HashHelper.ComputeFrom($"{whitelistId}{projectId}{"TWO00"}");
            Logger.Info($"id is {id}");


            //开启白名单
            _whiteListContract.SetAccount(InitAccount);
            var enableWhitelist = _whiteListContract.EnableWhitelist(whitelistId);
            enableWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            //例：移除不在白名单的用户
            _whiteListContract.SetAccount(InitAccount);
            var removeAddressInfoListFromWhitelist = _whiteListContract.RemoveAddressInfoListFromWhitelist
            (
                whitelistId,
                new ExtraInfoIdList
                {
                    Value =
                    {
                        new ExtraInfoId
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress2.ConvertAddress() }
                            },
                            Id = id
                        },
                        new ExtraInfoId
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress3.ConvertAddress() }
                            },
                            Id = id
                        }
                    }
                }
            );
            removeAddressInfoListFromWhitelist.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            removeAddressInfoListFromWhitelist.Error.ShouldContain("These extraInfos do not exist.");
            //例：whitelistId输入错误
            _whiteListContract.SetAccount(InitAccount);
            var removeAddressInfoListFromWhitelist1 = _whiteListContract.RemoveAddressInfoListFromWhitelist
            (
                whitelistId1,
                new ExtraInfoIdList
                {
                    Value =
                    {
                        new ExtraInfoId
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress2.ConvertAddress() }
                            },
                            Id = id
                        },
                        new ExtraInfoId
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress3.ConvertAddress() }
                            },
                            Id = id
                        }
                    }
                }
            );
            removeAddressInfoListFromWhitelist1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            removeAddressInfoListFromWhitelist1.Error.ShouldContain("Whitelist not found.");
            //例：Id输入错误
            _whiteListContract.SetAccount(InitAccount);
            var removeAddressInfoListFromWhitelist2 = _whiteListContract.RemoveAddressInfoListFromWhitelist
            (
                whitelistId,
                new ExtraInfoIdList
                {
                    Value =
                    {
                        new ExtraInfoId
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress3.ConvertAddress() }
                            },
                            Id = id2
                        },
                        new ExtraInfoId
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress3.ConvertAddress() }
                            },
                            Id = id2
                        }
                    }
                }
            );
            removeAddressInfoListFromWhitelist2.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            removeAddressInfoListFromWhitelist2.Error.ShouldContain("These extraInfos do not exist.");
            //例：非管理员调用
            _whiteListContract.SetAccount(UserAddress);
            var removeAddressInfoListFromWhitelist3 = _whiteListContract.RemoveAddressInfoListFromWhitelist
            (
                whitelistId,
                new ExtraInfoIdList
                {
                    Value =
                    {
                        new ExtraInfoId
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress.ConvertAddress() }
                            },
                            Id = id
                        },
                        new ExtraInfoId
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress1.ConvertAddress() }
                            },
                            Id = id1
                        }
                    }
                }
            );
            removeAddressInfoListFromWhitelist3.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            removeAddressInfoListFromWhitelist3.Error.ShouldContain("is not the manager of the whitelist.");
            //关闭白名单，管理员添加白名单
            _whiteListContract.SetAccount(InitAccount);
            var disableWhitelist = _whiteListContract.DisableWhitelist(whitelistId);
            disableWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _whiteListContract.SetAccount(UserAddress);
            var removeAddressInfoListFromWhitelist4 = _whiteListContract.RemoveAddressInfoListFromWhitelist
            (
                whitelistId,
                new ExtraInfoIdList
                {
                    Value =
                    {
                        new ExtraInfoId
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress.ConvertAddress() }
                            },
                            Id = id
                        },
                        new ExtraInfoId
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress1.ConvertAddress() }
                            },
                            Id = id1
                        }
                    }
                }
            );
            removeAddressInfoListFromWhitelist4.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            removeAddressInfoListFromWhitelist4.Error.ShouldContain("Whitelist is not available.");
        }

        [TestMethod]
        public void RemoveTagInfoFail()
        {
            var whitelistId = Hash.LoadFromHex("2c6054e42705447749f0702f818bec4cc19f47a484d497de84642284597851c1");
            var whitelistId1 = Hash.LoadFromHex("1995924f281943260a81dc3f73a0e4eea2339ae57f155504cfb8ff79ac8f1892");
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");
            var projectId1 = HashHelper.ComputeFrom($"{UserAddress4.ConvertAddress()}");
            var tagId = HashHelper.ComputeFrom($"{whitelistId}{projectId}{"TWO"}");
            var tagId1 = HashHelper.ComputeFrom($"{whitelistId}{projectId}{"TWO1111"}");

            Logger.Info($"tagId is {tagId}");
            //例：未删除标签信息，删除标签
            _whiteListContract.SetAccount(InitAccount);
            var removeTagInfo = _whiteListContract.RemoveTagInfo
            (
                whitelistId,
                projectId,
                tagId
            );
            removeTagInfo.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            removeTagInfo.Error.ShouldContain("Exist address list");
            //删除标签信息
            _whiteListContract.SetAccount(InitAccount);
            var removeAddressInfoFromWhitelist = _whiteListContract.RemoveAddressInfoListFromWhitelist
            (
                whitelistId,
                new ExtraInfoIdList
                {
                    Value =
                    {
                        new ExtraInfoId
                        {
                            AddressList = new AddressList
                            {
                                Value = { UserAddress1.ConvertAddress() }
                            },
                            Id = tagId
                        }
                    }
                }
            );
            removeAddressInfoFromWhitelist.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            removeAddressInfoFromWhitelist.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            //非管理员删除
            _whiteListContract.SetAccount(UserAddress);
            var removeTagInfo1 = _whiteListContract.RemoveTagInfo
            (
                whitelistId,
                projectId,
                tagId
            );
            removeTagInfo1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            removeTagInfo1.Error.ShouldContain("is not the manager of the whitelist.");
            //传入错误的whitelistId1
            _whiteListContract.SetAccount(InitAccount);
            var removeTagInfo2 = _whiteListContract.RemoveTagInfo
            (
                whitelistId1,
                projectId,
                tagId
            );
            removeTagInfo2.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            removeTagInfo2.Error.ShouldContain("Incorrect whitelist id");
            //例projectId输入错误
            _whiteListContract.SetAccount(InitAccount);
            var removeTagInfo3 = _whiteListContract.RemoveTagInfo
            (
                whitelistId,
                projectId1,
                tagId
            );
            removeTagInfo3.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            removeTagInfo3.Error.ShouldContain("Incorrect project id");
            //例tagId输入错误
            _whiteListContract.SetAccount(InitAccount);
            var removeTagInfo4 = _whiteListContract.RemoveTagInfo
            (
                whitelistId,
                projectId,
                tagId1
            );
            removeTagInfo4.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            removeTagInfo4.Error.ShouldContain("Incorrect tagInfoId");
            //例：关闭白名单，删除标签
            _whiteListContract.SetAccount(InitAccount);
            var disableWhitelist = _whiteListContract.DisableWhitelist(whitelistId);
            disableWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            _whiteListContract.SetAccount(InitAccount);
            var removeTagInfo5 = _whiteListContract.RemoveTagInfo
            (
                whitelistId,
                projectId,
                tagId1
            );
            removeTagInfo5.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            removeTagInfo5.Error.ShouldContain("Whitelist is not available");
            //开启白名单
            _whiteListContract.SetAccount(InitAccount);
            var enableWhitelist = _whiteListContract.EnableWhitelist(whitelistId);
            enableWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //例：重复删除
            var removeTagInfo6 = _whiteListContract.RemoveTagInfo
            (
                whitelistId,
                projectId,
                tagId
            );
            removeTagInfo6.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            Thread.Sleep(60 * 1000);
            var removeTagInfo7 = _whiteListContract.RemoveTagInfo
            (
                whitelistId,
                projectId,
                tagId
            );
            removeTagInfo7.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            removeTagInfo7.Error.ShouldContain("Incorrect tagInfoId");
        }

        [TestMethod]
        public void DisableWhitelistFail()
        {
            var whitelistId = Hash.LoadFromHex("2c6054e42705447749f0702f818bec4cc19f47a484d497de84642284597851c1");
            _whiteListContract.SetAccount(InitAccount);
            var disableWhitelist = _whiteListContract.DisableWhitelist(whitelistId);
            disableWhitelist.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            disableWhitelist.Error.ShouldContain("Whitelist is not available");

            _whiteListContract.SetAccount(UserAddress);
            var enableWhitelist = _whiteListContract.EnableWhitelist(whitelistId);
            enableWhitelist.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            enableWhitelist.Error.ShouldContain("is not the manager of the whitelist.");
        }

        [TestMethod]
        public void EnableWhitelistFail()
        {
            var whitelistId = Hash.LoadFromHex("2c6054e42705447749f0702f818bec4cc19f47a484d497de84642284597851c1");
            _whiteListContract.SetAccount(InitAccount);
            var enableWhitelist = _whiteListContract.EnableWhitelist(whitelistId);
            enableWhitelist.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            enableWhitelist.Error.ShouldContain("The whitelist is already available");

            _whiteListContract.SetAccount(UserAddress);
            var disableWhitelist = _whiteListContract.DisableWhitelist(whitelistId);
            disableWhitelist.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            disableWhitelist.Error.ShouldContain("is not the manager of the whitelist.");
        }


        [TestMethod]
        public void ChangeWhitelistCloneableFail()
        {
            var whitelistId = Hash.LoadFromHex("137d5d0c239e623944335532f40f2087ae10d1870269defb7e24b332140d95e1");
            var isCloneable = false;
            var isCloneable1 = true;

            //开启白名单
            _whiteListContract.SetAccount(InitAccount);
            var enableWhitelist = _whiteListContract.EnableWhitelist(whitelistId);
            enableWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //例：开启状态下开启克隆
            _whiteListContract.SetAccount(InitAccount);
            var changeWhitelistCloneable = _whiteListContract.ChangeWhitelistCloneable
            (
                whitelistId,
                isCloneable
            );
            changeWhitelistCloneable.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            //关闭克隆
            _whiteListContract.SetAccount(InitAccount);
            var changeWhitelistCloneable1 = _whiteListContract.ChangeWhitelistCloneable
            (
                whitelistId,
                isCloneable
            );
            changeWhitelistCloneable1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            //不是管理员开启
            _whiteListContract.SetAccount(UserAddress);
            var changeWhitelistCloneable2 = _whiteListContract.ChangeWhitelistCloneable
            (
                whitelistId,
                isCloneable
            );
            changeWhitelistCloneable2.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            changeWhitelistCloneable2.Error.ShouldContain("is not the manager of the whitelist.");
            //例关闭状态下关闭
            _whiteListContract.SetAccount(InitAccount);
            var changeWhitelistCloneable3 = _whiteListContract.ChangeWhitelistCloneable
            (
                whitelistId,
                isCloneable
            );
            changeWhitelistCloneable3.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            //例：关闭白名单
            _whiteListContract.SetAccount(InitAccount);
            var disableWhitelist = _whiteListContract.DisableWhitelist(whitelistId);
            disableWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //白名单关闭下开起
            _whiteListContract.SetAccount(InitAccount);
            var changeWhitelistCloneable4 = _whiteListContract.ChangeWhitelistCloneable
            (
                whitelistId,
                isCloneable1
            );
            changeWhitelistCloneable4.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            changeWhitelistCloneable4.Error.ShouldContain("Whitelist is not available.");
        }

        [TestMethod]
        public void UpdateExtraInfoFail()
        {
            var whitelistId = Hash.LoadFromHex("4197a028f863639cedf0ae1b2a2c8f9d87f67cd02c430f3519e7562f496c4c04");
            var whitelistId1 = Hash.LoadFromHex("4297a028f863639cedf0ae1b2a2c8f9d87f67cd02c430f3519e7562f496c4c04");
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");
            var owner = InitAccount.ConvertAddress();
            var tagId = HashHelper.ComputeFrom($"{whitelistId}{projectId}{"First"}");
            var tagId1 = HashHelper.ComputeFrom($"{whitelistId}{projectId}{"TWO"}");
            var tagId2 = HashHelper.ComputeFrom($"{whitelistId}{projectId}{"TWOoooo"}");
            /*
            //开启白名单
            _whiteListContract.SetAccount(InitAccount);
            var enableWhitelist = _whiteListContract.EnableWhitelist(whitelistId);
            enableWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            */

            //例：非管理员调用
            _whiteListContract.SetAccount(UserAddress);
            var updateExtraInfo = _whiteListContract.UpdateExtraInfo
            (
                whitelistId,
                new ExtraInfoId
                {
                    AddressList = new AddressList
                    {
                        Value = { UserAddress2.ConvertAddress() }
                    },
                    Id = tagId1
                }
            );
            updateExtraInfo.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            updateExtraInfo.Error.ShouldContain("is not the manager of the whitelist.");
            //例：理员调用，whitelistId输入错误
            _whiteListContract.SetAccount(InitAccount);
            var updateExtraInfo1 = _whiteListContract.UpdateExtraInfo
            (
                whitelistId1,
                new ExtraInfoId
                {
                    AddressList = new AddressList
                    {
                        Value = { UserAddress2.ConvertAddress() }
                    },
                    Id = tagId1
                }
            );
            updateExtraInfo1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            updateExtraInfo1.Error.ShouldContain("Whitelist not found.");
            //例：理员调用，Id输入错误
            _whiteListContract.SetAccount(InitAccount);
            var updateExtraInfo2 = _whiteListContract.UpdateExtraInfo
            (
                whitelistId,
                new ExtraInfoId
                {
                    AddressList = new AddressList
                    {
                        Value = { UserAddress2.ConvertAddress() }
                    },
                    Id = tagId2
                }
            );
            updateExtraInfo2.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            updateExtraInfo2.Error.ShouldContain("Incorrect extraInfoId.");
            //例：理员调用，address输入错误
            _whiteListContract.SetAccount(InitAccount);
            var updateExtraInfo3 = _whiteListContract.UpdateExtraInfo
            (
                whitelistId,
                new ExtraInfoId
                {
                    AddressList = new AddressList
                    {
                        Value = { UserAddress2.ConvertAddress() }
                    },
                    Id = tagId2
                }
            );
            updateExtraInfo3.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            updateExtraInfo3.Error.ShouldContain("Incorrect extraInfoId.");
            //例：无更换，进行设置
            _whiteListContract.SetAccount(InitAccount);
            var updateExtraInfo4 = _whiteListContract.UpdateExtraInfo
            (
                whitelistId,
                new ExtraInfoId
                {
                    AddressList = new AddressList
                    {
                        Value = { UserAddress.ConvertAddress() }
                    },
                    Id = tagId
                }
            );
            updateExtraInfo4.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
            //关闭白名单
            _whiteListContract.SetAccount(InitAccount);
            var disableWhitelist = _whiteListContract.DisableWhitelist(whitelistId);
            disableWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //例：关闭后进行设置
            _whiteListContract.SetAccount(InitAccount);
            var updateExtraInfo5 = _whiteListContract.UpdateExtraInfo
            (
                whitelistId,
                new ExtraInfoId
                {
                    AddressList = new AddressList
                    {
                        Value = { UserAddress2.ConvertAddress() }
                    },
                    Id = tagId1
                }
            );
            updateExtraInfo5.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            updateExtraInfo5.Error.ShouldContain("Whitelist is not available.");
        }

        [TestMethod]
        public void TransferManagerFail()
        {
            var whitelistId = Hash.LoadFromHex("4197a028f863639cedf0ae1b2a2c8f9d87f67cd02c430f3519e7562f496c4c04");
            var userAddress = UserAddress1.ConvertAddress();
            var managerAddress = ManagersAddress.ConvertAddress();
            //开启白名单
            _whiteListContract.SetAccount(InitAccount);
            var enableWhitelist = _whiteListContract.EnableWhitelist(whitelistId);
            enableWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //例：非管理者转让权限
            _whiteListContract.SetAccount(UserAddress);
            var transferManager = _whiteListContract.TransferManager
            (
                whitelistId,
                userAddress
            );
            transferManager.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            transferManager.Error.ShouldContain("is not the manager of the whitelist.");
            //例:自己给自己转移
            _whiteListContract.SetAccount(ManagersAddress);
            var transferManager1 = _whiteListContract.TransferManager
            (
                whitelistId,
                managerAddress
            );
            transferManager1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            transferManager1.Error.ShouldContain("Manager already exists.");

            //关闭白名单
            _whiteListContract.SetAccount(InitAccount);
            var disableWhitelist = _whiteListContract.DisableWhitelist(whitelistId);
            disableWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //白名单关闭状态下转移
            _whiteListContract.SetAccount(ManagersAddress);
            var transferManager2 = _whiteListContract.TransferManager
            (
                whitelistId,
                userAddress
            );
            transferManager2.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            transferManager2.Error.ShouldContain("Whitelist is not available.");
        }

        [TestMethod]
        public void AddManagersFail()
        {
            var whitelistId = Hash.LoadFromHex("5bc1323c177178e82567af7811c90d52f5af14d3822e1a90efe4a90fa361f3c2");
            //开启白名单
            _whiteListContract.SetAccount(InitAccount);
            var enableWhitelist = _whiteListContract.EnableWhitelist(whitelistId);
            enableWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //例：添加重复的管理者
            _whiteListContract.SetAccount(InitAccount);
            var addManagers = _whiteListContract.AddManagers
            (
                whitelistId,
                new AddressList
                {
                    Value = { ManagersAddress.ConvertAddress(), ManagersAddress3.ConvertAddress() }
                }
            );
            addManagers.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addManagers.Error.ShouldContain("Managers already exists.");
            //例：不是管理者去添加管理者
            _whiteListContract.SetAccount(UserAddress);
            var addManagers1 = _whiteListContract.AddManagers
            (
                whitelistId,
                new AddressList
                {
                    Value = { ManagersAddress2.ConvertAddress(), ManagersAddress3.ConvertAddress() }
                }
            );
            addManagers1.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            addManagers1.Error.ShouldContain("No permission.");
            //关闭白名单
            _whiteListContract.SetAccount(InitAccount);
            var disableWhitelist = _whiteListContract.DisableWhitelist(whitelistId);
            disableWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //例：关闭状态下添加的管理者
            _whiteListContract.SetAccount(InitAccount);
            var addManagers2 = _whiteListContract.AddManagers
            (
                whitelistId,
                new AddressList
                {
                    Value = { ManagersAddress2.ConvertAddress(), ManagersAddress3.ConvertAddress() }
                }
            );
            addManagers2.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public void RemoveManagersFail()
        {
            var whitelistId = Hash.LoadFromHex("93eaac5109d61d982a936c4bda7f912461cccbedd0579f785ae476ea9e695693");

            //移除不在白名单的用户
            _whiteListContract.SetAccount(InitAccount);
            var removeManagers = _whiteListContract.RemoveManagers
            (
                whitelistId,
                new AddressList
                {
                    Value = { ManagersAddress2.ConvertAddress(), ManagersAddress3.ConvertAddress() }
                }
            );
            removeManagers.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            removeManagers.Error.ShouldContain("Managers doesn't exists.");

            //移除不在白名单的用户
            _whiteListContract.SetAccount(UserAddress);
            var removeManagers1 = _whiteListContract.RemoveManagers
            (
                whitelistId,
                new AddressList
                {
                    Value = { ManagersAddress2.ConvertAddress(), ManagersAddress3.ConvertAddress() }
                }
            );
            removeManagers1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            removeManagers1.Error.ShouldContain("No permission.");
        }

        [TestMethod]
        public void ResetWhitelistFail()
        {
            //ea663c76b3484a4c0622ea0b01f6bb50a842e3dfb795d39a2ad6351984f501ef_已删除
            var whitelistId = Hash.LoadFromHex("08901ebf2812af9e4f899212023bf44dab9a96487d4eb558edf7f36bea12617d");
            var whitelistId1 = Hash.LoadFromHex("4f7b6d5c3076f02dd4dc75042b90b367d3f34aa976bb5310c64b0a68b2946ee4");
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");
            var projectId1 = HashHelper.ComputeFrom($"{UserAddress4.ConvertAddress()}");
            /*//开启白名单
            _whiteListContract.SetAccount(InitAccount);
            var enableWhitelist = _whiteListContract.EnableWhitelist(whitelistId);
            enableWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            */

            //例：非管理者调用
            _whiteListContract.SetAccount(UserAddress);
            var resetWhitelist = _whiteListContract.ResetWhitelist
            (
                whitelistId,
                projectId
            );
            resetWhitelist.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            resetWhitelist.Error.ShouldContain("is not the manager of the whitelist.");
            //例：whitelistId输入错误
            _whiteListContract.SetAccount(InitAccount);
            var resetWhitelist1 = _whiteListContract.ResetWhitelist
            (
                whitelistId1,
                projectId
            );
            resetWhitelist1.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            resetWhitelist1.Error.ShouldContain("Whitelist not found.");
            //例：projectId输入错误
            _whiteListContract.SetAccount(ManagersAddress);
            var resetWhitelist2 = _whiteListContract.ResetWhitelist
            (
                whitelistId,
                projectId1
            );
            resetWhitelist2.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            resetWhitelist2.Error.ShouldContain("Incorrect projectId.");
            //关闭白名单
            _whiteListContract.SetAccount(InitAccount);
            var disableWhitelist = _whiteListContract.DisableWhitelist(whitelistId);
            disableWhitelist.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            //例：projectId输入错误
            _whiteListContract.SetAccount(InitAccount);
            var resetWhitelist3 = _whiteListContract.ResetWhitelist
            (
                whitelistId,
                projectId1
            );
            resetWhitelist3.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.NodeValidationFailed);
            resetWhitelist3.Error.ShouldContain("Whitelist is not available.");
        }

        /*
        [TestMethod]
        public void Z()
        {
            var whitelistId = Hash.LoadFromHex("b7ee3fa3db5f6179323b218500f05f607240a79e00e257db8438be36f0d8f87e");
            var projectId = HashHelper.ComputeFrom($"{UserAddress.ConvertAddress()}");
            var list = new ExtraInfoIdList();

            // var list =  new List<ExtraInfoId>();
            for (int i = 0; i < 10; i++)
            {
                var address = NodeManager.NewAccount("123456");
                var id = HashHelper.ComputeFrom($"{InitAccount.ConvertAddress()}{projectId}{"First"}");

                Logger.Info($"\naddress:{address.ConvertAddress()}" +
                            $"\nid:{id}");
                //list.Value[i] = new ExtraInfoId();
                list.Value.Add(new ExtraInfoId());
                list.Value[i].AddressList = address.ConvertAddress();
                list.Value[i].Id = id;
            }

            _whiteListContract.SetAccount(InitAccount);
            var addAddressInfoListToWhitelist = _whiteListContract.AddAddressInfoListToWhitelist
            (
                whitelistId,
                list
            );
            addAddressInfoListToWhitelist.Status.ConvertTransactionResultStatus()
                .ShouldBe(TransactionResultStatus.Mined);
        }
        */
    }
}