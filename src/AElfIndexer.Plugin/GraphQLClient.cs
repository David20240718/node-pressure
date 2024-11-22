using AElfChain.Common.Helpers;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using log4net;

namespace AElfIndexer.Plugin;

public static class GraphQLClient
{
    private static GraphQLHttpClient? _graphQLClient;
    // private static readonly ILog Logger = Log4NetHelper.GetLogger();

    public static GraphQLHttpClient? GetGraphQlHttpClient(string url)
    {
        _graphQLClient = new GraphQLHttpClient(url, new NewtonsoftJsonSerializer());
        return _graphQLClient;
    }
    
    public static async Task<T> QueryDataAsync<T>(GraphQLRequest request)
    {
        var data = await _graphQLClient?.SendQueryAsync<T>(request)!;
        if (data.Errors == null || data.Errors.Length == 0)
        {
            return data.Data;
        }

        // Logger.Error("Query indexer failed. errors: {Errors}",
        // string.Join(",", data.Errors.Select(e => e.Message).ToList()));
        return default;
    }
    

}