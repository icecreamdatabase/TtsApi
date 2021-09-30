namespace TtsApi.Controllers.AuthController.AuthData
{
    public class AuthDataLogin : AuthData
    {
        private const string ReponseType = "token";

        private static readonly string[] Scopes =
        {
            ""
        };

        public static readonly string FullUrl =
            $"{BaseUrl}" +
            $"?client_id={ClientId}" +
            $"&redirect_uri={RedirectUrl}" +
            $"&response_type={ReponseType}" +
            $"&scope={string.Join("+", Scopes)}";
    }
}
