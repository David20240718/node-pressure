using System.Collections.Concurrent;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;
using log4net;

namespace PortkeyCryptoBoxContractScript;

public class AccountService
{
    public AccountService(Service service, ILog logger)
    {
        _service = service;
        _prepareService = new PrepareService(service, logger);
        _logger = logger;
        _nodeManager = service.NodeManager;
    }
    
     public List<string> InitSymbolAndAccount(int userCount, long amount)
    {
        var eoaAccountList = GetTestAccounts(userCount);
        var getAllAccount = GetTestAccounts();
        TransferToInit(getAllAccount);
        
        _logger.Info("=== Transfer NativeToken to tester address ===");
        var transferAmount = amount == 0 ? 10_00000000 : amount;
        TransferAccountForSizeFee(eoaAccountList, "ELF", transferAmount);
        CheckBalance(eoaAccountList, "ELF");
        return eoaAccountList;
    }


    public List<string> GetTestAccounts(int count = 0)
    {
        var accountList = new List<string>();
        var authority = new AuthorityManager(_nodeManager, _service.CallAddress);
        var miners = authority.GetCurrentMiners();
        var accounts = _nodeManager.ListAccounts();
        var testUsers = accounts.FindAll(o => !miners.Contains(o) && !o.Equals(_service.CallAddress));
        var takeCount = count == 0 ? testUsers.Count : count;
        if (testUsers.Count >= takeCount)
        {
            foreach (var acc in testUsers.Take(takeCount)) accountList.Add(acc);
        }
        else
        {
            foreach (var acc in testUsers) accountList.Add(acc);

            var generateCount = takeCount - testUsers.Count;
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
    
    public List<string> GenerateTesterPair(List<string> eoaAccountList)
    {
        var toAccountList = new List<string>();
        for (var i = 0; i < eoaAccountList.Count; i++)
        {
            var toAccount = _nodeManager.AccountManager.NewFakeAccount();
            toAccountList.Add(toAccount);
        }

        return toAccountList;
    }

    public Dictionary<string, string> GetEoaDelegateeTestAccountPair(List<string> accountList)
    {
        var eoaDelegatorPair = new Dictionary<string, string>();
        for (var i = 0; i < accountList.Count; i += 2)
        {
            eoaDelegatorPair[accountList[i]] = accountList[i+1];
        }

        return eoaDelegatorPair;
    }

    public void TransferAccountForSizeFee(List<string> accountList, string symbol, long amount)
    {
        _logger.Info($"=== Prepare {symbol} token for tester ===");
        var token = _service.TokenService;
        var txIds = new ConcurrentQueue<string>();
        Parallel.ForEach(accountList, account =>
        {
            
            var userBalance = token.GetUserBalance(account, symbol);
            if (userBalance >= amount)
                return;
            token.SetAccount(_service.CallAddress);
            var txId = token.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
            {
                To = account.ConvertAddress(),
                Amount = amount,
                Symbol = symbol,
                Memo = $"T-{Guid.NewGuid()}"
            });
            txIds.Enqueue(txId);
        }); 

        Parallel.ForEach(txIds, txId =>
        {
            _nodeManager.CheckTransactionResult(txId);
        });
        
        Parallel.ForEach(accountList, account =>
        {
            var balance = token.GetUserBalance(account);
            if (balance < amount.Div(2))
            {
                throw new Exception("Test account balance not enough");
            }
        });
    }
    
    public void TransferToInit(List<string> accountList)
    {
        var token = _service.TokenService;
        var txIds = new List<string>();
        foreach (var account in accountList)
        {
            if (account == _service.CallAddress)
            {
                continue;
            }
            var userBalance = token.GetUserBalance(account, "ELF");
            if (userBalance <= 100000000)
            {
                continue;
            }
            token.SetAccount(account);
            var txId = token.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
            {
                To = _service.CallAccount,
                Amount = userBalance.Sub(100000000),
                Symbol = "ELF",
                Memo = $"T-{Guid.NewGuid()}"
            });
            txIds.Add(txId);
        }
        Parallel.For(0, txIds.Count, i =>
        {
            _nodeManager.CheckTransactionResult(txIds[i]);
        });
    }

    public void CheckDelegatee(List<string> accountList)
    {
        Parallel.For(0, accountList.Count, i =>
        {
            var delegatees = _service.TokenService.GetTransactionFeeDelegatees(accountList[i]);
            _logger.Info(delegatees.DelegateeAddresses.Count);
        });
    }

    public void SetSecondaryDelegate(List<string> accountList, string delegatee)
    {
        _prepareService.SetSecondaryDelegate(accountList, delegatee);
    }

    public void CheckBalance(List<string> accountList, string symbol)
    {
        Parallel.For(0, accountList.Count, item =>
        {
            var balance = _service.TokenService.GetUserBalance(accountList[item], symbol);
            _logger.Info($"{accountList[item]} {symbol} balance: {balance}");
        });
    }


    private readonly Service _service;
    private readonly PrepareService _prepareService;
    private readonly ILog _logger;
    private readonly INodeManager _nodeManager;
    
}