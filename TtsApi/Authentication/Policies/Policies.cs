namespace TtsApi.Authentication.Policies
{
    public static class Policies
    {
        public const string RequiredSignupScopes = nameof(RequiredSignupScopes);
        public const string CanChangeChannelSettings = nameof(CanChangeChannelSettings);
        public const string CanAccessQueue = nameof(CanAccessQueue);
        public const string CanChangeBotSettings = nameof(CanChangeBotSettings);
    }
}
