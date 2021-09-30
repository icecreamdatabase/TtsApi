namespace TtsApi.Controllers.AuthController.AuthData
{
    public class AuthDataSignup : AuthData
    {
        private const string ReponseType = "code";

        private static readonly string[] Scopes =
        {
            "moderation:read",
            "channel:read:redemptions",
            "channel:manage:redemptions",
            "channel:moderate"
        };

        public static readonly string FullUrl =
            $"{BaseUrl}" +
            $"?client_id={ClientId}" +
            $"&redirect_uri={RedirectUrl}" +
            $"&response_type={ReponseType}" +
            $"&scope={string.Join("+", Scopes)}";
    }
}
