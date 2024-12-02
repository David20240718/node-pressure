using System.Collections.Concurrent;
using System.Diagnostics;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElfTest.Contract;
using Google.Protobuf;
using log4net;

namespace PortkeyCryptoBoxContractScript;

public class TestContractService : IContractService
{
    public TestContractService(Service service, ILog logger, string testContract,
        List<string> fromAccountList, List<string> toAccountList)
    {
        _service = service;
        _nodeManager = service.NodeManager;
        _logger = logger;
        _fromAccountList = fromAccountList;
        _toAccountList = toAccountList;
        _testContract = testContract == ""
            ? new AElfTestContract(_nodeManager, service.CallAddress, "", false)
            : new AElfTestContract(_nodeManager, service.CallAddress, testContract);
    }

    public IContractService UpdateService(Service service)
    {
        _logger.Info($"Update url to {service.NodeManager.GetApiUrl()}");
        return new TestContractService(service, _logger, _testContract.ContractAddress,
            _fromAccountList, _toAccountList);
    }

    public List<string>? CreateToken(int group)
    {
        var txIds = new List<string>();
        var symbolList = new List<string>();
        _logger.Info($"Create {group} test token: ");
        for (var i = 0; i < group; i++)
        {
            var symbol = CommonHelper.RandomString(4, false);
            symbolList.Add(symbol);

            var tokenInfo = _testContract.GetTestTokenInfo(symbol);
            if (!tokenInfo.Equals(new TestTokenInfo())) continue;
            _logger.Info($"Create test symbol: {symbol}");
            var txId = _testContract.ExecuteMethodWithTxId(TestMethod.TestCreate,
                new TestCreateInput
                {
                    Symbol = symbol,
                    TotalSupply = 1000000000000000000
                });
            txIds.Add(txId);
        }

        _nodeManager.CheckTransactionListResult(txIds);
        return symbolList;
    }

    public void TransferForTest(List<string>? symbols)
    {
        foreach (var symbol in symbols)
        {
            var txIds = new ConcurrentQueue<string>();
            _logger.Info($"Initialize symbol {symbol} to testers ...");
            var amount = _testContract.GetTestTokenInfo(symbol).TotalSupply.Div(_fromAccountList.Count*2);

            Parallel.ForEach(_fromAccountList, account =>
            {
                var txId = _testContract.ExecuteMethodWithTxId(TestMethod.TestTransfer, new TestTransferInput
                {
                    Symbol = symbol,
                    Amount = amount,
                    To = Address.FromBase58(account)
                });
                txIds.Enqueue(txId);
                
                // var txIdToken = _testContract.ExecuteMethodWithTxId(TestMethod.Transfer, new TransferInput
                // {
                //     Symbol = symbol,
                //     Amount = amount,
                //     To = Address.FromBase58(account)
                // });
                // txIds.Enqueue(txIdToken);
            });

            Parallel.ForEach(txIds, txId => { _nodeManager.CheckTransactionResult(txId); });

            _logger.Info($"Check symbol balance: ");
            Parallel.ForEach(_fromAccountList, account =>
            {
                var testBalance = _testContract.GetTestBalance(symbol, account);
                _logger.Info($"{account} {symbol}: {testBalance.Amount}");
            });
        }
    }
    
    public List<string> SendMultiTransactions(string symbol, long size, List<string> tester,
        string method, out DateTime sendTime)
    {
        var rawTransactionList = new ConcurrentDictionary<string, ReferenceInfo>();
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var transactionList = new List<string>();
        Parallel.For(1, _toAccountList.Count + 1, item =>
        {
            var (from, to) = GetTransferPair(item - 1);
            var transferInput = new TestTransferInput
            {
                Symbol = symbol,
                To = to.ConvertAddress(),
                Amount = GenerateAmount(),
                Memo = GenerateMemo(size)
            };
            var requestInfo =
                _nodeManager.GenerateRawTransactionWithRefInfo(from, _testContract.ContractAddress,
                    method,
                    transferInput, out var referenceInfo);
            rawTransactionList[requestInfo] = referenceInfo;
        });

        stopwatch.Stop();
        var createTxsTime = stopwatch.ElapsedMilliseconds;

        //Send batch transaction requests
        stopwatch.Restart();
        var rawTransactions = string.Join(",", rawTransactionList.Keys.ToList());
        var referenceInfos = rawTransactionList.Values.ToList();
        var transactions = _nodeManager.SendTransactions(rawTransactions);
        stopwatch.Stop();
        transactionList.AddRange(transactions);
        var requestTxsTime = stopwatch.ElapsedMilliseconds;

        sendTime = DateTime.UtcNow;
        // for (var j = 0; j < transactions.Count; j++)
        // {
        //     _logger.Info($"{transactions[j]} send to txhub time: {sendTime} " +
        //                  $"referenceBlock: {referenceInfos[j].referenceBlockHeight}, " +
        //                  $"referenceHash: {referenceInfos[j].referenceBlockHash}");
        // }

        _logger.Info($"First {transactions.First()} send to txhub time: {sendTime} " +
                     $"referenceBlock: {referenceInfos.First().referenceBlockHeight}, " +
                     $"referenceHash: {referenceInfos.First().referenceBlockHash}");
        _logger.Info($"Last {transactions.Last()} send to txhub time: {sendTime} " +
                     $"referenceBlock: {referenceInfos.Last().referenceBlockHeight}, " +
                     $"referenceHash: {referenceInfos.Last().referenceBlockHash}");

        _logger.Info(
            $"Thread request transactions: " +
            $"{tester.Count}, create time: {createTxsTime}ms, request time: {requestTxsTime}ms.");


        _logger.Info($"Transaction Count: {transactionList.Count}");
        return transactionList;
    }

    public Task SendMultiTransactionsOneBytOneTasks(string symbol, long size, List<string> tester, string method)
    {
            var taskList = tester.Select(i =>
            {
                var (from, to) = GetTransferPair(i);

                return Task.Run(
                    () => SendOneTransaction(symbol, size, method, from, to));
            });
            Task.WhenAll(taskList);

        return Task.CompletedTask;
    }

    private void SendOneTransaction(string symbol, long size, string method, string from, string to)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        _logger.Info($"From user: {from}");
        var transferInput = new TestTransferInput
        {
            Symbol = symbol,
            To = to.ConvertAddress(),
            Amount = GenerateAmount(),
            Memo = GenerateMemo(size)
        };
        var requestInfo =
            _nodeManager.GenerateRawTransactionWithRefInfo(from, _testContract.ContractAddress,
                method,
                transferInput, out var referenceInfo);
        var transaction = _nodeManager.SendTransaction(requestInfo);
        stopwatch.Stop();
        var sendTime = DateTime.Now;
        var requestTxsTime = stopwatch.ElapsedMilliseconds;
        _logger.Info(
            $"{symbol} request one transactions: " +
            $"request time: {requestTxsTime}ms.");
        _logger.Info($"{transaction} send to txhub time: {sendTime} " +
                     $"referenceBlock: {referenceInfo.referenceBlockHeight}, " +
                     $"referenceHash: {referenceInfo.referenceBlockHash}");
    }

    public void InitializeContract()
    {
        _testContract.ExecuteMethodWithResult(TestMethod.Initialize, new InitializeInput
        {
            Owner = Address.FromBase58(_service.CallAddress)
        });
    }

    public List<string>? InitTestContractTest(int group, List<string> testAccounts)
    {
        var symbols = CreateToken(group);
        TransferForTest(symbols);
        return symbols;
    }
    
    public List<string> CreateProxyMethodTest(List<ProxyService.ProxyAccountInfo> proxyAccountInfos,
        int count, string contract, string method, IMessage input,
        out DateTime sendTime)
    {
        throw new NotImplementedException();
    }


    private ByteString GenerateMemo(long size)
    {
        var bytes = CommonHelper.GenerateRandombytes(size);
        return ByteString.CopyFrom(bytes);
    }

    private long GenerateAmount()
    {
        var amount = CommonHelper.GenerateRandomNumber(1, 10000);
        return amount;
    }

    private (string, string) GetTransferPair(int i)
    {
        return (_fromAccountList[i], _toAccountList[i]);
    }

    private (string, string) GetTransferPair(string i)
    {
        var j = _fromAccountList.IndexOf(i);
        return (i, _toAccountList[j]);
    }

    private Service _service;
    private INodeManager _nodeManager;
    private readonly ILog _logger;
    private readonly AElfTestContract _testContract;
    private List<string> _fromAccountList;
    private List<string> _toAccountList;
}