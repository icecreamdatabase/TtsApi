using System.Diagnostics.CodeAnalysis;

namespace TtsApi.Controllers.AuthController
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
    [SuppressMessage("ReSharper", "CA1822")]
    public class AuthDataLoginView
    {
        private readonly AuthDataView _authDataView;
        public AuthDataLoginView(AuthDataView authDataView) => _authDataView = authDataView;

        public string ReponseType => "token";
        public string Scope => "";

        public string FullUrl =>
            $"{_authDataView.BaseUrl}?client_id={_authDataView.ClientId}&redirect_uri={_authDataView.RedirectUrl}&response_type={ReponseType}&scope={Scope}";
    }
}
