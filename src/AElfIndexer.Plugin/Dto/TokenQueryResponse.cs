namespace AElfIndexer.Plugin.Dto;

public class TokenQueryResponse
{
    public class TokenInfoResponse
    {
        public List<TokenInfoDto> TokenInfo { get; set; }
    }
    
    public class AccountInfoResponse
    {
        public List<AccountInfoDto> AccountInfo { get; set; }
    }
    
    public class AccountTokenResponse
    {
        public List<AccountTokenDto> AccountToken { get; set; }
    }
    
    public class TransferInfoResponse
    {
        public List<TransferInfoDto> TransferInfo { get; set; }
    }
}