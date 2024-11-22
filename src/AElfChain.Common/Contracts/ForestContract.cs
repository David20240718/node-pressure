using AElf.Client.Dto;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;
using Forest;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Contracts
{
    public enum ForestContractMethod
    {
        Initialize,

        // For Sellers
        ListWithFixedPrice,
        Deal,
        Delist,

        // For Buyers
        MakeOffer,
        CancelOffer,

        // For Admin
        SetServiceFee,
        SetTokenWhiteList,
        SetGlobalTokenWhiteList,
        SetWhitelistContract,
        SetAdministrator,
        SetBizConfig,

        //View
        GetListedNFTInfoList,
        GetWhiteListAddressPriceList,
        GetOfferAddressList,
        GetOfferList,
        GetTokenWhiteList,
        GetGlobalTokenWhiteList,
        GetServiceFeeInfo,
        GetAdministrator,
        GetBizConfig
    }

    public class ForestContract : BaseContract<ForestContractMethod>
    {
        public ForestContract(INodeManager nm, string account) :
            base(nm, ContractFileName, account)
        {
        }

        public ForestContract(INodeManager nm, string callAddress, string contractAbi) :
            base(nm, contractAbi)
        {
            SetAccount(callAddress);
        }

        public static string ContractFileName => "Forest";

        public TransactionResultDto Initialize(string adminAdress, int SetServiceFeeRate,
            string serviceFeeReceiver, long serviceFee,string whiteListContract)
        {
            return ExecuteMethodWithResult(ForestContractMethod.Initialize, new InitializeInput
            {
                AdminAddress = adminAdress.ConvertAddress(),
                ServiceFeeRate = SetServiceFeeRate,
                ServiceFeeReceiver = serviceFeeReceiver.ConvertAddress(),
                ServiceFee = serviceFee,
                WhitelistContractAddress = whiteListContract.ConvertAddress()
            });
        }

        public TransactionResultDto ListWithFixedPrice(string symbol, Price price, long quantity,
            ListDuration duration, WhitelistInfoList whitelistInfoList,
            bool isMergeToPreviousListedInfo)
        {
            return ExecuteMethodWithResult(ForestContractMethod.ListWithFixedPrice, new ListWithFixedPriceInput
            {
                Symbol = symbol,
                Price = price,
                Quantity = quantity,
                Duration = duration,
                Whitelists = whitelistInfoList
            });
        }


        public TransactionResultDto Deal(string symbol, string offerFrom, Price price, long quantity)
        {
            return ExecuteMethodWithResult(ForestContractMethod.Deal, new DealInput
            {
                Symbol = symbol,
                OfferFrom = offerFrom.ConvertAddress(),
                Price = price,
                Quantity = quantity
            });
        }

        public TransactionResultDto Delist(string symbol, Price price, long quantity)
        {
            return ExecuteMethodWithResult(ForestContractMethod.Delist, new DelistInput
            {
                Symbol = symbol,
                Price = price,
                Quantity = quantity
            });
        }

        public TransactionResultDto MakeOffer(string symbol, string offerTo, long quantity, Price price,
            Timestamp expireTime)
        {
            return ExecuteMethodWithResult(ForestContractMethod.MakeOffer, new MakeOfferInput
            {
                Symbol = symbol,
                OfferTo = offerTo.ConvertAddress(),
                Quantity = quantity,
                Price = price,
                ExpireTime = expireTime,
            });
        }

        public TransactionResultDto CancelOffer(string symbol, long tokenId, Int32List indexList, string offerFrom,
            bool isCancelBid)
        {
            return ExecuteMethodWithResult(ForestContractMethod.CancelOffer, new CancelOfferInput
            {
                Symbol = symbol,
                IndexList = indexList,
                OfferFrom = offerFrom.ConvertAddress(),
            });
        }

        public TransactionResultDto SetServiceFee(int serviceFeeRate, string serviceFeeReceiver)
        {
            return ExecuteMethodWithResult(ForestContractMethod.SetServiceFee, new SetServiceFeeInput
            {
                ServiceFeeRate = serviceFeeRate,
                ServiceFeeReceiver = serviceFeeReceiver.ConvertAddress()
            });
        }

        public TransactionResultDto SetGlobalTokenWhiteList(StringList globalTokenWhiteList)
        {
            return ExecuteMethodWithResult(ForestContractMethod.SetGlobalTokenWhiteList, globalTokenWhiteList);
        }
        
        public TransactionResultDto SetWhitelistContract(string whiteListContract)
        {
            return ExecuteMethodWithResult(ForestContractMethod.SetWhitelistContract, whiteListContract.ConvertAddress());
        }
        
        public TransactionResultDto SetAdministrator(string newAdmin)
        {
            return ExecuteMethodWithResult(ForestContractMethod.SetAdministrator, newAdmin.ConvertAddress());
        }
        
        public TransactionResultDto SetAdministratorNull()
        {
            return ExecuteMethodWithResult(ForestContractMethod.SetAdministrator, null);
        }
        
        public TransactionResultDto SetAdministratorEmpty()
        {
            return ExecuteMethodWithResult(ForestContractMethod.SetAdministrator, new Empty());
        }

        public ListedNFTInfoList GetListedNFTInfoList(string symbol, string owner)
        {
            return CallViewMethod<ListedNFTInfoList>(ForestContractMethod.GetListedNFTInfoList,
                new GetListedNFTInfoListInput
                {
                    Symbol = symbol,
                    Owner = owner.ConvertAddress()
                });
        }

        public AddressList GetOfferAddressList(string symbol, long tokenId)
        {
            return CallViewMethod<AddressList>(ForestContractMethod.GetOfferAddressList,
                new GetAddressListInput
                {
                    Symbol = symbol
                });
        }

        public OfferList GetOfferList(string symbol, string address)
        {
            return CallViewMethod<OfferList>(ForestContractMethod.GetOfferList, new GetOfferListInput
            {
                Symbol = symbol,
                Address = address.ConvertAddress()
            });
        }


        public StringList GetTokenWhiteList(string symbol)
        {
            return CallViewMethod<StringList>(ForestContractMethod.GetTokenWhiteList,
                new StringValue { Value = symbol });
        }

        public StringList GetGlobalTokenWhiteList()
        {
            return CallViewMethod<StringList>(ForestContractMethod.GetGlobalTokenWhiteList, new Empty());
        }

        public ServiceFeeInfo GetServiceFeeInfo()
        {
            return CallViewMethod<ServiceFeeInfo>(ForestContractMethod.GetServiceFeeInfo, new Empty());
        }
        
        public Address GetAdministrator()
        {
            return CallViewMethod<Address>(ForestContractMethod.GetAdministrator, new Empty());
        }
        
        public BizConfig GetBizConfig()
        {
            return CallViewMethod<BizConfig>(ForestContractMethod.GetBizConfig, new Empty());
        }
    }
}