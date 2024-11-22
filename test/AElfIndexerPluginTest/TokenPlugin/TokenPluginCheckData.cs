using AElf;
using AElf.Client.Dto;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfIndexer.Plugin.Dto;
using Google.Protobuf;
using SymbolType = AElfIndexer.Plugin.Dto.SymbolType;

namespace AElfIndexerPluginTest;

public class TokenPluginCheckData : TokenPluginTestBase
{
    protected bool CheckTokenInfo(TokenInfoDto tokenInfoDto, TokenInfo tokenInfo)
    {
        var external = new Dictionary<string, string>();
        foreach (var tokenExternalInfoDto in tokenInfoDto.ExternalInfo)
        {
            external[tokenExternalInfoDto.Key] = tokenExternalInfoDto.Value;
        }

        var type = SymbolType.TOKEN;
        if (tokenInfo.Symbol.Contains("-"))
        {
            type = tokenInfo.Symbol.Contains("-0") ? SymbolType.NFT_COLLECTION : SymbolType.NFT;
        }

        var externalCheck = true;
        if (!tokenInfo.ExternalInfo.Equals(new ExternalInfo()))
        {
            foreach (var (key, value) in external)
            {
                externalCheck = tokenInfo.ExternalInfo.Value.Keys.Contains(key) &&
                                tokenInfo.ExternalInfo.Value[key].Equals(value);
            }
        }

        return tokenInfoDto.Issued.Equals(tokenInfo.Issued) &&
               tokenInfoDto.IsBurnable.Equals(tokenInfo.IsBurnable) &&
               tokenInfoDto.Issuer.Equals(tokenInfo.Issuer.ToBase58()) &&
               tokenInfoDto.Owner.Equals(tokenInfo.Owner.ToBase58()) &&
               tokenInfoDto.Supply.Equals(tokenInfo.Supply) &&
               tokenInfoDto.TotalSupply.Equals(tokenInfo.TotalSupply) &&
               tokenInfoDto.TokenName.Equals(tokenInfo.TokenName) &&
               tokenInfoDto.Decimals.Equals(tokenInfo.Decimals) &&
               external.Count.Equals(tokenInfo.ExternalInfo.Value.Count) &&
               externalCheck && tokenInfoDto.Type.Equals(type);
    }


    private bool CheckAccountInfo(string address)
    {
        return true;
    }


    private bool CheckAccountToken(string address, string symbol)
    {
        return true;
    }


    protected bool CheckTransferInfo(TransferInfoDto transferInfo, TransactionResultDto transactionResultDto,
        string method)
    {
        var logEventDtos = transactionResultDto.Logs;
        var paramCheck = method switch
        {
            "Transfer" => CheckTransferParam(transferInfo, logEventDtos),
            "TransferFrom" => CheckTransferParam(transferInfo, logEventDtos),
            "CrossChainTransfer" => CheckCrossChainTransferParam(transferInfo, logEventDtos),
            "CrossChainReceive" => CheckCrossChainReceiveParam(transferInfo, logEventDtos),
            "Issue" => CheckIssueParam(transferInfo, logEventDtos),
            "Burn" => CheckBurnParam(transferInfo, logEventDtos),
            _ => false
        };

        return transferInfo.TransactionId.Equals(transactionResultDto.TransactionId) &&
               transferInfo.BlockHash.Equals(transactionResultDto.BlockHash) &&
               transferInfo.BlockHeight.Equals(transactionResultDto.BlockNumber) &&
               transferInfo.Method.Equals(method == "TransferFrom" ? "Transfer" : method) &&
               paramCheck;
    }
    
    protected bool CheckTransferInfo(TransferInfoDto transferInfo, LogEventDto[] logEventDto, string method)
    {
        var paramCheck = method switch
        {
            "Transfer" => CheckTransferParam(transferInfo, logEventDto),
            "TransferFrom" => CheckTransferParam(transferInfo, logEventDto),
            "CrossChainTransfer" => CheckCrossChainTransferParam(transferInfo, logEventDto),
            "CrossChainReceive" => CheckCrossChainReceiveParam(transferInfo, logEventDto),
            "Issue" => CheckIssueParam(transferInfo, logEventDto),
            "Burn" => CheckBurnParam(transferInfo, logEventDto),
            _ => false
        };

        return paramCheck;
    }

    private bool CheckTransferParam(TransferInfoDto transferInfoDto, LogEventDto[] logEventDtos)
    {
        var log = logEventDtos.First(l => l.Name.Equals("Transferred"));
        var nonIndexed = Transferred.Parser.ParseFrom(ByteString.FromBase64(log.NonIndexed));
        var from = new Address();
        var to = new Address();
        var symbol = "";
        foreach (var indexed in log.Indexed)
        {
            var transferredIndexed = Transferred.Parser.ParseFrom(ByteString.FromBase64(indexed));
            if (transferredIndexed.Symbol.Equals(""))
            {
                from = transferredIndexed.From ?? from;
                to = transferredIndexed.To ?? to;
            }
            else
                symbol = transferredIndexed.Symbol;
        }

        var d = transferInfoDto.Token.Decimals;
        var formatAmount = FormatAmount(nonIndexed.Amount, d);

        return transferInfoDto.From.Equals(from.ToBase58()) &&
               transferInfoDto.To.Equals(to.ToBase58()) &&
               transferInfoDto.Amount.Equals(nonIndexed.Amount) &&
               transferInfoDto.Memo.Equals(nonIndexed.Memo) &&
               transferInfoDto.Token.Symbol.Equals(symbol) &&
               transferInfoDto.FormatAmount.Equals(formatAmount);
    }

    private bool CheckCrossChainTransferParam(TransferInfoDto transferInfoDto, LogEventDto[] logEventDtos)
    {
        var log = logEventDtos.First(l => l.Name.Equals("CrossChainTransferred"));
        var nonIndexed = CrossChainTransferred.Parser.ParseFrom(ByteString.FromBase64(log.NonIndexed));
        var d = transferInfoDto.Token.Decimals;
        var formatAmount = FormatAmount(nonIndexed.Amount, d);

        var issueChainId = ChainHelper.ConvertChainIdToBase58(nonIndexed.IssueChainId);
        var toChainId = ChainHelper.ConvertChainIdToBase58(nonIndexed.ToChainId);
        return transferInfoDto.From.Equals(nonIndexed.From?.ToBase58()) &&
               transferInfoDto.To.Equals(nonIndexed.To?.ToBase58()) &&
               transferInfoDto.Amount.Equals(nonIndexed.Amount) &&
               transferInfoDto.Memo.Equals(nonIndexed.Memo) &&
               transferInfoDto.Token.Symbol.Equals(nonIndexed.Symbol) &&
               transferInfoDto.IssueChainId.Equals(issueChainId) &&
               transferInfoDto.FormatAmount.Equals(formatAmount) &&
               transferInfoDto.ToChainId.Equals(toChainId);
        // transferInfoDto.FromChainId.Equals(transferInfoDto.ChainId);
    }

    private bool CheckCrossChainReceiveParam(TransferInfoDto transferInfoDto, LogEventDto[] logEventDtos)
    {
        var log = logEventDtos.First(l => l.Name.Equals("CrossChainReceived"));
        var nonIndexed = CrossChainReceived.Parser.ParseFrom(ByteString.FromBase64(log.NonIndexed));
        var d = transferInfoDto.Token.Decimals;
        var formatAmount = FormatAmount(nonIndexed.Amount, d);

        var issueChainId = ChainHelper.ConvertChainIdToBase58(nonIndexed.IssueChainId);
        var fromChainId = ChainHelper.ConvertChainIdToBase58(nonIndexed.FromChainId);

        return transferInfoDto.From.Equals(nonIndexed.From?.ToBase58()) &&
               transferInfoDto.To.Equals(nonIndexed.To?.ToBase58()) &&
               transferInfoDto.Amount.Equals(nonIndexed.Amount) &&
               transferInfoDto.Memo.Equals(nonIndexed.Memo) &&
               transferInfoDto.Token.Symbol.Equals(nonIndexed.Symbol) &&
               transferInfoDto.IssueChainId.Equals(issueChainId) &&
               transferInfoDto.FormatAmount.Equals(formatAmount) &&
               // transferInfoDto.ToChainId.Equals(transferInfoDto.ChainId) &&
               transferInfoDto.FromChainId.Equals(fromChainId) &&
               transferInfoDto.ParentChainHeight.Equals(nonIndexed.ParentChainHeight) &&
               transferInfoDto.TransferTransactionId.Equals(nonIndexed.TransferTransactionId.ToHex());
    }

    private bool CheckIssueParam(TransferInfoDto transferInfoDto, LogEventDto[] logEventDtos)
    {
        var log = logEventDtos.First(l => l.Name.Equals("Issued"));
        var nonIndexed = Issued.Parser.ParseFrom(ByteString.FromBase64(log.NonIndexed));
        var d = transferInfoDto.Token.Decimals;
        var formatAmount = FormatAmount(nonIndexed.Amount, d);

        return transferInfoDto.To.Equals(nonIndexed.To.ToBase58()) &&
               transferInfoDto.From.Equals("") &&
               transferInfoDto.Amount.Equals(nonIndexed.Amount) &&
               transferInfoDto.Memo.Equals(nonIndexed.Memo) &&
               transferInfoDto.Token.Symbol.Equals(nonIndexed.Symbol) &&
               transferInfoDto.FormatAmount.Equals(formatAmount);
    }

    private bool CheckBurnParam(TransferInfoDto transferInfoDto, LogEventDto[] logEventDtos)
    {
        var log = logEventDtos.First(l => l.Name.Equals("Burned"));
        var nonIndexed = Burned.Parser.ParseFrom(ByteString.FromBase64(log.NonIndexed));
        var symbol = "";
        var burner = new Address();
        foreach (var indexed in log.Indexed)
        {
            var burnedIndexed = Burned.Parser.ParseFrom(ByteString.FromBase64(indexed));
            if (burnedIndexed.Symbol.Equals(""))
            {
                burner = burnedIndexed.Burner;
            }
            else
                symbol = burnedIndexed.Symbol;
        }

        var d = transferInfoDto.Token.Decimals;
        var formatAmount = FormatAmount(nonIndexed.Amount, d);

        return transferInfoDto.To.IsNullOrEmpty() &&
               transferInfoDto.From.Equals(burner.ToBase58()) &&
               transferInfoDto.Amount.Equals(nonIndexed.Amount) &&
               transferInfoDto.Memo.IsNullOrEmpty() &&
               transferInfoDto.Token.Symbol.Equals(symbol) &&
               transferInfoDto.FormatAmount.Equals(formatAmount);
    }

    protected decimal FormatAmount(long amount, int d)
    {
        return Math.Round(amount / (decimal)Math.Pow(10, d), d);
    }
}