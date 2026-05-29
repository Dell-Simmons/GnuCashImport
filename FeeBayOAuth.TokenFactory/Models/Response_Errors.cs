namespace FeeBayOAuth.TokenService.Models
{
    public class Response_Errors
    {
        public Error[] errors { get; set; }

        public class Error
        {
            public int errorId { get; set; }
            public string domain { get; set; }
            public string category { get; set; }
            public string message { get; set; }
            public Parameter[] parameters { get; set; }
        }

        public class Parameter
        {
            public string name { get; set; }
            public string value { get; set; }
        }
    }
}
