using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using EBridge.Contracts.Bridge;
using EBridge.Contracts.Report;
using Google.Protobuf;
using log4net;

namespace CreateReceiptScript;

public class BridgeService
{
    public BridgeService(Service service, List<string> accountList, ILog logger, string bridgeAddress)
    {
        _service = service;
        _logger = logger;
        _accountList = accountList;
        _bridge = new BridgeContract(service.NodeManager, service.CallAddress, bridgeAddress);
    }

    public void CreateReceipt(string symbol, string targetChainId, string targetAddress, int count)
    {
        var txIds = new List<string>();
        var round = count / _accountList.Count;
        for (var i = 0; i < round; i++)
        {
            Thread.Sleep(60000 / round);
            foreach (var account in _accountList)
            {
                _bridge.SetAccount(account);
                var amount = CommonHelper.GenerateRandomNumber(1, 1000);
                var txId = _bridge.ExecuteMethodWithTxId(BridgeMethod.CreateReceipt,
                    new CreateReceiptInput
                    {
                        Symbol = symbol,
                        Amount = amount,
                        TargetAddress = targetAddress,
                        TargetChainId = targetChainId
                    });
                txIds.Add(txId);
            }
        }

        _service.NodeManager.CheckTransactionListResult(txIds);
        var id = GetRoundId(txIds.Last());
        _logger.Info($"Create Receipt: {txIds.Last()} \n" +
                     $"roundId: {id}");
    }


    public void ApproveAccount(string symbol)
    {
        var input = new ApproveInput
        {
            Spender = _bridge.Contract,
            Symbol = symbol,
            Amount = long.MaxValue,
        };

        var txIds = new List<string>();
        foreach (var acc in _accountList)
        {
            _service.TokenService.SetAccount(acc);
            var approveTx = _service.TokenService.ExecuteMethodWithTxId(TokenMethod.Approve, input);
            txIds.Add(approveTx);
        }

        _service.TokenService.NodeManager.CheckTransactionListResult(txIds);
    }

    private long GetRoundId(string transactionId)
    {
        var result = _service.NodeManager.CheckTransactionResult(transactionId);
        var logs = result.Logs.First(l => l.Name.Equals("ReportProposed"));
        var reportInfo = ReportProposed.Parser.ParseFrom(ByteString.FromBase64(logs.NonIndexed));
        return reportInfo.RoundId;
    }


    private Service _service;
    private ILog _logger;
    private List<string> _accountList;
    private BridgeContract _bridge;
    private new Queue<List<Dictionary<string, List<string>>>> QueueTransaction { get; set; }
}