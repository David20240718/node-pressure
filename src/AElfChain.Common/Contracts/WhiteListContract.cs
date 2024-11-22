using AElf;
using AElf.Client.Dto;
using Forest.Whitelist;
using AElf.Types;
using AElfChain.Common.Managers;
using Forest.Whitelist;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;


namespace AElfChain.Common.Contracts
{
    public enum WhiteListContractMethod
    {
        //action
        //Managers
        Initialize,
        CreateWhitelist,
        AddExtraInfo,
        AddAddressInfoListToWhitelist,
        RemoveAddressInfoListFromWhitelist,
        RemoveTagInfo,
        DisableWhitelist,
        EnableWhitelist,
        ChangeWhitelistCloneable,
        UpdateExtraInfo,
        TransferManager,
        AddManagers,
        RemoveManagers,
        ResetWhitelist,

        //Subscribers
        SubscribeWhitelist,
        UnsubscribeWhitelist,
        ConsumeWhitelist,
        CloneWhitelist,

        //view
        GetWhitelistByManager,
        GetWhitelist,
        GetWhitelistDetail,
        GetWhitelistByProject,
        GetExtraInfoByTag,
        GetTagInfoByHash,
        GetExtraInfoIdList,
        GetExtraInfoByAddress,
        GetManagerList,
        GetSubscribeWhitelist,
        GetConsumedList,
        GetAvailableWhitelist,
        GetFromAvailableWhitelist,
        GetExtraInfoFromWhitelist,
        GetTagInfoFromWhitelist
    }

    public class WhiteListContract : BaseContract<WhiteListContractMethod>
    {
        public WhiteListContract(INodeManager nodeManager, string callAddress) :
            base(nodeManager, "Forest.Whitelist", callAddress)
        {
        }

        public WhiteListContract(INodeManager nodeManager, string callAddress, string contractAddress) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }


        //set
        public TransactionResultDto CreateWhitelist(ExtraInfoList extraInfoList, bool isCloneable, string remark,
            Address creator, AddressList addressList, Hash projectId, StrategyType strategyType, out Hash hash)
        {
            var result = ExecuteMethodWithResult(WhiteListContractMethod.CreateWhitelist, new CreateWhitelistInput
            {
                ExtraInfoList = extraInfoList,
                IsCloneable = isCloneable,
                Remark = remark,
                Creator = creator,
                ManagerList = addressList,
                ProjectId = projectId,
                StrategyType = strategyType
            });
            hash = Hash.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            return result;
        }

        public TransactionResultDto AddExtraInfo(Hash whitelistId, Hash projectId, TagInfo tagInfo,
            AddressList addressList)
        {
            var result = ExecuteMethodWithResult(WhiteListContractMethod.AddExtraInfo, new AddExtraInfoInput
            {
                WhitelistId = whitelistId,
                ProjectId = projectId,
                TagInfo = tagInfo,
                AddressList = addressList
            });
            return result;
        }

        public TransactionResultDto AddAddressInfoListToWhitelist(Hash whitelistId, ExtraInfoIdList extraInfoIdList)
        {
            var result = ExecuteMethodWithResult(WhiteListContractMethod.AddAddressInfoListToWhitelist,
                new AddAddressInfoListToWhitelistInput
                {
                    WhitelistId = whitelistId,
                    ExtraInfoIdList = extraInfoIdList
                });
            return result;
        }

        public TransactionResultDto RemoveAddressInfoListFromWhitelist(Hash whitelistId,
            ExtraInfoIdList extraInfoIdList)
        {
            var result = ExecuteMethodWithResult(WhiteListContractMethod.RemoveAddressInfoListFromWhitelist,
                new RemoveAddressInfoListFromWhitelistInput
                {
                    WhitelistId = whitelistId,
                    ExtraInfoIdList = extraInfoIdList
                });
            return result;
        }

        public TransactionResultDto RemoveTagInfo(Hash whitelistId, Hash projectId, Hash tagId)
        {
            var result = ExecuteMethodWithResult(WhiteListContractMethod.RemoveTagInfo, new RemoveTagInfoInput
            {
                WhitelistId = whitelistId,
                ProjectId = projectId,
                TagId = tagId
            });
            return result;
        }

        public TransactionResultDto DisableWhitelist(Hash whitelistId)
        {
            return ExecuteMethodWithResult(WhiteListContractMethod.DisableWhitelist, whitelistId);
        }

        public TransactionResultDto EnableWhitelist(Hash whitelistId)
        {
            return ExecuteMethodWithResult(WhiteListContractMethod.EnableWhitelist, whitelistId);
        }

        public TransactionResultDto ChangeWhitelistCloneable(Hash whitelistId, bool isCloneable)
        {
            var result = ExecuteMethodWithResult(WhiteListContractMethod.ChangeWhitelistCloneable,
                new ChangeWhitelistCloneableInput
                {
                    WhitelistId = whitelistId,
                    IsCloneable = isCloneable
                });
            return result;
        }


        public TransactionResultDto UpdateExtraInfo(Hash whitelistId, ExtraInfoId extraInfoId)
        {
            var result = ExecuteMethodWithResult(WhiteListContractMethod.UpdateExtraInfo, new UpdateExtraInfoInput
            {
                WhitelistId = whitelistId,
                ExtraInfoList = extraInfoId
            });
            return result;
        }

        public TransactionResultDto TransferManager(Hash whitelistId, Address manager)
        {
            var result = ExecuteMethodWithResult(WhiteListContractMethod.TransferManager, new TransferManagerInput
            {
                WhitelistId = whitelistId,
                Manager = manager
            });
            return result;
        }

        public TransactionResultDto AddManagers(Hash whitelistId, AddressList addressList)
        {
            var result = ExecuteMethodWithResult(WhiteListContractMethod.AddManagers, new AddManagersInput
            {
                WhitelistId = whitelistId,
                ManagerList = addressList
            });
            return result;
        }

        public TransactionResultDto RemoveManagers(Hash whitelistId, AddressList addressList)
        {
            var result = ExecuteMethodWithResult(WhiteListContractMethod.RemoveManagers, new RemoveManagersInput
            {
                WhitelistId = whitelistId,
                ManagerList = addressList
            });
            return result;
        }

        public TransactionResultDto ResetWhitelist(Hash whitelistId, Hash projectId)
        {
            var result = ExecuteMethodWithResult(WhiteListContractMethod.ResetWhitelist, new ResetWhitelistInput
            {
                WhitelistId = whitelistId,
                ProjectId = projectId
            });
            return result;
        }

        public TransactionResultDto SubscribeWhitelist(Hash projectId, Hash whitelistId, out Hash hash)
        {
            var result = ExecuteMethodWithResult(WhiteListContractMethod.SubscribeWhitelist, new SubscribeWhitelistInput
            {
                ProjectId = projectId,
                WhitelistId = whitelistId
            });
            hash = Hash.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            return result;
        }

        public TransactionResultDto UnsubscribeWhitelist(Hash subscribeId)
        {
            var result = ExecuteMethodWithResult(WhiteListContractMethod.UnsubscribeWhitelist, subscribeId);
            return result;
        }

        public TransactionResultDto ConsumeWhitelist(Hash subscribeId, Hash whitelistId, ExtraInfoId extraInfoId)
        {
            var result = ExecuteMethodWithResult(WhiteListContractMethod.ConsumeWhitelist, new ConsumeWhitelistInput
            {
                SubscribeId = subscribeId,
                WhitelistId = whitelistId,
                ExtraInfoId = extraInfoId
            });
            return result;
        }

        public TransactionResultDto CloneWhitelist(Hash whitelistId, Address creator, AddressList addressList,
            out Hash hash)
        {
            var result = ExecuteMethodWithResult(WhiteListContractMethod.CloneWhitelist, new CloneWhitelistInput
            {
                WhitelistId = whitelistId,
                Creator = creator,
                ManagerList = addressList
            });
            hash = Hash.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            return result;
        }

        //Views.
        public WhitelistIdList GetWhitelistByManager(Address address)
        {
            return CallViewMethod<WhitelistIdList>(WhiteListContractMethod.GetWhitelistByManager, new Address
            {
                Value = address.Value
            });
        }

        public WhitelistInfo GetWhitelist(Hash hash)
        {
            return CallViewMethod<WhitelistInfo>(WhiteListContractMethod.GetWhitelist, new Hash
            {
                Value = hash.Value
            });
        }

        public ExtraInfoList GetWhitelistDetail(Hash hash)
        {
            return CallViewMethod<ExtraInfoList>(WhiteListContractMethod.GetWhitelistDetail, new Hash
            {
                Value = hash.Value
            });
        }

        public WhitelistIdList GetWhitelistByProject(Hash hash)
        {
            return CallViewMethod<WhitelistIdList>(WhiteListContractMethod.GetWhitelistByProject, new Hash
            {
                Value = hash.Value
            });
        }

        public ExtraInfoList GetExtraInfoByTag(Hash whitelistId, Hash tagInfoId)
        {
            return CallViewMethod<ExtraInfoList>(WhiteListContractMethod.GetExtraInfoByTag, new GetExtraInfoByTagInput
            {
                WhitelistId = whitelistId,
                TagInfoId = tagInfoId
            });
        }

        public TagInfo GetTagInfoByHash(Hash hash)
        {
            return CallViewMethod<TagInfo>(WhiteListContractMethod.GetTagInfoByHash, new Hash
            {
                Value = hash.Value
            });
        }

        public HashList GetExtraInfoIdList(Hash whitelistId, Hash projectId)
        {
            return CallViewMethod<HashList>(WhiteListContractMethod.GetExtraInfoIdList, new GetExtraInfoIdListInput
            {
                WhitelistId = whitelistId,
                ProjectId = projectId
            });
        }


        public TagInfo GetExtraInfoByAddress(Hash whitelistId, Address address)
        {
            return CallViewMethod<TagInfo>(WhiteListContractMethod.GetExtraInfoByAddress, new GetExtraInfoByAddressInput
            {
                WhitelistId = whitelistId,
                Address = address
            });
        }

        public AddressList GetManagerList(Hash hash)
        {
            return CallViewMethod<AddressList>(WhiteListContractMethod.GetManagerList, new Hash
            {
                Value = hash.Value
            });
        }


        public SubscribeWhitelistInfo GetSubscribeWhitelist(Hash hash)
        {
            return CallViewMethod<SubscribeWhitelistInfo>(WhiteListContractMethod.GetSubscribeWhitelist, new Hash
            {
                Value = hash.Value
            });
        }

        public ConsumedList GetConsumedList(Hash hash)
        {
            return CallViewMethod<ConsumedList>(WhiteListContractMethod.GetConsumedList, new Hash
            {
                Value = hash.Value
            });
        }

        public ExtraInfoList GetAvailableWhitelist(Hash hash)
        {
            return CallViewMethod<ExtraInfoList>(WhiteListContractMethod.GetAvailableWhitelist, new Hash
            {
                Value = hash.Value
            });
        }

        public BoolValue GetFromAvailableWhitelist(Hash subscribeId, ExtraInfoId extraInfoId)
        {
            return CallViewMethod<BoolValue>(WhiteListContractMethod.GetFromAvailableWhitelist,
                new GetFromAvailableWhitelistInput
                {
                    SubscribeId = subscribeId,
                    ExtraInfoId = extraInfoId
                });
        }

        public BoolValue GetExtraInfoFromWhitelist(Hash whitelistId, ExtraInfoId extraInfoId)
        {
            return CallViewMethod<BoolValue>(WhiteListContractMethod.GetExtraInfoFromWhitelist,
                new GetExtraInfoFromWhitelistInput
                {
                    WhitelistId = whitelistId,
                    ExtraInfoId = extraInfoId
                });
        }

        public BoolValue GetTagInfoFromWhitelist(Hash whitelistId, Hash projectId, TagInfo tagInfo)
        {
            return CallViewMethod<BoolValue>(WhiteListContractMethod.GetTagInfoFromWhitelist,
                new GetTagInfoFromWhitelistInput
                {
                    WhitelistId = whitelistId,
                    ProjectId = projectId,
                    TagInfo = tagInfo
                });
        }
    }
}