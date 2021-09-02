using System.Threading.Tasks;
using TtsApi.Hubs.TtsHub.TransferClasses;

namespace TtsApi.Hubs.TtsHub
{
    public interface ITtsHub
    {
        Task ConnId(string connectionId);
        
        Task TtsPlayRequest(TtsRequest request);

        Task TtsSkipCurrent();

        Task Reload();
    }
}
