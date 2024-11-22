using System.Collections.Concurrent;
using AElf.Client;
using AElf.Client.Dto;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Volo.Abp.Threading;

namespace AElfTestContractScript;

public class MonitorService
{
    public MonitorService(INodeManager nodeManager, ILog logger)
    {
        _nodeManager = nodeManager;
        _logger = logger;
        MaxValidateLimit = ConfigInfo.ReadInformation.TransactionInfo.SentTxLimit;
        _pendingTransaction = new ConcurrentQueue<string>();
    }

    public bool CheckTransactionPoolStatus(bool enable)
    {
        if (!enable) return true;
        var checkTimes = 0;
        while (true)
        {
            if (checkTimes >= 20) return false; //over check time and cancel current round execution            
            var poolStatus = GetTransactionPoolTxCount();
            if (poolStatus.Equals(new TransactionPoolStatusOutput()))
                return true;
            if (poolStatus.Validated < MaxValidateLimit && poolStatus.Queued < MaxValidateLimit)
                return true;

            checkTimes++;
            if (checkTimes % 10 == 0)
                $"TxHub transaction count: QueuedCount={poolStatus.Queued} ValidatedCount={poolStatus.Validated}. Transaction limit: {MaxValidateLimit}"
                    .WriteWarningLine();
            Thread.Sleep(1000);
        }
    }

    public void CheckNodeHeightStatus(bool enable = true)
    {
        if (!enable) return;

        var checkTimes = 0;
        while (true)
        {
            long currentHeight = 0;
            try
            {
                currentHeight = AsyncHelper.RunSync(_nodeManager.ApiClient.GetBlockHeightAsync);
            }
            catch (AElfClientException e)
            {
                _logger.Error("GetBlockHeightAsync error ...");
                _logger.Error(e);
            }

            if (BlockHeight != currentHeight && currentHeight != 0)
            {
                BlockHeight = currentHeight;
                return;
            }

            checkTimes++;
            Thread.Sleep(100);
            if (checkTimes % 10 == 0)
                Console.Write(
                    $"\rCurrent block height {currentHeight}, not changed in {checkTimes / 10: 000} seconds.");

            if (checkTimes != 3000) continue;

            Console.Write("\r\n");
            throw new TimeoutException("Node block exception, block height not changed 5 minutes later.");
        }
    }

    private TransactionPoolStatusOutput GetTransactionPoolTxCount()
    {
        try
        {
            var transactionPoolStatusOutput =
                AsyncHelper.RunSync(_nodeManager.ApiClient.GetTransactionPoolStatusAsync);

            return transactionPoolStatusOutput;
        }
        catch (AElfClientException e)
        {
            _logger.Error("GetTransactionPoolStatusAsync error ...");
            _logger.Error(e);
            return new TransactionPoolStatusOutput();
        }
    }

    public void CheckTransactions(List<string> transactions, DateTime sendTime)
    {
        if (transactions.Count < 1) return;
        _logger.Info($"Check transaction, the first transaction: {transactions.First()}, send time: {sendTime}");
        var txIds = new Dictionary<string, RefBlockInfo>();
        long blockHeight = 0;
        DateTime minedTime = default;

        var groupSize = 10;

        var groupedTransaction = transactions
            .Select((transaction, index) => new { transaction, index })
            .GroupBy(item => item.index / groupSize, g => g.transaction)
            .Select(group => group.ToList())
            .ToList();
        try
        {
            foreach (var transaction in groupedTransaction)
            {
                Parallel.ForEach(transaction, item =>
                {
                    var transactionResult =
                        AsyncHelper.RunSync(() => _nodeManager.ApiClient.GetTransactionResultAsync(item));
                    var status = transactionResult.Status.ConvertTransactionResultStatus();
                    switch (status)
                    {
                        case TransactionResultStatus.Mined:
                            minedTime = blockHeight == transactionResult.BlockNumber
                                ? minedTime
                                : (AsyncHelper.RunSync(() =>
                                    _nodeManager.ApiClient.GetBlockByHeightAsync(transactionResult.BlockNumber))).Header
                                .Time;

                            _logger.Info($"Check transaction, the transaction: {item}, mined time: {minedTime}");
                            break;
                        case TransactionResultStatus.Failed:
                            minedTime = blockHeight == transactionResult.BlockNumber
                                ? minedTime
                                : (AsyncHelper.RunSync(() =>
                                    _nodeManager.ApiClient.GetBlockByHeightAsync(transactionResult.BlockNumber))).Header
                                .Time;

                            _logger.Info($"Check failed transaction, the transaction: {item}, mined time: {minedTime}");
                            break;
                        case TransactionResultStatus.Pending:
                            _pendingTransaction.Enqueue(item);
                            _logger.Info($"Transaction status is pending, check again later ... ");
                            break;
                        case TransactionResultStatus.NodeValidationFailed:
                            _logger.Error($"{transactionResult.Error}");
                            break;
                        case TransactionResultStatus.NotExisted:
                            _logger.Info($"Transaction status is NotExisted, txId: {item} ... ");
                            if (transactionResult.Transaction != null)
                            {
                                txIds[item] = new RefBlockInfo(transactionResult.Transaction.RefBlockNumber,
                                    transactionResult.Transaction.RefBlockPrefix);
                            }
                            break;
                    }
                });
            }
        }
        catch (Exception e)
        {
            var message = "Execute check transaction got exception." +
                          $"\r\nMessage: {e.Message}" +
                          $"\r\nStackTrace: {e.StackTrace}";
            _logger.Error(message);
        }

        if (txIds.Count < 1) return;
        foreach (var transaction in txIds)
        {
            _logger.Warn($"NotExisted transaction : {transaction.Key}\n" +
                         $"RefBlockNumber: {transaction.Value.RefBlockHeight}\n" +
                         $"RefBlockPrefix: {transaction.Value.RefBlockPreFix}");
        }
    }

    public void CheckTransactions(List<string> transactions)
    {
        if (transactions.Count < 1) return;
        var txIds = new Dictionary<string, RefBlockInfo>();
        long blockHeight = 0;
        DateTime minedTime = default;
        Parallel.For(0, transactions.Count, item =>
        {
            var transactionResult =
                AsyncHelper.RunSync(() => _nodeManager.ApiClient.GetTransactionResultAsync(transactions[item]));
            var status = transactionResult.Status.ConvertTransactionResultStatus();
            switch (status)
            {
                case TransactionResultStatus.Mined:
                    minedTime = blockHeight == transactionResult.BlockNumber
                        ? minedTime
                        : (AsyncHelper.RunSync(() =>
                            _nodeManager.ApiClient.GetBlockByHeightAsync(transactionResult.BlockNumber))).Header.Time;

                    _logger.Info($"Check transaction, the transaction: {transactions[item]}, mined time: {minedTime}");
                    break;
                case TransactionResultStatus.Failed:
                    minedTime = blockHeight == transactionResult.BlockNumber
                        ? minedTime
                        : (AsyncHelper.RunSync(() =>
                            _nodeManager.ApiClient.GetBlockByHeightAsync(transactionResult.BlockNumber))).Header.Time;

                    _logger.Info(
                        $"Check failed transaction, the transaction: {transactions[item]}, mined time: {minedTime}");
                    break;
                case TransactionResultStatus.Pending:
                    _pendingTransaction.Enqueue(transactions[item]);
                    _logger.Info($"Transaction status is pending, check again later ... ");
                    break;
                case TransactionResultStatus.NodeValidationFailed:
                    _logger.Error($"{transactionResult.Error}");
                    break;
                case TransactionResultStatus.NotExisted:
                    _logger.Info($"Transaction status is NotExisted, txId: {transactions[item]} ... ");
                    if (transactionResult.Transaction != null)
                    {
                        txIds[transactions[item]] = new RefBlockInfo(transactionResult.Transaction.RefBlockNumber,
                            transactionResult.Transaction.RefBlockPrefix);
                    }

                    break;
            }
        });

        if (txIds.Count < 1) return;
        foreach (var transaction in txIds)
        {
            _logger.Warn($"NotExisted transaction : {transaction.Key}\n" +
                         $"RefBlockNumber: {transaction.Value.RefBlockHeight}\n" +
                         $"RefBlockPrefix: {transaction.Value.RefBlockPreFix}");
        }
    }

    public bool CheckPendingTransactions()
    {
        var transactions = GetTransaction();
        if (transactions == new List<string>())
            return false;
        Parallel.For(0, transactions.Count, item =>
        {
            _logger.Info($"Check pending transaction, txId: {transactions[item]}");
            long blockHeight = 0;
            DateTime minedTime = default;
            var transactionResult =
                AsyncHelper.RunSync(() => _nodeManager.ApiClient.GetTransactionResultAsync(transactions[item]));
            var status = transactionResult.Status.ConvertTransactionResultStatus();
            switch (status)
            {
                case TransactionResultStatus.Mined:
                    minedTime = blockHeight == transactionResult.BlockNumber
                        ? minedTime
                        : (AsyncHelper.RunSync(() =>
                            _nodeManager.ApiClient.GetBlockByHeightAsync(transactionResult.BlockNumber))).Header.Time;

                    _logger.Info($"Check transaction, the transaction: {transactions[item]}, mined time: {minedTime}");
                    break;
                case TransactionResultStatus.Failed:
                    minedTime = blockHeight == transactionResult.BlockNumber
                        ? minedTime
                        : (AsyncHelper.RunSync(() =>
                            _nodeManager.ApiClient.GetBlockByHeightAsync(transactionResult.BlockNumber))).Header.Time;

                    _logger.Info(
                        $"Check failed transaction, the transaction: {transactions[item]}, mined time: {minedTime}");
                    break;
                case TransactionResultStatus.Pending:
                    _pendingTransaction.Enqueue(transactions[item]);
                    break;
                case TransactionResultStatus.NodeValidationFailed:
                    _logger.Error($"{transactionResult.Error}");
                    break;
                case TransactionResultStatus.NotExisted:
                {
                    if (transactionResult.Transaction != null)
                    {
                        _logger.Warn($"NotExisted transaction : {transactions[item]}\n" +
                                     $"RefBlockNumber: {transactionResult.Transaction.RefBlockNumber}\n" +
                                     $"RefBlockPrefix: {transactionResult.Transaction.RefBlockPrefix}");
                    }

                    break;
                }
            }
        });
        return true;
    }

    public void CheckTransaction(string transaction, DateTime sendTime)
    {
        _logger.Info($"Check transaction {transaction}, send time: {sendTime}");
        var txIds = new Dictionary<string, RefBlockInfo>();
        var transactionResult =
            AsyncHelper.RunSync(() => _nodeManager.ApiClient.GetTransactionResultAsync(transaction));
        var status = transactionResult.Status.ConvertTransactionResultStatus();
        if (!status.Equals(TransactionResultStatus.NotExisted))
        {
            var minedTime =
                (AsyncHelper.RunSync(() => _nodeManager.ApiClient.GetBlockByHeightAsync(transactionResult.BlockNumber)))
                .Header.Time;

            _logger.Info($"Check transaction, the transaction: {transaction}, mined time: {minedTime}");
        }
        else
        {
            txIds[transaction] = transactionResult.Transaction.Equals(null)
                ? new RefBlockInfo(0, "")
                : new RefBlockInfo(transactionResult.Transaction.RefBlockNumber,
                    transactionResult.Transaction.RefBlockPrefix);
            _logger.Warn($"NotExisted transaction : {transaction}\n" +
                         $"RefBlockNumber: {txIds[transaction].RefBlockHeight}\n" +
                         $"RefBlockPrefix: {txIds[transaction].RefBlockPreFix}");
        }
    }

    public void TransactionSentPerSecond(long transactionCount, int round, long milliseconds)
    {
        var tx = (float)transactionCount;
        var time = (float)milliseconds;

        var result = tx * 1000 / time;

        _logger.Info(
            $"Summary analyze: round {round} request {transactionCount} transactions in {time / 1000:0.000} seconds, average {result:0.00} txs/second. Total request {transactionCount * round} ");
    }

    private List<string> GetTransaction()
    {
        var transactionList = new List<string>();
        var count = _pendingTransaction.Count > 20 ? 20 : _pendingTransaction.Count;

        for (var i = 0; i < count; i++)
        {
            var tryDequeue = _pendingTransaction.TryDequeue(out var transaction);
            if (_pendingTransaction.Count == 0 && !tryDequeue)
                return new List<string>();
            transactionList.Add(transaction);
        }


        return transactionList;
    }

    private static int MaxValidateLimit { get; set; }
    private readonly INodeManager _nodeManager;
    private readonly ILog _logger;
    private long BlockHeight { get; set; } = 1;
    private ConcurrentQueue<string> _pendingTransaction;
}

public class RefBlockInfo
{
    public RefBlockInfo(long refBlockHeight, string refBlockPreFix)
    {
        RefBlockHeight = refBlockHeight;
        RefBlockPreFix = refBlockPreFix;
    }

    public long RefBlockHeight;
    public string RefBlockPreFix;
}