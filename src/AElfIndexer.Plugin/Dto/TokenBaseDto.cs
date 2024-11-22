namespace AElfIndexer.Plugin.Dto;

public class TokenBaseDto : GraphQLDto
{
    public string Symbol { get; set; }
    public string CollectionSymbol { get; set; }
    public SymbolType Type { get; set; }
    public int Decimals { get; set; }
}
public enum SymbolType
{
    TOKEN,
    NFT,
    NFT_COLLECTION
}