using System.Collections.Generic;

namespace TtsApi.Hubs.TransferClasses
{
    public class TtsRequest
    {
        public string Id { get; set; }

        public float MaxMessageTimeSeconds { get; set; }

        public List<TtsIndividualSynthesize> TtsIndividualSynthesizes { get; set; }
    }
}
