using AElf;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestContract.VirtualTransactionEvent;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using log4net;
using Shouldly;

namespace SystemContractTest;

[TestClass]
public class VirtualLogEventTest
{
    private ILog Logger { get; set; }
    private INodeManager NodeManager { get; set; }
    private AuthorityManager AuthorityManager { get; set; }

    private GenesisContract _genesisContract;
    private TokenContract _tokenContract;
    private AssociationContract _associationContract;
    private string _virtualAddress  = "2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS";
    private VirtualTransactionEventTestContract _virtualTransactionEventTestContract;
    
    private string InitAccount { get; } = "2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd";
    private static string RpcUrl { get; } = "http://127.0.0.1:8000";

    [TestInitialize]
    public void Initialize()
    {
        Log4NetHelper.LogInit("VirtualLogEventTest");
        Logger = Log4NetHelper.GetLogger();
        NodeInfoHelper.SetConfig("nodes");

        NodeManager = new NodeManager(RpcUrl);
        AuthorityManager = new AuthorityManager(NodeManager, InitAccount);

        _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
        _tokenContract = _genesisContract.GetTokenContract(InitAccount);
        _associationContract = _genesisContract.GetAssociationAuthContract(InitAccount);
        _virtualTransactionEventTestContract = _virtualAddress == ""
            ? new VirtualTransactionEventTestContract(NodeManager, InitAccount)
            : new VirtualTransactionEventTestContract(NodeManager, InitAccount, _virtualAddress);
    }
    
    [TestMethod]
    public void FireVirtualTransactionEventTest()
    {
        var toAddress = NodeManager.ListAccounts().ToList().First();
        var amount = 1_00000000;
        var virtualHash1 = HashHelper.ComputeFrom("test");
        var virtualHash2 = HashHelper.ComputeFrom("virtual");
        var virtualAddress1 = ConvertVirtualAddressToContractAddress(virtualHash1, _virtualTransactionEventTestContract.Contract);
        var virtualAddress2 = ConvertVirtualAddressToContractAddress(virtualHash2, _virtualTransactionEventTestContract.Contract);
        _tokenContract.TransferBalance(InitAccount, virtualAddress1.ToBase58(), amount, "ELF");
        _tokenContract.TransferBalance(InitAccount, virtualAddress2.ToBase58(), amount, "ELF");

        var balance = _tokenContract.GetUserBalance(toAddress, "ELF");
        var balance1 = _tokenContract.GetUserBalance(virtualAddress1.ToBase58(), "ELF");
        var balance2 = _tokenContract.GetUserBalance(virtualAddress2.ToBase58(), "ELF");
        Logger.Info($"{balance}\n{balance1}\n{balance2}");

        var args = new TransferInput
        {
            To = Address.FromBase58(toAddress),
            Symbol = "ELF",
            Amount = amount,
            Memo = "Test"
        }.ToByteString();
        
        var result = _virtualTransactionEventTestContract.ExecuteMethodWithResult(
            EventTestContractMethod.FireVirtualTransactionEventTest, new FireVirtualTransactionEventTestInput
            {
                To = _tokenContract.Contract,
                Args = args,
                MethodName = nameof(TokenMethod.Transfer)
            });
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        var logs = result.Logs.Where(l => l.Name.Equals("VirtualTransactionCreated")).ToList();
        logs.Count.ShouldBe(1);
        var virtualLog = logs.First();
        var indexed = virtualLog.Indexed.ToList();
        var nonIndexed =  virtualLog.NonIndexed;
        VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(indexed[0])).VirtualHash.ShouldBe(virtualHash1);
        VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(indexed[1])).From.ShouldBe(virtualAddress1);
        VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(indexed[2])).To.ShouldBe(_tokenContract.Contract);
        VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(indexed[3])).MethodName.ShouldBe(nameof(TokenMethod.Transfer));
        VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(indexed[4])).Signatory.ShouldBe(Address.FromBase58(InitAccount));
        VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(nonIndexed)).Params.ShouldBe(args);
        
        balance = _tokenContract.GetUserBalance(toAddress, "ELF");
        balance1 = _tokenContract.GetUserBalance(virtualAddress1.ToBase58(), "ELF");
        balance2 = _tokenContract.GetUserBalance(virtualAddress2.ToBase58(), "ELF");
        Logger.Info($"{balance}\n{balance1}\n{balance2}");
    }
    
    [TestMethod]
    public void FireTwoVirtualTransactionEventTest()
    {
        var toAddress = NodeManager.ListAccounts().ToList().First();
        var amount = 1_00000000;
        var virtualHash1 = HashHelper.ComputeFrom("test");
        var virtualHash2 = HashHelper.ComputeFrom("virtual");
        var virtualAddress1 = ConvertVirtualAddressToContractAddress(virtualHash1, _virtualTransactionEventTestContract.Contract);
        var virtualAddress2 = ConvertVirtualAddressToContractAddress(virtualHash2, _virtualTransactionEventTestContract.Contract);
        _tokenContract.TransferBalance(InitAccount, virtualAddress1.ToBase58(), amount * 2, "ELF");
        _tokenContract.TransferBalance(InitAccount, virtualAddress2.ToBase58(), amount * 2, "ELF");

        var balance = _tokenContract.GetUserBalance(toAddress, "ELF");
        var balance1 = _tokenContract.GetUserBalance(virtualAddress1.ToBase58(), "ELF");
        var balance2 = _tokenContract.GetUserBalance(virtualAddress2.ToBase58(), "ELF");
        Logger.Info($"{balance}\n{balance1}\n{balance2}");

        var transferArgs = new TransferInput
        {
            To = Address.FromBase58(toAddress),
            Symbol = "ELF",
            Amount = amount,
            Memo = "Test"
        }.ToByteString();
        
        var args = new FireVirtualTransactionEventTestInput
        {
            To = _tokenContract.Contract,
            Args = transferArgs,
            MethodName = nameof(TokenMethod.Transfer)
        }.ToByteString();

        var result = _virtualTransactionEventTestContract.ExecuteMethodWithResult(
            EventTestContractMethod.FireVirtualTransactionEventTest, new FireVirtualTransactionEventTestInput
            {
                To = _virtualTransactionEventTestContract.Contract,
                Args = args,
                MethodName = nameof(EventTestContractMethod.FireVirtualTransactionEventTest)
            });
        
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        var logs = result.Logs.Where(l => l.Name.Equals("VirtualTransactionCreated")).ToList();
        logs.Count.ShouldBe(3);
        var outLog = logs.First();
        var indexed = outLog.Indexed.ToList();
        var nonIndexed =  outLog.NonIndexed;
        VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(indexed[0])).VirtualHash.ShouldBe(virtualHash1);
        VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(indexed[1])).From.ShouldBe(virtualAddress1);
        VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(indexed[2])).To.ShouldBe(_virtualTransactionEventTestContract.Contract);
        VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(indexed[3])).MethodName.ShouldBe(nameof(EventTestContractMethod.FireVirtualTransactionEventTest));
        VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(indexed[4])).Signatory.ShouldBe(Address.FromBase58(InitAccount));
        VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(nonIndexed)).Params.ShouldBe(args);
        
        indexed = logs[1].Indexed.ToList();
        nonIndexed = logs[1].NonIndexed;
        VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(indexed[0])).VirtualHash.ShouldBe(virtualHash1);
        VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(indexed[1])).From.ShouldBe(virtualAddress1);
        VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(indexed[2])).To.ShouldBe(_tokenContract.Contract);
        VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(indexed[3])).MethodName.ShouldBe(nameof(TokenMethod.Transfer));
        VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(indexed[4])).Signatory.ShouldBe(virtualAddress1);
        VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(nonIndexed)).Params.ShouldBe(transferArgs);
        
        indexed = logs[2].Indexed.ToList();
        nonIndexed = logs[2].NonIndexed;
        VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(indexed[0])).VirtualHash.ShouldBe(virtualHash1);
        VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(indexed[1])).From.ShouldBe(virtualAddress1);
        VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(indexed[2])).To.ShouldBe(_tokenContract.Contract);
        VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(indexed[3])).MethodName.ShouldBe(nameof(TokenMethod.Transfer));
        VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(indexed[4])).Signatory.ShouldBe(virtualAddress2);
        VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(nonIndexed)).Params.ShouldBe(transferArgs);
        
        balance = _tokenContract.GetUserBalance(toAddress, "ELF");
        balance1 = _tokenContract.GetUserBalance(virtualAddress1.ToBase58(), "ELF");
        balance2 = _tokenContract.GetUserBalance(virtualAddress2.ToBase58(), "ELF");
        Logger.Info($"{balance}\n{balance1}\n{balance2}");
    }

    [TestMethod]
    public void SendVirtualTransactionWithoutEvent()
    {
        var toAddress = NodeManager.ListAccounts().ToList().First();
        var amount = 1_00000000;
        var virtualHash = HashHelper.ComputeFrom("test1");
        var virtualAddress = ConvertVirtualAddressToContractAddress(virtualHash, _virtualTransactionEventTestContract.Contract);
        _tokenContract.TransferBalance(InitAccount, virtualAddress.ToBase58(), amount, "ELF");
        var balance = _tokenContract.GetUserBalance(toAddress, "ELF");
        var balance1 = _tokenContract.GetUserBalance(virtualAddress.ToBase58(), "ELF");
        Logger.Info($"{balance}\n{balance1}");

        var args = new TransferInput
        {
            To = Address.FromBase58(toAddress),
            Symbol = "ELF",
            Amount = amount,
            Memo = "Test"
        }.ToByteString();
        var result = _virtualTransactionEventTestContract.ExecuteMethodWithResult(
            EventTestContractMethod.SendVirtualTransactionWithOutEvent, new FireVirtualTransactionEventTestInput
            {
                To = _tokenContract.Contract,
                Args = args,
                MethodName = nameof(TokenMethod.Transfer)
            });
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        result.Logs.Length.ShouldBe(1);
        result.Logs.First().Name.ShouldBe("Transferred");
        
        balance = _tokenContract.GetUserBalance(toAddress, "ELF");
        balance1 = _tokenContract.GetUserBalance(virtualAddress.ToBase58(), "ELF");
        Logger.Info($"{balance}\n{balance1}");
    }
    
    [TestMethod]
    public void FireVirtualTransactionEventThroughWithoutEvent()
    {
        var toAddress = NodeManager.ListAccounts().ToList().First();
        var amount = 1_00000000;
        var virtualHash = HashHelper.ComputeFrom("test1");
        var virtualHash1 = HashHelper.ComputeFrom("test");
        var virtualHash2 = HashHelper.ComputeFrom("virtual");
        
        var virtualAddress = ConvertVirtualAddressToContractAddress(virtualHash, _virtualTransactionEventTestContract.Contract);
        var virtualAddress1 = ConvertVirtualAddressToContractAddress(virtualHash1, _virtualTransactionEventTestContract.Contract);
        var virtualAddress2 = ConvertVirtualAddressToContractAddress(virtualHash2, _virtualTransactionEventTestContract.Contract);
        _tokenContract.TransferBalance(InitAccount, virtualAddress1.ToBase58(), amount * 2, "ELF");
        _tokenContract.TransferBalance(InitAccount, virtualAddress2.ToBase58(), amount * 2, "ELF");

        var balance = _tokenContract.GetUserBalance(toAddress, "ELF");
        var balance1 = _tokenContract.GetUserBalance(virtualAddress.ToBase58(), "ELF");
        var balance2 = _tokenContract.GetUserBalance(virtualAddress1.ToBase58(), "ELF");
        var balance3 = _tokenContract.GetUserBalance(virtualAddress2.ToBase58(), "ELF");

        Logger.Info($"{balance}\n{balance1}\n{balance2}\n{balance3}");

        var transferArgs = new TransferInput
        {
            To = Address.FromBase58(toAddress),
            Symbol = "ELF",
            Amount = amount,
            Memo = "Test"
        }.ToByteString();
        
        var args = new FireVirtualTransactionEventTestInput
        {
            To = _tokenContract.Contract,
            Args = transferArgs,
            MethodName = nameof(TokenMethod.Transfer)
        }.ToByteString();
        
        var result = _virtualTransactionEventTestContract.ExecuteMethodWithResult(
            EventTestContractMethod.SendVirtualTransactionWithOutEvent, new FireVirtualTransactionEventTestInput
            {
                To = _virtualTransactionEventTestContract.Contract,
                Args = args,
                MethodName = nameof(EventTestContractMethod.FireVirtualTransactionEventTest)
            });
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        var logs = result.Logs.Where(l => l.Name.Equals("VirtualTransactionCreated")).ToList();
        logs.Count.ShouldBe(1);
        var virtualLog = logs.First();
        var indexed = virtualLog.Indexed.ToList();
        var nonIndexed =  virtualLog.NonIndexed;
        VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(indexed[0])).VirtualHash.ShouldBe(virtualHash1);
        VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(indexed[1])).From.ShouldBe(virtualAddress1);
        VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(indexed[2])).To.ShouldBe(_tokenContract.Contract);
        VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(indexed[3])).MethodName.ShouldBe(nameof(TokenMethod.Transfer));
        VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(indexed[4])).Signatory.ShouldBe(virtualAddress);
        VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(nonIndexed)).Params.ShouldBe(transferArgs);

        balance = _tokenContract.GetUserBalance(toAddress, "ELF");
        balance1 = _tokenContract.GetUserBalance(virtualAddress.ToBase58(), "ELF");
        balance2 = _tokenContract.GetUserBalance(virtualAddress1.ToBase58(), "ELF");
        balance3 = _tokenContract.GetUserBalance(virtualAddress2.ToBase58(), "ELF");

        Logger.Info($"{balance}\n{balance1}\n{balance2}\n{balance3}");
    }

    [TestMethod]
    [DataRow(true)]
    public void SendVirtualInlineBySystemContract_Association(bool isEvent)
    {
        var toAddress = NodeManager.ListAccounts().ToList().First();
        var amount = 1_00000000; 
        var list = NodeManager.ListAccounts().Take(3).ToList();
        if (!list.Contains(InitAccount))
            list.Add(InitAccount);
        var organizationAddress = AuthorityManager.CreateAssociationOrganization(list);
        var organizationInfo = _associationContract.GetOrganization(organizationAddress);
        var virtualHash = HashHelper.ConcatAndCompute(organizationInfo.OrganizationHash, organizationInfo.CreationToken);
        foreach (var member in list)
        {
            if (member.Equals(InitAccount))
                continue;
            _tokenContract.TransferBalance(InitAccount, member, 100_00000000);
        }
        _tokenContract.TransferBalance(InitAccount, organizationAddress.ToBase58(), amount);
        var balance = _tokenContract.GetUserBalance(organizationAddress.ToBase58());
        var balance1 = _tokenContract.GetUserBalance(toAddress);
        Logger.Info($"{balance}\n{balance1}\n");
        
        var args = new TransferInput
        {
            To = Address.FromBase58(toAddress),
            Symbol = "ELF",
            Amount = amount,
            Memo = "Test"
        };
        var proposer = organizationInfo.ProposerWhiteList.Proposers.First();
        var createProposal = _associationContract.CreateProposal(_tokenContract.ContractAddress,
            nameof(TokenMethod.Transfer), args, organizationAddress, proposer.ToBase58());
        foreach (var member in list)
            _associationContract.ApproveProposal(createProposal, member);
        var releaseProposal = _associationContract.ReleaseProposal(createProposal, proposer.ToBase58());
        releaseProposal.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        var logs = releaseProposal.Logs.Where(l => l.Name.Equals("VirtualTransactionCreated")).ToList();

        if (isEvent)
        {
            logs.Count.ShouldBe(1);
            var virtualLog = logs.First();
            var indexed = virtualLog.Indexed.ToList();
            var nonIndexed =  virtualLog.NonIndexed;
            VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(indexed[0])).VirtualHash.ShouldBe(virtualHash);
            VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(indexed[1])).From.ShouldBe(organizationAddress);
            VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(indexed[2])).To.ShouldBe(_tokenContract.Contract);
            VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(indexed[3])).MethodName.ShouldBe(nameof(TokenMethod.Transfer));
            VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(indexed[4])).Signatory.ShouldBe(proposer);
            VirtualTransactionCreated.Parser.ParseFrom(ByteString.FromBase64(nonIndexed)).Params.ShouldBe(args.ToByteString());
        }
        else
        {
            logs.Count.ShouldBe(0);
        } 
        balance = _tokenContract.GetUserBalance(organizationAddress.ToBase58());
        balance1 = _tokenContract.GetUserBalance(toAddress);
        Logger.Info($"{balance}\n{balance1}\n");
    }
        
    
    private Address ConvertVirtualAddressToContractAddress(Hash virtualAddress, Address contractAddress)
    {
        return Address.FromPublicKey(contractAddress.Value.Concat(
            virtualAddress.Value.ToByteArray().ComputeHash()).ToArray());
    }
}