using AElf;
using AElf.Client.Dto;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Timelock;
using AElf.Contracts.TokenConverter;
using AElf.Standards.ACS0;
using AElf.Standards.ACS3;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Volo.Abp.Threading;
using InitializeInput = AElf.Contracts.Timelock.InitializeInput;

namespace FeatureTest;

[TestClass]
public class TimeLockTest
{
    private ILog Logger { get; set; }
    private INodeManager NodeManager { get; set; }
    private AuthorityManager AuthorityManager { get; set; }

    private TokenContract _tokenContract;
    private GenesisContract _genesisContract;

    private string _proxyContractAddress = "RXcxgSXuagn8RrvhQAV81Z652EEYSwR6JLnqHYJ5UVpEptW8Y";
    private string _timeLockContractAddress = "2wRDbyVF28VBQoSPgdSEFaL4x7CaXz8TCBujYhgWc9qTMxBE3n";

    //RXcxgSXuagn8RrvhQAV81Z652EEYSwR6JLnqHYJ5UVpEptW8Y
    //2wRDbyVF28VBQoSPgdSEFaL4x7CaXz8TCBujYhgWc9qTMxBE3n

    //2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS
    //2WHXRoLRjbUTDQsuqR5CntygVfnDb125qdJkudev4kVNbLhTdG

    private ProxyAccountContract _proxyAccountContract;
    private TimeLockContract _timeLockContract;

    private string InitAccount { get; } = "2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd";
    private string TestAccount { get; set; }
    private static string RpcUrl { get; } = "127.0.0.1:8011";
    public const long MinDelay = 10;
    public const long MaxDelay = 3600;
    public const long GracePeriod = 600;
    private ulong delayTime = 300;

    [TestInitialize]
    public void Initialize()
    {
        Log4NetHelper.LogInit("TimeLockTest");
        Logger = Log4NetHelper.GetLogger();
        NodeInfoHelper.SetConfig("nodes");

        NodeManager = new NodeManager(RpcUrl);
        AuthorityManager = new AuthorityManager(NodeManager, InitAccount);

        _genesisContract = GenesisContract.GetGenesisContract(NodeManager, InitAccount);
        _tokenContract = _genesisContract.GetTokenContract(InitAccount);
        _proxyAccountContract = _proxyContractAddress == ""
            ? new ProxyAccountContract(NodeManager, InitAccount)
            : new ProxyAccountContract(NodeManager, InitAccount, _proxyContractAddress);
        _timeLockContract = _timeLockContractAddress == ""
            ? new TimeLockContract(NodeManager, InitAccount)
            : new TimeLockContract(NodeManager, InitAccount, _timeLockContractAddress);
        TestAccount = NodeManager.ListAccounts().First();
    }

    [TestMethod]
    public void InitializeTest()
    {
        TransactionResultDto result;
        {
            _timeLockContract.SetAccount(TestAccount);
            result = _timeLockContract.ExecuteMethodWithResult(TimeLockMethod.Initialize,
                new InitializeInput
                {
                    Delay = 300
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.Error.ShouldContain("No permission.");
        }
        {
            _timeLockContract.SetAccount(InitAccount);
            result = _timeLockContract.ExecuteMethodWithResult(TimeLockMethod.Initialize,
                new InitializeInput
                {
                    Delay = 1
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.Error.ShouldContain("Delay must exceed minimum delay");
        }
        {
            result = _timeLockContract.ExecuteMethodWithResult(TimeLockMethod.Initialize,
                new InitializeInput
                {
                    Delay = 7200
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.Error.ShouldContain("Delay must not exceed maximum delay");
        }
        {
            result = _timeLockContract.ExecuteMethodWithResult(TimeLockMethod.Initialize,
                new InitializeInput
                {
                    Delay = delayTime
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }
        {
            result = _timeLockContract.ExecuteMethodWithResult(TimeLockMethod.Initialize,
                new InitializeInput
                {
                    Delay = 600
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.NodeValidationFailed);
            result.Error.ShouldContain("Already initialized.");
        }

        var admin = _timeLockContract.GetAdmin();
        admin.ShouldBe(Address.FromBase58(InitAccount));
        var delay = _timeLockContract.GetDelay();
        delay.ShouldBe(delayTime);
    }

    [TestMethod]
    public void QueueTransactionTest()
    {
        var toAddress = "2AYBtuTNqo65vz6mv8qK4YjrGnbgkkwzyD39R1BZwSP24NMAPi";
        var dateTime = DateTime.UtcNow.AddSeconds(delayTime + 30);
        var executeTime = Timestamp.FromDateTime(dateTime);
        var input = new TransferInput
        {
            Symbol = "ELF",
            Amount = 1_00000000,
            To = Address.FromBase58(toAddress),
            Memo = "Memo"
        };
        var transactionInput = new TransactionInput
        {
            Target = _tokenContract.Contract,
            Method = nameof(TokenMethod.Transfer),
            ExecuteTime = executeTime,
            Data = input.ToByteString()
        };

        var txnHash = HashHelper.ComputeFrom(transactionInput);
        var queueTransaction =
            _timeLockContract.ExecuteMethodWithResult(TimeLockMethod.QueueTransaction, transactionInput);
        queueTransaction.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        var logs = queueTransaction.Logs.First(l => l.Name.Equals("QueueTransaction"));
        var logInfo = QueueTransaction.Parser.ParseFrom(ByteString.FromBase64(logs.NonIndexed));
        logInfo.Data.ShouldBe(input.ToByteString());
        logInfo.Method.ShouldBe("Transfer");
        logInfo.Target.ShouldBe(_tokenContract.Contract);
        Logger.Info(logInfo);
        Logger.Info(txnHash.ToHex());
        var getTransaction = _timeLockContract.GetTransaction(txnHash);
        getTransaction.ShouldBeTrue();
    }

    //{ "target": "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE", "method": "Transfer", "data": "CiIKIG3aC9b0B9prUs1D0V0dPsQTO+3ZFXHoESbjg7Xh0G2aEgNFTEYYgMLXLyIETWVtbw==", "executeTime": "2023-09-27T10:16:24.976579Z" }
    //{ "target": "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE", "method": "Transfer", "data": "CiIKIJlaR9WtX+4+npnkA4uV0owddl3I9pPVOBV++UrT9rNIEgNFTEYYgMLXLyIETWVtbw==", "executeTime": "2023-09-28T03:57:15.084511Z" }
    [TestMethod]
    [DataRow("2AYBtuTNqo65vz6mv8qK4YjrGnbgkkwzyD39R1BZwSP24NMAPi", "2023-09-28T03:57:15.084511Z",
        "393503fe84cea9d28f028a1de446a37844abe9157ebb0f474eae6913ba05f605")]
    public void ExecuteTransactionTest(string toAddress, string dateTimeStr, string txId)
    {
        var queueTransaction = AsyncHelper.RunSync(() => NodeManager.ApiClient.GetTransactionResultAsync(txId));
        var queueLogs = QueueTransaction.Parser.ParseFrom(
            ByteString.FromBase64(queueTransaction.Logs.First(l => l.Name.Equals("QueueTransaction")).NonIndexed));
        var executeTime = GetTimestamp(dateTimeStr);
        _tokenContract.TransferBalance(InitAccount, _timeLockContractAddress, 1000000000);
        var timeLockBalance = _tokenContract.GetUserBalance(_timeLockContractAddress);

        Logger.Info(executeTime);
        var input = new TransferInput
        {
            Symbol = "ELF",
            Amount = 1_00000000,
            To = Address.FromBase58(toAddress),
            Memo = "Memo"
        };
        var transactionInput = new TransactionInput
        {
            Target = _tokenContract.Contract,
            Method = nameof(TokenMethod.Transfer),
            ExecuteTime = executeTime,
            Data = input.ToByteString()
        };

        var txnHash = HashHelper.ComputeFrom(transactionInput);
        var getTransaction = _timeLockContract.GetTransaction(txnHash);
        getTransaction.ShouldBeTrue();

        var executeTransaction =
            _timeLockContract.ExecuteMethodWithResult(TimeLockMethod.ExecuteTransaction, transactionInput);
        executeTransaction.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        var logs = executeTransaction.Logs.First(l => l.Name.Equals("ExecuteTransaction"));
        var logInfo = ExecuteTransaction.Parser.ParseFrom(ByteString.FromBase64(logs.NonIndexed));
        queueLogs.Data.ShouldBe(logInfo.Data);
        queueLogs.Target.ShouldBe(logInfo.Target);
        queueLogs.ExecuteTime.ShouldBe(logInfo.ExecuteTime);
        queueLogs.Method.ShouldBe(logInfo.Method);

        var transferLogs = executeTransaction.Logs.First(l => l.Name.Equals("Transferred"));
        foreach (var indexed in transferLogs.Indexed)
        {
            var transferredIndexed = Transferred.Parser.ParseFrom(ByteString.FromBase64(indexed));
            if (transferredIndexed.Symbol.Equals(""))
            {
                Logger.Info(transferredIndexed.From == null
                    ? $"To: {transferredIndexed.To}"
                    : $"From: {transferredIndexed.From}");
            }
            else
                Logger.Info($"Symbol: {transferredIndexed.Symbol}");
        }

        getTransaction = _timeLockContract.GetTransaction(txnHash);
        getTransaction.ShouldBeFalse();
        var afterTimeLockBalance = _tokenContract.GetUserBalance(_timeLockContractAddress);
        afterTimeLockBalance.ShouldBe(timeLockBalance - 1_00000000);
    }

    [TestMethod]
    [DataRow("2AYBtuTNqo65vz6mv8qK4YjrGnbgkkwzyD39R1BZwSP24NMAPi", "2023-09-28T02:36:54.233536Z",
        "398011ee2f06cf2dff5e573949a0aa7275a9174a70adcafe702ec76e5e2f9f47")]
    public void CancelTransactionTest(string toAddress, string dateTimeStr, string txId)
    {
        var queueTransaction = AsyncHelper.RunSync(() => NodeManager.ApiClient.GetTransactionResultAsync(txId));
        var queueLogs = QueueTransaction.Parser.ParseFrom(
            ByteString.FromBase64(queueTransaction.Logs.First(l => l.Name.Equals("QueueTransaction")).NonIndexed));
        var executeTime = GetTimestamp(dateTimeStr);
        Logger.Info(executeTime);
        var input = new TransferInput
        {
            Symbol = "ELF",
            Amount = 1_00000000,
            To = Address.FromBase58(toAddress),
            Memo = "Memo"
        };
        var transactionInput = new TransactionInput
        {
            Target = _tokenContract.Contract,
            Method = nameof(TokenMethod.Transfer),
            ExecuteTime = executeTime,
            Data = input.ToByteString()
        };

        var txnHash = HashHelper.ComputeFrom(transactionInput);
        var getTransaction = _timeLockContract.GetTransaction(txnHash);
        getTransaction.ShouldBeTrue();

        var executeTransaction =
            _timeLockContract.ExecuteMethodWithResult(TimeLockMethod.CancelTransaction, transactionInput);
        executeTransaction.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        var logs = executeTransaction.Logs.First(l => l.Name.Equals("CancelTransaction"));
        var logInfo = CancelTransaction.Parser.ParseFrom(ByteString.FromBase64(logs.NonIndexed));
        queueLogs.Data.ShouldBe(logInfo.Data);
        queueLogs.Target.ShouldBe(logInfo.Target);
        queueLogs.ExecuteTime.ShouldBe(logInfo.ExecuteTime);
        queueLogs.Method.ShouldBe(logInfo.Method);

        getTransaction = _timeLockContract.GetTransaction(txnHash);
        getTransaction.ShouldBeFalse();
    }

    [TestMethod]
    public void MultiQueueTransactionTest()
    {
        var transactionInputs = new List<TransactionInput>();
        var tokenConverter = _genesisContract.GetTokenConverterContract(InitAccount);
        _tokenContract.TransferBalance(InitAccount, _timeLockContractAddress, 1000000000);
        {
            var burn = new BurnInput()
            {
                Symbol = "CPU",
                Amount = 1_0000000
            };
            var transactionInput = GenerateInput(_tokenContract.Contract, nameof(TokenMethod.Burn), 300, burn);
            transactionInputs.Add(transactionInput);
        }
        {
            var buyInput = new BuyInput
            {
                Symbol = "CPU",
                Amount = 20_0000000
            };
            var transactionInput =
                GenerateInput(tokenConverter.Contract, nameof(TokenConverterMethod.Buy), 600, buyInput);
            transactionInputs.Add(transactionInput);
        }

        foreach (var t in transactionInputs)
            GenerateQueueTransaction(t);
    }

    [TestMethod]
    public void MultiDeployQueueTransactionTest()
    {
        var transactionInputs = new List<TransactionInput>();
        _tokenContract.TransferBalance(InitAccount, _timeLockContractAddress, 1000000000);
        var contractReader = new SmartContractReader(CommonHelper.GetCurrentDataDir());
        {
           
            var codeArray = contractReader.Read("AElf.Contracts.TestContract.VirtualTransactionEvent");
            var deployInput = new ContractDeploymentInput
            {
                Code = ByteString.CopyFrom(codeArray),
                Category = 0
            };
            var transactionInput = GenerateInput(_genesisContract.Contract, nameof(GenesisMethod.ProposeNewContract),
                600, deployInput);
            transactionInputs.Add(transactionInput);
        }
        { 
            var codeArray = contractReader.Read("AElf.Contracts.TestContract.B");
            var userDeployInput = new ContractDeploymentInput 
            { 
                Code = ByteString.CopyFrom(codeArray),
                Category = 0
            };
            var transactionInput = GenerateInput(_genesisContract.Contract, nameof(GenesisMethod.DeployUserSmartContract), 
                600, userDeployInput);
                transactionInputs.Add(transactionInput);
        }

        foreach (var t in transactionInputs)
                GenerateQueueTransaction(t);
    }

    [TestMethod]
    public void MultiExecuteTransactionTest()
    {
        var tokenConverter = _genesisContract.GetTokenConverterContract(InitAccount);
        // _tokenContract.TransferBalance(InitAccount, _timeLockContractAddress, 1000000000);
        var timeLockBalance = _tokenContract.GetUserBalance(_timeLockContractAddress, "CPU");
        Logger.Info(timeLockBalance);
        var transactionInputs = new List<TransactionInput>();
        {
            var buyInput = new BuyInput
            {
                Symbol = "CPU",
                Amount = 20_0000000
            };
            var transactionInput = GenerateInput(tokenConverter.Contract, nameof(TokenConverterMethod.Buy),
                "2023-09-28T03:51:09.435926Z", buyInput);
            transactionInputs.Add(transactionInput);
        }
        {
            var burn = new BurnInput()
            {
                Symbol = "CPU",
                Amount = 1_0000000
            };
            var transactionInput = GenerateInput(_tokenContract.Contract, nameof(TokenMethod.Burn),
                "2023-09-28T03:46:09.371671Z", burn);
            transactionInputs.Add(transactionInput);
        }
       
        foreach (var input in transactionInputs)
            GenerateExecuteTransaction(input);

        timeLockBalance = _tokenContract.GetUserBalance(_timeLockContractAddress, "CPU");
        Logger.Info(timeLockBalance);
    }

    [TestMethod]
    public void MultiDeployExecutedTransactionTest()
    {
        var transactionInputs = new List<TransactionInput>();
        _tokenContract.TransferBalance(InitAccount, _timeLockContractAddress, 1000000000);
        var contractReader = new SmartContractReader(CommonHelper.GetCurrentDataDir());
        {
           
            var codeArray = contractReader.Read("AElf.Contracts.TestContract.VirtualTransactionEvent");
            var deployInput = new ContractDeploymentInput
            {
                Code = ByteString.CopyFrom(codeArray),
                Category = 0
            };
            var transactionInput = GenerateInput(_genesisContract.Contract, nameof(GenesisMethod.ProposeNewContract),
                "2023-09-28T06:10:14.327150Z", deployInput);
            transactionInputs.Add(transactionInput);
        }
        { 
            var codeArray = contractReader.Read("AElf.Contracts.TestContract.B");
            var userDeployInput = new ContractDeploymentInput 
            { 
                Code = ByteString.CopyFrom(codeArray),
                Category = 0
            };
            var transactionInput = GenerateInput(_genesisContract.Contract, nameof(GenesisMethod.DeployUserSmartContract), 
                "2023-09-28T06:10:14.385784Z", userDeployInput);
            transactionInputs.Add(transactionInput);
        }

        foreach (var input in transactionInputs)
            GenerateExecuteTransaction(input);
    }

    [TestMethod]
    public void QueueReleaseApprove()
    {
        var txId = "f1daa8ad10f62c3629280285dd34fb33e8508cb278bf7babee82dee06c958be9";
        var logs = NodeManager.CheckTransactionResult(txId);
        var info = logs.Logs.First(l => l.Name.Equals("ContractProposed")).NonIndexed;
        var proposalInfo = logs.Logs.First(l => l.Name.Equals("ProposalCreated")).NonIndexed;
        var proposalId = ProposalCreated.Parser.ParseFrom(ByteString.FromBase64(proposalInfo)).ProposalId;
        var contractHash = ContractProposed.Parser.ParseFrom(ByteString.FromBase64(info)).ProposedContractInputHash;
        var parliament = _genesisContract.GetParliamentContract(InitAccount);
        parliament.Approve(proposalId, InitAccount);
        
        var input = new ReleaseContractInput
        {
            ProposalId = proposalId,
            ProposedContractInputHash = contractHash
        };
        var transactionInput = GenerateInput(_genesisContract.Contract, nameof(GenesisMethod.ReleaseApprovedContract),
            300, input);
        var queue = GenerateQueueTransaction(transactionInput);
        queue.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
    }
    // { "target": "2dtnkWDyJJXeDRcREhKSZHrYdDGMbn3eus5KYpXonfoTygFHZm", "method": "ReleaseApprovedContract", "data": "CiIKIDw6baMrIONX5mbrY5BfAmC6GHxabdk0H5FqppulG9GSEiIKIBnJsxZsNCYgAa0F+s8hfZjvkwwt57j72uG8dXcTTf6F", "executeTime": "2023-09-28T06:34:54.946080Z" }
    [TestMethod]
    public void ExecuteReleaseApprove()
    {
        var txId = "f1daa8ad10f62c3629280285dd34fb33e8508cb278bf7babee82dee06c958be9";
        var logs = NodeManager.CheckTransactionResult(txId);
        var info = logs.Logs.First(l => l.Name.Equals("ContractProposed")).NonIndexed;
        var proposalInfo = logs.Logs.First(l => l.Name.Equals("ProposalCreated")).NonIndexed;
        var proposalId = ProposalCreated.Parser.ParseFrom(ByteString.FromBase64(proposalInfo)).ProposalId;
        var contractHash = ContractProposed.Parser.ParseFrom(ByteString.FromBase64(info)).ProposedContractInputHash;
      
        var input = new ReleaseContractInput
        {
            ProposalId = proposalId,
            ProposedContractInputHash = contractHash
        };
        var transactionInput = GenerateInput(_genesisContract.Contract, nameof(GenesisMethod.ReleaseApprovedContract),
            "2023-09-28T06:34:54.946080Z", input);
        var execute = GenerateExecuteTransaction(transactionInput);
        execute.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
    }
    
    [TestMethod]
    public void QueueReleaseCodeCheck()
    {
        var txId = "f1daa8ad10f62c3629280285dd34fb33e8508cb278bf7babee82dee06c958be9";
        var releaseTxId = "9f765deb6e2ef2c6499befccd8c34b9623ad8a97d41f25103970d48468546054";
        var releaseLogs = NodeManager.CheckTransactionResult(releaseTxId);
        var proposalInfo = releaseLogs.Logs.First(l => l.Name.Equals("ProposalCreated")).NonIndexed;
        var proposalId = ProposalCreated.Parser.ParseFrom(ByteString.FromBase64(proposalInfo)).ProposalId;
        
        var logs = NodeManager.CheckTransactionResult(txId);
        var info = logs.Logs.First(l => l.Name.Equals("ContractProposed")).NonIndexed;
        var contractHash = ContractProposed.Parser.ParseFrom(ByteString.FromBase64(info)).ProposedContractInputHash;

        var input = new ReleaseContractInput
        {
            ProposalId = proposalId,
            ProposedContractInputHash = contractHash
        };
        var transactionInput = GenerateInput(_genesisContract.Contract, nameof(GenesisMethod.ReleaseCodeCheckedContract),
            30, input);
        var queue = GenerateQueueTransaction(transactionInput);
        queue.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
    }
    //{ "target": "2dtnkWDyJJXeDRcREhKSZHrYdDGMbn3eus5KYpXonfoTygFHZm", "method": "ReleaseCodeCheckedContract", "data": "CiIKIARFDwFDILOWpAeFcI3p+0xZL2JKcCGtLwNbUrtq/zfYEiIKIBnJsxZsNCYgAa0F+s8hfZjvkwwt57j72uG8dXcTTf6F", "executeTime": "2023-09-28T06:43:31.828930Z" }
    [TestMethod]
    public void ExecuteReleaseCodeCheck()
    {
        var txId = "f1daa8ad10f62c3629280285dd34fb33e8508cb278bf7babee82dee06c958be9";
        var releaseTxId = "9f765deb6e2ef2c6499befccd8c34b9623ad8a97d41f25103970d48468546054";
        var releaseLogs = NodeManager.CheckTransactionResult(releaseTxId);
        var proposalInfo = releaseLogs.Logs.First(l => l.Name.Equals("ProposalCreated")).NonIndexed;
        var proposalId = ProposalCreated.Parser.ParseFrom(ByteString.FromBase64(proposalInfo)).ProposalId;
        
        var logs = NodeManager.CheckTransactionResult(txId);
        var info = logs.Logs.First(l => l.Name.Equals("ContractProposed")).NonIndexed;
        var contractHash = ContractProposed.Parser.ParseFrom(ByteString.FromBase64(info)).ProposedContractInputHash;

        var input = new ReleaseContractInput
        {
            ProposalId = proposalId,
            ProposedContractInputHash = contractHash
        };
        var transactionInput = GenerateInput(_genesisContract.Contract, nameof(GenesisMethod.ReleaseCodeCheckedContract),
            "2023-09-28T06:43:31.828930Z", input);
        var execute = GenerateExecuteTransaction(transactionInput);
        execute.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
    }

    private TransactionResultDto GenerateQueueTransaction(TransactionInput input)
    {
        var txnHash = HashHelper.ComputeFrom(input);
        var queueTransaction =
            _timeLockContract.ExecuteMethodWithResult(TimeLockMethod.QueueTransaction, input);
        queueTransaction.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        var logs = queueTransaction.Logs.First(l => l.Name.Equals("QueueTransaction"));
        var logInfo = QueueTransaction.Parser.ParseFrom(ByteString.FromBase64(logs.NonIndexed));
        logInfo.Data.ShouldBe(input.Data);
        logInfo.Method.ShouldBe(input.Method);
        logInfo.Target.ShouldBe(input.Target);
        Logger.Info(logInfo);
        Logger.Info(txnHash.ToHex());
        var getTransaction = _timeLockContract.GetTransaction(txnHash);
        getTransaction.ShouldBeTrue();
        return queueTransaction;
    }

    private TransactionResultDto GenerateExecuteTransaction(TransactionInput transactionInput)
    {
        var txnHash = HashHelper.ComputeFrom(transactionInput);
        var getTransaction = _timeLockContract.GetTransaction(txnHash);
        getTransaction.ShouldBeTrue();

        var executeTransaction =
            _timeLockContract.ExecuteMethodWithResult(TimeLockMethod.ExecuteTransaction, transactionInput);
        executeTransaction.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        var logs = executeTransaction.Logs.First(l => l.Name.Equals("ExecuteTransaction"));
        var logInfo = ExecuteTransaction.Parser.ParseFrom(ByteString.FromBase64(logs.NonIndexed));
        Logger.Info(logInfo);
        logInfo.Data.ShouldBe(transactionInput.Data);
        logInfo.Target.ShouldBe(transactionInput.Target);
        logInfo.ExecuteTime.ShouldBe(transactionInput.ExecuteTime);
        logInfo.Method.ShouldBe(transactionInput.Method);

        getTransaction = _timeLockContract.GetTransaction(txnHash);
        getTransaction.ShouldBeFalse();
        return executeTransaction;
    }

    private Timestamp GetTimestamp(string dateTimeStr)
    {
        var dateTimeOffset = DateTimeOffset.Parse(dateTimeStr);
        return Timestamp.FromDateTime(dateTimeOffset.UtcDateTime);
    }

    private TransactionInput GenerateInput(Address target, string method, ulong addTime, IMessage input)
    {
        var dateTime = DateTime.UtcNow.AddSeconds(delayTime + addTime);
        var executeTime = Timestamp.FromDateTime(dateTime);
        Logger.Info(dateTime);
        Logger.Info(executeTime);
        var transactionInput = new TransactionInput
        {
            Target = target,
            Method = method,
            ExecuteTime = executeTime,
            Data = input.ToByteString()
        };
        return transactionInput;
    }

    private TransactionInput GenerateInput(Address target, string method, string dateTimeStr, IMessage input)
    {
        var executeTime = GetTimestamp(dateTimeStr);
        Logger.Info(executeTime);
        var transactionInput = new TransactionInput
        {
            Target = target,
            Method = method,
            ExecuteTime = executeTime,
            Data = input.ToByteString()
        };
        return transactionInput;
    }
}