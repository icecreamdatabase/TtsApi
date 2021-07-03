using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace TtsApi.Controllers.AuthController
{
    [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "CA1822")]
    public class AuthDataView
    {
        private static AuthDataView _authDataViewInstance;

        public static AuthDataView Instance => _authDataViewInstance ??= new AuthDataView();

        public string BaseUrl => "https://id.twitch.tv/oauth2/authorize";
        public string ClientId => "fy2ntph9sdyeb73p5mdf7o5h4hs5j8";
        public string RedirectUrl => "https://tts.icdb.dev";

        private AuthDataLoginView _authDataLoginView;
        [JsonPropertyName("login")]
        public AuthDataLoginView AuthDataLoginView => _authDataLoginView ??= new AuthDataLoginView(this);

        private AuthDataSignupView _authDataSignupView;
        [JsonPropertyName("signup")]
        public AuthDataSignupView AuthDataSignupView => _authDataSignupView ??= new AuthDataSignupView(this);
    }
}
