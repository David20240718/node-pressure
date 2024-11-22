using AElfIndexer.Plugin.Dto;
using GraphQL;

namespace AElfIndexer.Plugin.PluginQuery;

public class TokenQuery
{
    public TokenQuery(string uri)
    {
        GraphQLClient.GetGraphQlHttpClient(uri);
    }

    public async Task<List<AccountInfoDto>> GetAccountInfo(string chainId, string address, int skipCount = 0,
        int maxResultCount = 10)
    {
        var request = GetAccountInfoRequest(chainId, address, skipCount, maxResultCount);
        var data = await GraphQLClient.QueryDataAsync<TokenQueryResponse.AccountInfoResponse>(request);
        return data.AccountInfo;
    }

    public async Task<List<AccountTokenDto>> GetAccountToken(string chainId, string address, string symbol,
        string partialSymbol = "", int skipCount = 0, int maxResultCount = 10)
    {
        var request = GetAccountTokenRequest(chainId, symbol, partialSymbol, address, skipCount, maxResultCount);
        var data = await GraphQLClient.QueryDataAsync<TokenQueryResponse.AccountTokenResponse>(request);
        return data.AccountToken;
    }

    public async Task<List<TokenInfoDto>> GetTokenInfo(string chainId, string symbol, int skipCount = 0,
        int maxResultCount = 10, string partialSymbol = "", string tokenName = "", string partialTokenName = "",
        string owner = "", string issuer = "", List<SymbolType>? types = null)
    {
        var list = types ?? new List<SymbolType>();
        var request = GetTokenInfoRequest(chainId, symbol, partialSymbol, tokenName, partialTokenName, owner, issuer,
            list, skipCount, maxResultCount);
        var data = await GraphQLClient.QueryDataAsync<TokenQueryResponse.TokenInfoResponse>(request);
        return data.TokenInfo;
    }

    public async Task<List<TransferInfoDto>> GetTransferInfo(string chainId, string symbol, string address,
        string transactionId, int skipCount = 0, int maxResultCount = 10, string from = "", string to = "", 
        List<string>? methods = null)
    {
        var list = methods ?? new List<string>();
        var request = GetTransferInfoRequest(chainId, symbol, address, from, to, list, transactionId, skipCount,
            maxResultCount);
        var data = await GraphQLClient.QueryDataAsync<TokenQueryResponse.TransferInfoResponse>(request);
        return data.TransferInfo;
    }

    public GraphQLRequest GetTokenInfoRequest(string chainId, string symbol, string partialSymbol, string tokenName,
        string partialTokenName, string owner, string issuer, List<SymbolType> types, int skipCount, int maxResultCount)
    {
        return new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$symbol:String,$partialSymbol:String,
                $tokenName:String,$partialTokenName:String,$owner:String,$issuer:String,$types:[SymbolType!],
                $skipCount:Int!, $maxResultCount:Int!){
            tokenInfo(input: {chainId:$chainId, symbol:$symbol, partialSymbol: $partialSymbol, 
                tokenName: $tokenName, partialTokenName:$partialTokenName, 
                owner:$owner, issuer:$issuer, types:$types, 
                skipCount:$skipCount, maxResultCount:$maxResultCount}){
                id,
                chainId,
                blockHash,
                blockHeight,
                blockTime,
                symbol, 
                collectionSymbol,
                type,
                tokenName,
                totalSupply,
                supply,
                issued,
                decimals,
                issuer,
                owner,
                isPrimaryToken,
                isBurnable,
                issueChainId,
                externalInfo{
                    key,
                    value
                },
                holderCount,
                transferCount
            }
        }",
            Variables = new
            {
                chainId = chainId,
                symbol = symbol,
                partialSymbol = partialSymbol,
                tokenName = tokenName,
                partialTokenName = partialTokenName,
                owner = owner,
                issuer = issuer,
                types = types,
                skipCount = skipCount,
                maxResultCount = maxResultCount
            }
        };
    }

    public GraphQLRequest GetAccountInfoRequest(string chainId, string address, int skipCount, int maxResultCount)
    {
        return new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$address:String,$skipCount:Int!, $maxResultCount:Int!){
            accountInfo(input: {chainId:$chainId, address:$address, 
                skipCount:$skipCount, maxResultCount:$maxResultCount}){
                id,
                chainId,
                blockHash,
                blockHeight,
                blockTime,
                address, 
                tokenHoldingCount,
                transferCount
            }
        }",
            Variables = new
            {
                chainId = chainId,
                address = address,
                skipCount = skipCount,
                maxResultCount = maxResultCount
            }
        };
    }

    public GraphQLRequest GetAccountTokenRequest(string chainId, string symbol, string partialSymbol, string address,
        int skipCount, int maxResultCount)
    {
        return new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$symbol:String,$partialSymbol:String,$address:String,$skipCount:Int!, $maxResultCount:Int!){
            accountToken(input: {chainId:$chainId, symbol:$symbol, partialSymbol:$partialSymbol, address:$address, 
                skipCount:$skipCount, maxResultCount:$maxResultCount}){
                id,
                chainId,
                blockHash,
                blockHeight,
                blockTime,
                address, 
                token{
                    symbol,
                    collectionSymbol,
                    type,
                    decimals
                },
                amount,
                formatAmount,
                transferCount,
                firstNftTransactionId,
                firstNftTime        
            }
        }",
            Variables = new
            {
                chainId = chainId,
                symbol = symbol,
                partialSymbol = partialSymbol,
                address = address,
                skipCount = skipCount,
                maxResultCount = maxResultCount
            }
        };
    }

    public GraphQLRequest GetTransferInfoRequest(string chainId, string symbol, string address, string from, string to,
        List<string> methods, string transactionId, int skipCount, int maxResultCount)
    {
        return new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$symbol:String,$address:String,
                $from:String,$to:String,$methods:[String],$transactionId:String,
                $skipCount:Int!, $maxResultCount:Int!){
            transferInfo(input: {chainId:$chainId, symbol:$symbol, address: $address, 
                from: $from, to:$to, methods:$methods, transactionId:$transactionId,
                skipCount:$skipCount, maxResultCount:$maxResultCount}){
                id,
                chainId,
                blockHash,
                blockHeight,
                blockTime,
                transactionId, 
                from,
                to,
                method,
                amount,
                formatAmount,
                token{
                    symbol,
                    collectionSymbol,
                    type,
                    decimals
                },
                memo,
                fromChainId,
                toChainId,
                issueChainId,
                parentChainHeight,
                transferTransactionId
            }
        }",
            Variables = new
            {
                chainId = chainId,
                symbol = symbol,
                address = address,
                from = from,
                to = to,
                methods = methods,
                transactionId = transactionId,
                skipCount = skipCount,
                maxResultCount = maxResultCount
            }
        };
    }
}