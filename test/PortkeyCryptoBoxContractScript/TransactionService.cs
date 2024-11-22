using System.Collections.Concurrent;
using System.Diagnostics;
using AElf.CSharp.Core;
using log4net;
using Volo.Abp.Threading;

namespace PortkeyCryptoBoxContractScript;

public class TransactionService
{
    public TransactionService(ILog logger,
        IContractService contractService,
        int testDuration,
        long size,
        List<string> testers,
        List<Service> service,
        List<string>? symbols = null)
    {
        _logger = logger;
        _service = service.First();
        _services = service;
        _monitorService = new MonitorService(_service.NodeManager, logger);
        _contractService = contractService;
        _testAccounts = testers;
        _testDuration = testDuration;
        _size = size;
        _symbols = symbols;
        QueueTransaction = new ConcurrentDictionary<DateTime, List<string>>();
    }

    public void ExecuteContinuousRoundsTransactionsWithProxyTask(
        List<ProxyService.ProxyAccountInfo> proxyAccountInfos)
    {
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        var txCts = new CancellationTokenSource();
        var txToken = txCts.Token;
        txCts.CancelAfter(_testDuration * 1000);

        var checkTime = 10;
        var taskList = new List<Task>
        {
            Task.Run(() => GeneratedTransaction(proxyAccountInfos, txCts, txToken), txToken),
            Task.Run(() =>
            {
                CheckTransactionResult(checkTime, token);
            }, token),
            Task.Run(() =>
            {
                for (var i = 1; i > 0; i++)
                {
                    var isContinuePendingTransactions = _monitorService.CheckPendingTransactions();
                    if (checkTime != 0 || isContinuePendingTransactions) continue;
                    _logger.Info("The pending queue is empty ...");
                    cts.Cancel();
                    break;
                }
            }, token)
        };

        Task.WaitAll(taskList.ToArray<Task>());
        _logger.Info("END");
    }
    
    public void ExecuteTransactionsWithoutResultTask(string method)
    {
        var txCts = new CancellationTokenSource();
        var txToken = txCts.Token;
        txCts.CancelAfter(_testDuration * 1000);

        var taskList = _services.Select(i =>
        {
            return Task.Run(
                () => GeneratedTransaction(txCts, txToken, _contractService.UpdateService(i), method,false), txToken);
        });

        Task.WaitAll(taskList.ToArray());
        _logger.Info("END");
    }
    
    public void ExecuteCurrentRoundsTransactionsWithoutResultTaskOneByOne()
    {
        var txCts = new CancellationTokenSource();
        var txToken = txCts.Token;
        txCts.CancelAfter(_testDuration * 1000);

        var taskList = new List<Task>
        {
            Task.Run(() => GeneratedTransactionWithoutResultOneByOne(_contractService,  txCts, txToken), txToken)
        };
        Task.WaitAll(taskList.ToArray());

        _logger.Info("END");
    }

    private void GeneratedTransaction(List<ProxyService.ProxyAccountInfo> proxyAccountInfos,
        CancellationTokenSource cts, CancellationToken token)
    {
        _logger.Info("Begin generate multi requests.");
        try
        {
            for (var r = 1; r > 0; r++) //continuous running
            {
                if (token.IsCancellationRequested)
                {
                    var endTIme = DateTime.UtcNow;
                    _logger.Info($"End execution transaction request round, total round:{r - 1}, end time: {endTIme}");
                    break;
                }

                var stopwatch = Stopwatch.StartNew();
                try
                {
                    //multi task for SendTransactions query
                        var r1 = r;
                        Task.Run(() =>
                        {
                            var list =  ExecuteBatchTransactionTask(proxyAccountInfos, r1, out var dateTime);
                            if (list != new List<string>())
                                QueueTransaction.TryAdd(dateTime, list);
                        }, token);

                    Thread.Sleep(500);
                }
                catch (AggregateException exception)
                {
                    _logger.Error($"Request to {_service.NodeManager.GetApiUrl()} got exception, {exception}");
                }
                catch (Exception e)
                {
                    var message = "Execute continuous transaction got exception." +
                                  $"\r\nMessage: {e.Message}" +
                                  $"\r\nStackTrace: {e.StackTrace}";
                    _logger.Error(message);
                }

                stopwatch.Stop();
                // _monitorService.TransactionSentPerSecond(_groupCount * proxyAccountInfos.Count, r,
                // stopwatch.ElapsedMilliseconds);
            }
        }
        catch (Exception e)
        {
            _logger.Error(e.Message);
            _logger.Error("Cancel all tasks due to transaction execution exception.");
            cts.Cancel(); //cancel all tasks
        }
    }

    private void GeneratedTransaction(CancellationTokenSource cts,
        CancellationToken token, IContractService contractService,string method, bool isNeedResult)
    {
        _logger.Info("Begin generate multi requests.");
        try
        {
            for (var r = 1;  r > 0; r++) //continuous running
            {
                if (token.IsCancellationRequested)
                {
                    var endTIme = DateTime.UtcNow;
                    _logger.Info($"End execution transaction request round, total round:{r - 1}, end time: {endTIme}");
                    break;
                }

                var roundTransaction = new List<string>();
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    _logger.Info($"Execution transaction request round: {r}");
                    //multi task for SendTransactions query
                    for (var i = 0; i < _symbols.Count; i++)
                    {
                        var i1 = i;
                        var r1 = r;
                        Task.Run(() =>
                        {
                            var list = ExecuteBatchTransactionTask(contractService, _symbols[i1], r1, method, isNeedResult, out var dateTime);
                            if (!isNeedResult) return;
                            if (list == new List<string>()) return;
                            QueueTransaction.TryAdd(dateTime, list);
                            roundTransaction.AddRange(list);

                        }, token);
                    }

                    Thread.Sleep(500);
                }
                catch (AggregateException exception)
                {
                    _logger.Error($"Request to {_service.NodeManager.GetApiUrl()} got exception, {exception}");
                }
                catch (Exception e)
                {
                    var message = "Execute continuous transaction got exception." +
                                  $"\r\nMessage: {e.Message}" +
                                  $"\r\nStackTrace: {e.StackTrace}";
                    _logger.Error(message);
                }

                stopwatch.Stop();
                if (roundTransaction.Count > 0)
                {
                    _monitorService.TransactionSentPerSecond(roundTransaction.Count, r,
                        stopwatch.ElapsedMilliseconds);
                }
            }
        }
        catch (Exception e)
        {
            _logger.Error(e.Message);
            _logger.Error("Cancel all tasks due to transaction execution exception.");
            cts.Cancel(); //cancel all tasks
        }
    }

    private async Task GeneratedTransactionWithoutResultOneByOne(IContractService testContractService, 
      CancellationTokenSource cts, CancellationToken token)
    {
        _logger.Info($"Begin generate multi requests.");
        try
        {
            for (var r = 1; r > 0; r++) //continuous running
            {
                if (token.IsCancellationRequested)
                {
                    var endTIme = DateTime.UtcNow;
                    _logger.Info($"End execution transaction request round, total round:{r - 1}, end time: {endTIme}");
                    break;
                }

                var stopwatch = Stopwatch.StartNew();
                try
                {
                    _logger.Info($"Execution transaction request round: {r}");
                    //multi task for SendTransactions query

                    var taskList = _symbols.Select(i =>
                    {
                        var r1 = r;
                        _logger.Info($"Execution transaction request round: {i}");

                        return Task.Run(
                            () =>  ExecuteBatchTransactionWithoutCheckOneByOneTask(testContractService, i, _size, r1), token);
                    });
                    await Task.WhenAll(taskList);
                    await Task.Delay(500, token);
                }
                catch (AggregateException exception)
                {
                    _logger.Error($"Request to {_service.NodeManager.GetApiUrl()} got exception, {exception}");
                }
                catch (Exception e)
                {
                    var message = "Execute continuous transaction got exception." +
                                  $"\r\nMessage: {e.Message}" +
                                  $"\r\nStackTrace: {e.StackTrace}";
                    _logger.Error(message);
                }

                stopwatch.Stop();
            }
        }
        catch (Exception e)
        {
            _logger.Error(e.Message);
            _logger.Error("Cancel all tasks due to transaction execution exception.");
            cts.Cancel(); //cancel all tasks
        }
    }


    private List<string> ExecuteBatchTransactionTask(
        List<ProxyService.ProxyAccountInfo> proxyAccountInfos, int round,  out DateTime sendTime)
    {
        // var result = _monitorService.CheckTransactionPoolStatus(true);
        var transactionWithDate = new List<string>();
        // if (!result)
        // {
        //     _logger.Warn("Transaction pool transactions over limited, canceled this round execution.");
        //     sendTime = DateTime.Now;
        //     return transactionWithDate;
        // }
        //
        // var transactionList =
        //     _contractService.CreateProxyMethodTest(proxyAccountInfos, count, out var dateTime);
        // if (round == 1 && count == 0)
        // {
        //     var blockHeight = AsyncHelper.RunSync(() => _service.NodeManager.ApiClient.GetBlockHeightAsync());
        //     _logger.Info(
        //         $"Start execution transaction request round, start time: {dateTime}, blockHeight: {blockHeight}");
        // }
        //
        // transactionWithDate = transactionList;
        // sendTime = dateTime;
        sendTime = default;
        return transactionWithDate;
    }

    private List<string> ExecuteBatchTransactionTask(IContractService contractService, string symbol,
        int round, string method, bool isNeedResult, out DateTime sendTime)
    {
        _logger.Info(
            $"=============== Start ==========");
        var result = _monitorService.CheckTransactionPoolStatus(true);
        var transactionWithDate = new List<string>();
        if (!result)
        {
            _logger.Warn("Transaction pool transactions over limited, canceled this round execution.");
            sendTime = DateTime.Now;
            return transactionWithDate;
        }

        DateTime dateTime;

        if (isNeedResult)
        {
            var transactionList =
                contractService.SendMultiTransactions(symbol, _size, _testAccounts, method,
                    out dateTime);
            
            transactionWithDate = transactionList;
            sendTime = dateTime;
        }
        else
        {
            contractService.SendMultiTransactions(symbol, _size, _testAccounts, method, out dateTime);
            sendTime = dateTime;
        }

        if (round != 1)
        {
            sendTime = default;
            return transactionWithDate;
        }

        var blockHeight = AsyncHelper.RunSync(() => _service.NodeManager.ApiClient.GetBlockHeightAsync());
        _logger.Info(
            $"Start execution transaction request round, start time: {dateTime}, blockHeight: {blockHeight}");

        return transactionWithDate;
    }

    private async Task ExecuteBatchTransactionWithoutCheckOneByOneTask(IContractService testContractService,
        string symbol,
        long size, int round)
    {
        _logger.Info(
            $"=============== Start ==========");
        var result = _monitorService.CheckTransactionPoolStatus(true);
        if (!result)
        {
            _logger.Warn("Transaction pool transactions over limited, canceled this round execution.");
        }
        
        await testContractService.SendMultiTransactionsOneBytOneTasks( symbol, size, _testAccounts, "TestTransfer");
        if (round != 1) return;
        var blockHeight = AsyncHelper.RunSync(() => _service.NodeManager.ApiClient.GetBlockHeightAsync());
        _logger.Info(
            $"Start execution transaction request round, start blockHeight: {blockHeight}");
    }

    private void CheckTransactionResult(int checkTime, CancellationToken token)
    {
        if (checkTime < 0) throw new ArgumentOutOfRangeException(nameof(checkTime));
        while (QueueTransaction.Count == 0)
        {
            _logger.Info("Waiting for transactions ... ");
            Thread.Sleep(5000);
        }

        for (var i = 1; i > 0; i++)
        {
            var transactionsList = new ConcurrentDictionary<DateTime, List<string>>();
            checkTime = 20;
            while (transactionsList.Count == 0)
            {
                if (checkTime == 0)
                {
                    _logger.Info("The queue is empty ...");
                    break;
                }

                Thread.Sleep(1000);
                transactionsList = GetTransactions();
                checkTime--;
            }

            if (transactionsList.Count == 0)
            {
                break;
            }

            _logger.Info($"Check transaction result {i}:");
            var txsTasks = transactionsList
                .Select(transactions =>
                    Task.Run(() => _monitorService.CheckTransactions(transactions.Value, transactions.Key),
                        token)).ToList();
            Task.WaitAll(txsTasks.ToArray<Task>());
        }
    }
    
    private ConcurrentDictionary<DateTime, List<string>> GetTransactions()
    {
        var transactionLists = new ConcurrentDictionary<DateTime, List<string>>();
        if (QueueTransaction.Count == 0)
            return transactionLists;
        var keyList = QueueTransaction.Count >= 5
            ? QueueTransaction.Keys.Take(20).ToList()
            : QueueTransaction.Keys.ToList();

        Parallel.ForEach(keyList, item =>
        {
            if (QueueTransaction.TryRemove(item, out var txs))
            {
                transactionLists[item] = txs;
            }
        });
        return transactionLists;
    }

    private readonly ILog _logger;
    private ConcurrentDictionary<DateTime, List<string>> QueueTransaction { get; set; }

    private readonly long _size;
    private readonly int _testDuration;
    private readonly Service _service;
    private readonly List<Service> _services;
    private readonly MonitorService _monitorService;
    private readonly IContractService _contractService;
    private readonly List<string>? _symbols;
    private readonly List<string> _testAccounts;
}