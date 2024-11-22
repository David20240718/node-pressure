using System.Collections.Concurrent;
using System.Diagnostics;
using AElf;
using AElf.Cryptography;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElfTest.Contract;
using Google.Protobuf;
using log4net;
using Portkey.Contracts.CryptoBox;
using InitializeInput = Portkey.Contracts.CryptoBox.InitializeInput;

namespace PortkeyCryptoBoxContractScript;

public class TestCryptoBoxContractService : IContractService
{
    public TestCryptoBoxContractService(Service service, ILog logger, string testContract,
        List<string> fromAccountList, string defaultAddress,List<string> toAccountList,AElfKeyStore keyStore,int boxCount,int transferCryptoCount)
    {
        _service = service;
        _nodeManager = service.NodeManager;
        _logger = logger;
        _fromAccountList = fromAccountList;
        _toAccountList = toAccountList;
        _testCryptoBoxContract = testContract == ""
            ? new CryptoBoxContract(_nodeManager, service.CallAddress, "", false)
            : new CryptoBoxContract(_nodeManager, service.CallAddress, testContract);
        _keyStore = keyStore;
        _defaultAddress = defaultAddress;
        _boxCount = boxCount;
        _transferCryptoCount = transferCryptoCount;
        _boxIdList = new List<string>();
    }

    public IContractService UpdateService(Service service)
    {
        _logger.Info($"Update url to {service.NodeManager.GetApiUrl()}");
        return new TestContractService(service, _logger, _testCryptoBoxContract.ContractAddress,
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
    
            var tokenInfo = _testCryptoBoxContract.GetTestTokenInfo(symbol);
            if (!tokenInfo.Equals(new TestTokenInfo())) continue;
            _logger.Info($"Create test symbol: {symbol}");
            var txId = _testCryptoBoxContract.ExecuteMethodWithTxId(TestMethod.TestCreate,
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

    // public void TransferForTest(List<string>? symbols)
    // {
    //     foreach (var symbol in symbols)
    //     {
    //         var txIds = new ConcurrentQueue<string>();
    //         _logger.Info($"Initialize symbol {symbol} to testers ...");
    //         var amount = _testCryptoBoxContract.GetTestTokenInfo(symbol).TotalSupply.Div(_fromAccountList.Count);
    //
    //         Parallel.ForEach(_fromAccountList, account =>
    //         {
    //             var txId = _testCryptoBoxContract.ExecuteMethodWithTxId(TestMethod.TestTransfer, new TestTransferInput
    //             {
    //                 Symbol = symbol,
    //                 Amount = amount,
    //                 To = Address.FromBase58(account)
    //             });
    //             txIds.Enqueue(txId);
    //         });
    //
    //         Parallel.ForEach(txIds, txId => { _nodeManager.CheckTransactionResult(txId); });
    //
    //         _logger.Info($"Check symbol balance: ");
    //         Parallel.ForEach(_fromAccountList, account =>
    //         {
    //             var testBalance = _testCryptoBoxContract.GetTestBalance(symbol, account);
    //             _logger.Info($"{account} {symbol}: {testBalance.Amount}");
    //         });
    //     }
    // }
    
    public List<string> SendMultiTransactions(string symbol, long size, List<string> tester,
        string method, out DateTime sendTime)
    {
        var rawTransactionList = new ConcurrentDictionary<string, ReferenceInfo>();
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var transactionList = new List<string>();

        var createTxsTime = stopwatch.ElapsedMilliseconds;
    
        //Send batch transaction requests
        stopwatch.Restart();
        for (int i = 0; i < _boxIdList.Count; i++)
        {
            string id = _boxIdList[i];
            SendTransferCryptoBoxesSendOneTransaction(id,symbol,size,method);

        }
        stopwatch.Stop();
       
        var requestTxsTime = stopwatch.ElapsedMilliseconds;
    
        sendTime = DateTime.UtcNow;
        // for (var j = 0; j < transactions.Count; j++)
        // {
        //     _logger.Info($"{transactions[j]} send to txhub time: {sendTime} " +
        //                  $"referenceBlock: {referenceInfos[j].referenceBlockHeight}, " +
        //                  $"referenceHash: {referenceInfos[j].referenceBlockHash}");
        // }
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
                    () => SendTransferCryptoBoxesSendOneTransaction("1",symbol, size, method));
            });
            Task.WhenAll(taskList);

        return Task.CompletedTask;
    }

    private void SendTransferCryptoBoxesSendOneTransaction(string id ,string symbol, long size, string method)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        
        var kp = _keyStore.GetAccountKeyPair(_defaultAddress);
        var privateKey = kp.PrivateKey.ToHex();
        

        var signatureStr =
            $"{id}-{_defaultAddress.ConvertAddress()}-{10}";

        var byteArray = HashHelper.ComputeFrom(signatureStr).ToByteArray();
        var receiveSignature =
            CryptoHelper.SignWithPrivateKey(ByteArrayHelper.HexStringToByteArray(privateKey), byteArray)
                .ToHex();

        var list = new List<TransferCryptoBoxInput>();
        

        for (int i = 0; i < _transferCryptoCount; i++)
        {
            
            var (from, to) = GetTransferPair(i);

            
            list.Add( new TransferCryptoBoxInput
            {
                Amount = 10,
                Receiver = to.ConvertAddress(),
                CryptoBoxSignature = receiveSignature
            });
        }
        
        
        var batchInput = new TransferCryptoBoxesInput
        {
            CryptoBoxId = id,
            TransferCryptoBoxInputs = { list }
        };
        
        var requestInfo =
            _nodeManager.GenerateRawTransactionWithRefInfo(_defaultAddress, _testCryptoBoxContract.ContractAddress,
                method,
                batchInput, out var referenceInfo);
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

        _testCryptoBoxContract.ExecuteMethodWithResult(TestMethod.Initialize, new InitializeInput
        {
            // Admin = DefaultAddress,
            Admin = _defaultAddress.ConvertAddress(),
            MaxCount = 1000
        });

    }
    
    public String CreateCryptoBox(string symbol)
    {
        
        var id = Guid.NewGuid().ToString().Replace("-", "");

        var timeSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds() * 1000;
        
        var kp = _keyStore.GetAccountKeyPair(_defaultAddress);


        _testCryptoBoxContract.ExecuteMethodWithResult(TestMethod.CreateCryptoBox, new CreateCryptoBoxInput
        {
            CryptoBoxSymbol = symbol,
            TotalAmount = 1000,
            TotalCount = 10,
            MinAmount = 10,
            Sender = _defaultAddress.ConvertAddress(),
            PublicKey = kp.PublicKey.ToHex(),
            CryptoBoxType = CryptoBoxType.QuickTransfer,
            ExpirationTime = timeSeconds + 1000,
            CryptoBoxId = id
        });

        return id;
    }

    public void InitTestContractTest(List<String> symbolList)
    {
        foreach (var symbol in symbolList)
        {
            for (int i = 0; i < _boxCount; i++)
            {
                string id = CreateCryptoBox(symbol);
                _boxIdList.Add(id);
            }
        }
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
    private readonly CryptoBoxContract _testCryptoBoxContract;
    private List<string> _fromAccountList;
    private List<string> _toAccountList;
    private readonly AElfKeyStore _keyStore;
    
    private String _defaultAddress;
    
    private List<string> _boxIdList;

    
    private int _boxCount;
    private int _transferCryptoCount;


}