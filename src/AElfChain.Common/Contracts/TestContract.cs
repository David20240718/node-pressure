using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts;

public enum TestSchrodingerMethod
{
    
}

public class TestContract : BaseContract<TestSchrodingerMethod>
{
    public TestContract(INodeManager nodeManager, string callAddress, string electionAddress)
        : base(nodeManager, electionAddress)
    {
        SetAccount(callAddress);
    }

    public TestContract(INodeManager nodeManager, string callAddress)
        : base(nodeManager, "Schrodinger.Contracts.TestContract", callAddress)
    {
    }
}