using System.Collections.Concurrent;
using System.Diagnostics;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ProxyAccountContract;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Shouldly;
using CreateInput = AElf.Contracts.ProxyAccountContract.CreateInput;

namespace PortkeyCryptoBoxContractScript;

public class ProxyService : IContractService
{
    public ProxyService(Service service, AccountService accountService, ILog logger,
        string proxyAccount = "") 
    {
        _service = service;
        _logger = logger;
        _nodeManager = service.NodeManager;
        _accountService = accountService;
        _proxyAccountContract = proxyAccount == ""
            ? new ProxyAccountContract(_nodeManager, service.CallAddress, "ProxyAccountContract", false)
            : new ProxyAccountContract(_nodeManager, service.CallAddress, proxyAccount);
    }

    public List<ProxyAccountInfo> InitProxyAccount(bool isNeedDelegatee, List<string> eoaAccountList)
    {
        _logger.Info("=== Create ProxyAccount ===");
        var proxyAccountInfos = CreateProxyAccountParallel(eoaAccountList);
        var proxyAccountList = proxyAccountInfos.Select(p => p.ProxyAddress.ToBase58()).ToList();
        var proxyManagerList = proxyAccountInfos.Select(p => p.ManagerAddress.ToBase58()).ToList();
        if (isNeedDelegatee)
        {
            _logger.Info("=== Set Secoundary Delegate ===");
            SetSecondaryDelegate(proxyAccountInfos);
            _accountService.CheckDelegatee(proxyManagerList);

            _logger.Info("=== Set InitAddress Secoundary Delegate ===");
            _accountService.SetSecondaryDelegate(
                proxyManagerList,
                _service.CallAddress);
            _accountService.CheckDelegatee(proxyManagerList);
            _logger.Info("=== Transfer NativeToken to proxy address ===");
            _accountService.TransferAccountForSizeFee(
                proxyAccountList, "ELF", 50_00000000);
        }

        _logger.Info("=== Transfer NFT to tester address ===");
        _accountService.TransferAccountForSizeFee(proxyAccountList, "BEANPASS-1", 1);

        _logger.Info("=== Check proxy NativeToken Balance ===");
        _accountService.CheckBalance(proxyAccountList, "ELF");

        _logger.Info("=== Check proxy NFT Balance ===");
        _accountService.CheckBalance(proxyAccountList, "BEANPASS-1");

        _logger.Info("=== Check manager NativeToken Balance ===");
        _accountService.CheckBalance(proxyManagerList, "ELF");
        return proxyAccountInfos;
    }

    public List<ProxyAccountInfo> CreateProxyAccount(List<string> eoaAccountList)
    {
        var proxyAccountList = new List<ProxyAccountInfo>();
        foreach (var eoaAccount in eoaAccountList)
        {
            var managerList = new List<ManagementAddress> { new() { Address = eoaAccount.ConvertAddress() } };
            var result = _proxyAccountContract.ExecuteMethodWithResult(ProxyMethod.Create,
                new CreateInput
                {
                    ManagementAddresses = { managerList }
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logs = result.Logs.First(l => l.Name.Equals("ProxyAccountCreated")).NonIndexed;
            var proxyAccountCreated = ProxyAccountCreated.Parser.ParseFrom(ByteString.FromBase64(logs));
            _logger.Info(proxyAccountCreated.ProxyAccountAddress);
            var proxyInfo = new ProxyAccountInfo(proxyAccountCreated.ProxyAccountHash,
                proxyAccountCreated.ProxyAccountAddress, Address.FromBase58(eoaAccount));
            proxyAccountList.Add(proxyInfo);
        }

        return proxyAccountList;
    }

    public List<ProxyAccountInfo> CreateProxyAccountParallel(List<string> eoaAccountList)
    {
        var proxyAccountList = new ConcurrentQueue<ProxyAccountInfo>();
        Parallel.For(0, eoaAccountList.Count, i =>
        {
            var managerList = new List<ManagementAddress> { new() { Address = eoaAccountList[i].ConvertAddress() } };
            var result = _proxyAccountContract.ExecuteMethodWithResult(ProxyMethod.Create,
                new CreateInput
                {
                    ManagementAddresses = { managerList }
                });
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var logs = result.Logs.First(l => l.Name.Equals("ProxyAccountCreated")).NonIndexed;
            var proxyAccountCreated = ProxyAccountCreated.Parser.ParseFrom(ByteString.FromBase64(logs));
            _logger.Info(proxyAccountCreated.ProxyAccountAddress);
            var proxyInfo = new ProxyAccountInfo(proxyAccountCreated.ProxyAccountHash,
                proxyAccountCreated.ProxyAccountAddress, Address.FromBase58(eoaAccountList[i]));
            proxyAccountList.Enqueue(proxyInfo);
        });

        return proxyAccountList.ToList();
    }


    public void SetSecondaryDelegate(List<ProxyAccountInfo> proxyAccountList)
    {
        var txIds = new List<string>();
        foreach (var proxyAccountInfo in proxyAccountList)
        {
            var manger = proxyAccountInfo.ManagerAddress;
            var checkDelegatee = _service.TokenService.GetTransactionFeeDelegatees(manger.ToBase58());
            if (checkDelegatee.DelegateeAddresses.Any())
            {
                _logger.Info($"Delegator: {manger.ToBase58()}");
                _logger.Info("=== Remove old delegatee === ");
                foreach (var delegatee in checkDelegatee.DelegateeAddresses)
                {
                    _logger.Info($"Delegatee: {delegatee.ToBase58()}");
                }

                RemoveDelegation(checkDelegatee.DelegateeAddresses.Select(d => d).ToList(), manger.ToBase58());
            }

            var delegations = new Dictionary<string, long>
            {
                [Constants.FreeSymbol] = Constants.FreeAmount
            };
            var input = new SetTransactionFeeDelegationsInput()
            {
                DelegatorAddress = manger,
                Delegations =
                {
                    delegations
                }
            };
            var inputForwardCall = new ForwardCallInput
            {
                ContractAddress = _service.TokenService.Contract,
                MethodName = nameof(TokenMethod.SetTransactionFeeDelegations),
                ProxyAccountHash = proxyAccountInfo.ProxyHash,
                Args = input.ToByteString()
            };
            _proxyAccountContract.SetAccount(manger.ToBase58());
            var txId = _proxyAccountContract.ExecuteMethodWithTxId(ProxyMethod.ForwardCall, inputForwardCall);
            txIds.Add(txId);
        }

        _nodeManager.CheckTransactionListResult(txIds);
    }

    private void RemoveDelegation(List<Address> delegatees, string delegator)
    {
        var txIds = new List<string>();
        foreach (var delegatee in delegatees)
        {
            var input = new RemoveTransactionFeeDelegateeInput()
            {
                DelegateeAddress = delegatee
            };
            _service.TokenService.SetAccount(delegator);
            var txId = _service.TokenService.ExecuteMethodWithTxId(TokenMethod.RemoveTransactionFeeDelegatee, input);
            txIds.Add(txId);
        }

        _nodeManager.CheckTransactionListResult(txIds);
    }

    public IContractService UpdateService(Service service)
    {
        throw new NotImplementedException();
    }

    public async Task SendMultiTransactionsOneBytOneTasks(string symbol, long size, List<string> tester, string method)
    {
        await _contractServiceImplementation.SendMultiTransactionsOneBytOneTasks(symbol, size, tester, method);
    }

    public List<string> SendMultiTransactions(string symbol, long size, List<string> tester, string method, out DateTime sendTime)
    {
        return _contractServiceImplementation.SendMultiTransactions(symbol, size, tester, method, out sendTime);
    }
    
    public List<string> CreateProxyMethodTest(List<ProxyAccountInfo> proxyAccountInfos, int count, string contract, string method, IMessage input, 
        out DateTime sendTime)
    {
        var rawTransactionList = new ConcurrentBag<string>();
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        Parallel.For(0, proxyAccountInfos.Count, item =>
        {
            var inputForwardCall = new ForwardCallInput
            {
                ContractAddress = Address.FromBase58(contract),
                MethodName = method,
                ProxyAccountHash = proxyAccountInfos[item].ProxyHash,
                Args = input.ToByteString()
            };
            var requestInfo =
                _nodeManager.GenerateRawTransaction(proxyAccountInfos[item].ManagerAddress.ToBase58(),
                    _proxyAccountContract.ContractAddress,
                    nameof(ProxyMethod.ForwardCall),
                    inputForwardCall);
            rawTransactionList.Add(requestInfo);
        });

        stopwatch.Stop();
        var createTxsTime = stopwatch.ElapsedMilliseconds;

        //Send batch transaction requests 
        stopwatch.Restart();
        var rawTransactions = string.Join(",", rawTransactionList);
        var transactions = _nodeManager.SendTransactions(rawTransactions);
        stopwatch.Stop();
        sendTime = DateTime.UtcNow;
        foreach (var tx in transactions)
        {
            _logger.Info($"{tx} send to txhub time: {sendTime}");
        }

        var requestTxsTime = stopwatch.ElapsedMilliseconds;
        _logger.Info(
            $"create transaction count: {transactions.Count}, send to txhub time: {sendTime}, create time: {createTxsTime}ms, request time: {requestTxsTime}ms.");
        return transactions;
    }


    public void InitializeContract()
    {
        _proxyAccountContract.ExecuteMethodWithResult(ProxyMethod.Initialize, new Empty());
    }

    private readonly ILog _logger;
    private readonly Service _service;
    private readonly INodeManager _nodeManager;
    private readonly ProxyAccountContract _proxyAccountContract;
    private readonly AccountService _accountService;
    private IContractService _contractServiceImplementation;

    public class ProxyAccountInfo
    {
        public ProxyAccountInfo(Hash proxyHash, Address proxyAddress, Address managerAddress)
        {
            ProxyHash = proxyHash;
            ProxyAddress = proxyAddress;
            ManagerAddress = managerAddress;
        }

        public Hash ProxyHash { get; set; }
        public Address ProxyAddress { get; set; }
        public Address ManagerAddress { get; set; }
    }
}