using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using Shouldly;
using SymbolType = AElfIndexer.Plugin.Dto.SymbolType;

namespace AElfIndexerPluginTest;

[TestClass]
public class IssueAndBurnTokenPluginTest : TokenPluginCheckData
{
    [TestInitialize]
    public void InitializeTest()
    {
        Initialize();
    }

    [TestMethod]
    [DataRow("CPU")]
    public async Task CheckToken(string symbol)
    {
        var mainToSideManager = _mainToSideManagers.First(s => s.ToChainNodeManager.GetChainId().Equals("tDVW"));
        var chainId = mainToSideManager.ToChainNodeManager.GetChainId();

        var tokenInfo = await _token.GetTokenInfo(chainId, symbol);
        var tokenInfoFromChain = mainToSideManager.ToChainToken.GetTokenInfo(symbol);
        var owner = tokenInfoFromChain.Owner == null ? tokenInfoFromChain.Issuer : tokenInfoFromChain.Owner;
        tokenInfo.First().Issuer.ShouldBe(tokenInfoFromChain.Issuer.ToBase58());
        tokenInfo.First().Decimals.ShouldBe(tokenInfoFromChain.Decimals);
        tokenInfo.First().Supply.ShouldBe(tokenInfoFromChain.Supply);
        tokenInfo.First().TotalSupply.ShouldBe(tokenInfoFromChain.TotalSupply);
        tokenInfo.First().Owner.ShouldBe(owner.ToBase58());
    }
    
    [TestMethod]
    [DataRow("tDVW")]
    public async Task CheckTokenWithTypes(string chainId)
    {
        var mainToSideManager = _mainToSideManagers.First(s => s.ToChainNodeManager.GetChainId().Equals("tDVW"));
        var types = new List<AElfIndexer.Plugin.Dto.SymbolType> { SymbolType.NFT_COLLECTION };
        var nftCollectionTokenInfo = await _token.GetTokenInfo(chainId, "", 0, 100, "", "", "", "","", types);
        Logger.Info(nftCollectionTokenInfo.Count);
        // foreach (var t in nftCollectionTokenInfo)
        // {
        //     var tokenInfoFromChain = mainToSideManager.ToChainToken.GetTokenInfo(t.Symbol);
        //     var checkOwner = tokenInfoFromChain.Owner == null ? tokenInfoFromChain.Issuer : tokenInfoFromChain.Owner;
        //     t.Issuer.ShouldBe(tokenInfoFromChain.Issuer.ToBase58());
        //     t.Decimals.ShouldBe(tokenInfoFromChain.Decimals);
        //     t.Supply.ShouldBe(tokenInfoFromChain.Supply);
        //     t.TotalSupply.ShouldBe(tokenInfoFromChain.TotalSupply);
        //     t.Owner.ShouldBe(checkOwner.ToBase58());
        // }
        
        types = new List<AElfIndexer.Plugin.Dto.SymbolType> { SymbolType.NFT };
        var nftTokenInfo = await _token.GetTokenInfo(chainId, "", 0, 200, "", "", "", "","", types);
        Logger.Info(nftTokenInfo.Count);
        nftTokenInfo.Any(n => n.Symbol.Equals("BEANPASS-1")).ShouldBeTrue();
        
        types = new List<AElfIndexer.Plugin.Dto.SymbolType> { SymbolType.TOKEN };
        var tokenInfo = await _token.GetTokenInfo(chainId, "", 0, 200, "", "", "", "","", types);
        Logger.Info(tokenInfo.Count);
    }
    
    [TestMethod]
    [DataRow("tDVW")]
    public async Task CheckTokenWithParam(string chainId)
    {
        var mainToSideManager = _mainToSideManagers.First(s => s.ToChainNodeManager.GetChainId().Equals("tDVW"));
        var types = new List<AElfIndexer.Plugin.Dto.SymbolType> { SymbolType.NFT_COLLECTION };
        var partialTokenName = "d";
        var nftCollectionTokenInfo = await _token.GetTokenInfo(chainId, "", 0, 100, "", "", "", "","", types);
        var nftCollectionTokenInfoWithTokenName = await _token.GetTokenInfo(chainId, "", 0, 100, "", "", partialTokenName, "","", types);
        foreach (var t in nftCollectionTokenInfo)
        {
            var tokenInfoFromChain = mainToSideManager.ToChainToken.GetTokenInfo(t.Symbol);
            var checkOwner = tokenInfoFromChain.Owner == null ? tokenInfoFromChain.Issuer : tokenInfoFromChain.Owner;
            t.TokenName.ToLower().ShouldContain(partialTokenName);
            t.Issuer.ShouldBe(tokenInfoFromChain.Issuer.ToBase58());
            t.Decimals.ShouldBe(tokenInfoFromChain.Decimals);
            t.Supply.ShouldBe(tokenInfoFromChain.Supply);
            t.TotalSupply.ShouldBe(tokenInfoFromChain.TotalSupply);
            t.Owner.ShouldBe(checkOwner.ToBase58());
        }
        Logger.Info(nftCollectionTokenInfoWithTokenName.Count);

        var withoutCount = 1;
        foreach (var t in nftCollectionTokenInfo)
        {
            var tokenInfoFromChain = mainToSideManager.ToChainToken.GetTokenInfo(t.Symbol);
            var checkOwner = tokenInfoFromChain.Owner == null ? tokenInfoFromChain.Issuer : tokenInfoFromChain.Owner;
            if (nftCollectionTokenInfoWithTokenName.Contains(t))
            {
                t.TokenName.ToLower().ShouldContain(partialTokenName);
            }
            else
            {
                t.TokenName.ToLower().ShouldNotContain(partialTokenName);
                withoutCount++;
            }

            t.Issuer.ShouldBe(tokenInfoFromChain.Issuer.ToBase58());
            t.Decimals.ShouldBe(tokenInfoFromChain.Decimals);
            t.Supply.ShouldBe(tokenInfoFromChain.Supply);
            t.TotalSupply.ShouldBe(tokenInfoFromChain.TotalSupply);
            t.Owner.ShouldBe(checkOwner.ToBase58());
        }
        withoutCount.Add(nftCollectionTokenInfoWithTokenName.Count).ShouldBe(nftCollectionTokenInfo.Count);
    }
    
    [TestMethod]
    [DataRow("tDVW","")]
    public async Task CheckTokenCheckSipCount(string chainId, string symbol)
    {
        var types = new List<AElfIndexer.Plugin.Dto.SymbolType> { SymbolType.NFT_COLLECTION };
        var nftCollectionTokenInfo = await _token.GetTokenInfo(chainId, symbol, 0, 100, "", "", "", "","", types);
        Logger.Info(nftCollectionTokenInfo.Count);

        types = new List<AElfIndexer.Plugin.Dto.SymbolType> { SymbolType.NFT };
        var nftTokenInfoFirst = await _token.GetTokenInfo(chainId, symbol, 0, 100, "", "", "", "","", types);
        Logger.Info(nftTokenInfoFirst.Count);
        var firstList = nftTokenInfoFirst.Select(l => l.Symbol).ToList();
        var nftTokenInfoSecond = await _token.GetTokenInfo(chainId, symbol, 100, 100, "", "", "", "","", types);
        Logger.Info(nftTokenInfoSecond.Count);
        nftTokenInfoSecond.Any(n => n.Symbol.IsIn(firstList)).ShouldBeFalse();
        
        types = new List<AElfIndexer.Plugin.Dto.SymbolType> { SymbolType.TOKEN };
        var tokenInfo = await _token.GetTokenInfo(chainId, symbol, 0, 200, "", "", "", "","", types);
        Logger.Info(tokenInfo.Count);
    }

    [TestMethod]
    [DataRow("3591c8637cdaf5c34632d07774c0ced4168b2edc5e76af537fa75e6381ad8bc3","tDVW")]
    public async Task CheckTransaction(string txId, string chainId)
    {
        var mainToSideManager = _mainToSideManagers.First(s => s.ToChainNodeManager.GetChainId().Equals(chainId));
        var txResult = mainToSideManager.ToChainNodeManager.CheckTransactionResult(txId);
        var tokenAddress = mainToSideManager.ToChainToken.ContractAddress;
        var transferLogs = txResult.Logs.Where(l => l.Name.Equals("Transferred") && l.Address.Equals(tokenAddress)).ToList();
        var burnLogs = txResult.Logs.Where(l => l.Name.Equals("Burned") && l.Address.Equals(tokenAddress)).ToList();
        var issueLogs = txResult.Logs.Where(l => l.Name.Equals("Issued") && l.Address.Equals(tokenAddress)).ToList();
        var crossChainLogs = txResult.Logs.Where(l => l.Name.Equals("CrossChainTransferred") && l.Address.Equals(tokenAddress)).ToList();
        var receiveLogs = txResult.Logs.Where(l => l.Name.Equals("CrossChainReceived") && l.Address.Equals(tokenAddress)).ToList();

        var transactionInfo = await _token.GetTransferInfo(chainId, "", "", txId);
        transactionInfo.Count.ShouldBe(transferLogs.Count.Add(burnLogs.Count).Add(issueLogs.Count).Add(crossChainLogs.Count).Add(receiveLogs.Count));
        var transferInfos =
            await _token.GetTransferInfo(chainId, "", "", txId, 0, 10, "", "", new List<string> { "Transfer" });
        foreach (var transfer in transferInfos)
        {
            CheckTransferInfo(transfer, txResult, "Transfer").ShouldBeTrue();
        }

        var burnInfos =
            await _token.GetTransferInfo(chainId, "", "", txId, 0, 10, "", "", new List<string> { "Burn" });
        foreach (var burnInfo in burnInfos)
        {
            CheckTransferInfo(burnInfo, txResult, "Burn").ShouldBeTrue();
        }
        
        var issueInfos = await _token.GetTransferInfo(chainId, "", "", txId, 0, 10, "", "", new List<string> { "Issue" });
        foreach (var issueInfo in issueInfos)
        {
            CheckTransferInfo(issueInfo, txResult, "Issue").ShouldBeTrue();
        }
        
        var crossChainInfos = await _token.GetTransferInfo(chainId, "", "", txId, 0, 10, "", "", new List<string> { "CrossChainTransfer" });
        foreach (var crossChainInfo in crossChainInfos)
        {
            CheckTransferInfo(crossChainInfo, txResult, "CrossChainTransfer").ShouldBeTrue();
        }
        
        var receiveInfos = await _token.GetTransferInfo(chainId, "", "", txId, 0, 10, "", "", new List<string> { "CrossChainReceiveToken" });
        foreach (var receive in receiveInfos)
        {
            CheckTransferInfo(receive, txResult, "CrossChainReceive").ShouldBeTrue();
        }
    }
    
    [TestMethod]
    [DataRow("tDVW","USDT","","")]
    public async Task CheckTransferInfoWithParam(string chainId, string symbol, string address,
        string transactionId, int skipCount = 0, int maxResultCount = 100, string from = "", string to = "", 
        List<string>? methods = null)
    {
        var transferInfo = await _token.GetTransferInfo(chainId, symbol, address, transactionId);
        transferInfo.All(t => t.Token.Symbol.Equals(symbol)).ShouldBeTrue();
        foreach (var t in transferInfo)
        {
            Logger.Info(t.TransactionId);
        }
    }

    [TestMethod]
    [DataRow("")]
    public async Task GetAccountToken(string address)
    {
        var mainToSideManager = _mainToSideManagers.First(s => s.ToChainNodeManager.GetChainId().Equals("tDVW"));
        var chainId = mainToSideManager.ToChainNodeManager.GetChainId();
        var tokenContract = mainToSideManager.ToChainToken;
        var accountToken = await _token.GetAccountToken(chainId, address,"", "", 0, 100);
        Logger.Info(accountToken.Count);
        
        var ELFBalance = tokenContract.GetUserBalance(address, "ELF");
        var accountELFInfo = accountToken.Find(a => a.Token.Symbol.Equals("ELF"));
        accountELFInfo.Amount.ShouldBe(ELFBalance);
        accountELFInfo.FormatAmount.ShouldBe(FormatAmount(ELFBalance, accountELFInfo.Token.Decimals ));
        accountELFInfo.Token.Decimals.ShouldBe(8);
    }
}