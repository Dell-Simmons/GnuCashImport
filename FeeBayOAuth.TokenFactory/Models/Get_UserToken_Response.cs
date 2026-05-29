namespace FeeBayOAuth.TokenService.Models
{
    public class Get_UserToken_Response
    {
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public string token_type { get; set; }
    }
}
