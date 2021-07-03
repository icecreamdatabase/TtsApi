using System.Diagnostics.CodeAnalysis;

namespace TtsApi.Controllers.AuthController
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
    [SuppressMessage("ReSharper", "CA1822")]
    public class AuthDataSignupView
    {
        private readonly AuthDataView _authDataView;
        public AuthDataSignupView(AuthDataView authDataView) => _authDataView = authDataView;

        public string ReponseType => "code";
        public string Scope => "moderation:read+channel:read:redemptions+channel:manage:redemptions";

        public string FullUrl =>
            $"{_authDataView.BaseUrl}?client_id={_authDataView.ClientId}&redirect_uri={_authDataView.RedirectUrl}&response_type={ReponseType}&scope={Scope}";
    }
}
