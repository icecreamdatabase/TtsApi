using System.Collections.Generic;

namespace TtsApi.Hubs.TransferClasses
{
    public class TtsRequest
    {
        public string Id { get; set; }
        
        public float Volume { get; set; } 
        
        public float MaxMessageTime { get; set; }
        
        public List<TtsIndividualSynthesize> TtsIndividualSynthesizes { get; set; }
    }
}
