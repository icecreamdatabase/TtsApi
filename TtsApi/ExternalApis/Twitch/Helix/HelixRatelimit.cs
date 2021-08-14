using TtsApi.Helper;

namespace TtsApi.ExternalApis.Twitch.Helix
{
    public static class HelixRatelimit
    {
        // TODO: Have some central Helix HttpClient that listens to Ratelimit response headers
        public static readonly BasicBucket Bucket = new(800, 60);
    }
}
