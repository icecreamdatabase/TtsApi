namespace TtsApi.Authentication.Policies
{
    public static class Policies
    {
        public const string RequiredSignupScopes = nameof(RequiredSignupScopes);
        public const string CanChangeSettings = nameof(CanChangeSettings);
        public const string CanAccessQueue = nameof(CanAccessQueue);
    }
}
