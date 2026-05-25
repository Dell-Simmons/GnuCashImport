namespace FeeBayOAuth.TokenFactory.Models
{
    public class UserTokenResult
    {
        public bool IsSuccess { get; private set; }
        public Get_UserToken_Response? Response { get; private set; }
        public Response_Errors? Errors { get; private set; }

        private UserTokenResult() { }

        public static UserTokenResult Success(Get_UserToken_Response? response)
        {
            return new UserTokenResult
            {
                IsSuccess = true,
                Response = response
            };
        }

        public static UserTokenResult Failure(Response_Errors? errors)
        {
            return new UserTokenResult
            {
                IsSuccess = false,
                Errors = errors
            };
        }
    }
}
