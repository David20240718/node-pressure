using AElf.Types;
using AElfChain.Common.Managers;
using AElfTest.Contract;

namespace AElfChain.Common.Contracts;

public enum TestCryptoBoxContractMethod
{
    Initialize,
    TestCreate,
    TestTransfer,
    TransferWithoutParallel,
    GetTestBalance,
    GetTestTokenInfo,
    SetLongKeyValue,
    GetLongKey
}

public class CryptoBoxContract : BaseContract<TestMethod>
{
    public CryptoBoxContract(INodeManager nodeManager, string callAddress, string contractAddress ) :
        base(nodeManager, contractAddress)
    {
        SetAccount(callAddress);
    }

    public CryptoBoxContract(INodeManager nodeManager, string callAddress, string salt = "", bool isApprove = true)
        : base(nodeManager, ContractFileName, callAddress, salt, isApprove)
    {
    }

    public TestTokenInfo GetTestTokenInfo(string symbol)
    {
        return CallViewMethod<TestTokenInfo>(TestMethod.GetTestTokenInfo,
            new GetTestTokenInfoInput { Symbol = symbol });
    }
    
    public TestBalance GetTestBalance(string symbol, string owner)
    {
        return CallViewMethod<TestBalance>(TestMethod.GetTestBalance,
            new GetTestBalanceInput
            {
                Owner = Address.FromBase58(owner),
                Symbol = symbol
            });
    }

    public static string ContractFileName => "AElfTest.Contract";
    public static string Salt => "AElfTest.Contract";
}