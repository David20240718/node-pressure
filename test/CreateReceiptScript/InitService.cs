using AElf;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;
using Google.Protobuf;
using log4net;
using Shouldly;

namespace CreateReceiptScript;

public class InitService
{
    public InitService(Service service, ILog logger, bool isSideChain = false, string mainChainUrl = "")
    {
        _service = service;
        _logger = logger;
        _nodeManager = service.NodeManager;
        if (!isSideChain) return;
        var mainChainManager = new NodeManager(mainChainUrl);
        _crossChainManager = new CrossChainManager(mainChainManager, _nodeManager);
    }

    public List<string> GetTestAccounts(int count)
    {
        var accountList = new List<string>();
        var authority = new AuthorityManager(_nodeManager, _service.CallAddress);
        var miners = authority.GetCurrentMiners();
        var accounts = _nodeManager.ListAccounts();
        var testUsers = accounts.FindAll(o => !miners.Contains(o));
        if (testUsers.Count >= count)
        {
            foreach (var acc in testUsers.Take(count)) accountList.Add(acc);
        }
        else
        {
            foreach (var acc in testUsers) accountList.Add(acc);

            var generateCount = count - testUsers.Count;
            for (var i = 0; i < generateCount; i++)
            {
                var account = _nodeManager.NewAccount();
                accountList.Add(account);
            }
        }

        foreach (var acc in accountList)
        {
            var result = _nodeManager.UnlockAccount(acc);
            if (!result)
                throw new Exception($"Account unlock {acc} failed.");
        }

        return accountList;
    }

    public void IssueBalanceToInitAccount(string symbol)
    {
        var initAccount = _service.CallAddress;
        var token = _service.TokenService;
        var balance = token.GetUserBalance(initAccount, symbol);
        if (balance > 50000_00000000) return;
        var symbolIssuer = token.GetTokenInfo(symbol).Issuer;
        token.SetAccount(symbolIssuer.ToBase58());
        token.IssueBalance(symbolIssuer.ToBase58(), initAccount, 50000_00000000, symbol);
    }

    public void CrossTransferToInitAccount(string symbol)
    {
        var token = _service.TokenService;
        var initAccount = _service.CallAddress;
        var mainChainId = ChainHelper.ConvertBase58ToChainId(_crossChainManager.FromNoeNodeManager.GetChainId());
        if (_crossChainManager.CheckPrivilegePreserved()) return;

        var initBalance = token.GetUserBalance(initAccount, symbol);
        if (initBalance > 50000_00000000) return;
        _logger.Info($"{initAccount} {symbol} balance is {initBalance}, need cross transfer first");

        //cross chain transfer 
        var amount = 50000_00000000;
        var result =
            _crossChainManager.CrossChainTransfer(symbol, amount, initAccount, initAccount, out string raw);
        var txId = _crossChainManager.FromNoeNodeManager.SendTransaction(raw);
        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

        // create input 
        var merklePath = _crossChainManager.GetMerklePath(result.BlockNumber,
            txId, out var root);

        var crossChainCreateToken = new CrossChainReceiveTokenInput
        {
            MerklePath = merklePath,
            FromChainId = mainChainId,
            ParentChainHeight = result.BlockNumber,
            TransferTransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(raw))
        };

        //check last transaction index 
        _crossChainManager.CheckSideChainIndexMainChain(result.BlockNumber);

        //side chain receive 
        var receiveResult =
            token.ExecuteMethodWithResult(TokenMethod.CrossChainReceiveToken, crossChainCreateToken);
        receiveResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        initBalance = token.GetUserBalance(initAccount, symbol);
        _logger.Info($"{initAccount} {symbol} balance is {initBalance}");
    }

    public void TransferAccount(List<string> accountList, string  symbol)
    {
        _logger.Info("Prepare chain basic token for tester.");
        var token = _service.TokenService;
        var txIds = new List<string>();
        foreach (var account in accountList)
        {
            var userBalance = token.GetUserBalance(account, symbol);
            if (userBalance > 2000_00000000)
                continue;
            token.SetAccount(_service.CallAddress);
            var txId = token.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
            {
                To = account.ConvertAddress(),
                Amount = 1000_00000000,
                Symbol = symbol,
                Memo = $"T-{Guid.NewGuid()}"
            });
            txIds.Add(txId);
        }
        token.NodeManager.CheckTransactionListResult(txIds);
    }

    private readonly Service _service;
    private readonly ILog _logger;
    private readonly INodeManager _nodeManager;
    private readonly CrossChainManager _crossChainManager;
}