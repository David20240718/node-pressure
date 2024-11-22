namespace AElfIndexer.Plugin.Dto;

public class TokenInfoDto : TokenBaseDto
{
    public string TokenName { get; set; }
    public long TotalSupply { get; set; }
    public long Supply { get; set; }
    public long Issued { get; set; }
    public string Issuer { get; set; }
    public string Owner { get; set; }
    public bool IsPrimaryToken { get; set; }
    public bool IsBurnable { get; set; }
    public string IssueChainId { get; set; }
    public List<TokenExternalInfoDto> ExternalInfo { get; set; } = new();
    public long HolderCount { get; set; }
    public long TransferCount { get; set; }
}

public class TokenExternalInfoDto
{
    public string Key { get; set; }
    public string Value { get; set; }
}