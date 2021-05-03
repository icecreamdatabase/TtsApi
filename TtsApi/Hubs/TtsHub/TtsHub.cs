using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using TtsApi.Hubs.TtsHub.TransferClasses;
using TtsApi.Hubs.TtsHub.TransformationClasses;
using TtsApi.Model;

namespace TtsApi.Hubs.TtsHub
{
    //https://dotnetplaybook.com/which-is-best-websockets-or-signalr/
    public class TtsHub : Hub<ITtsHub>
    {
        private readonly TtsDbContext _ttsDbContext;
        private readonly TtsHandler _ttsHandler;

        public TtsHub(TtsDbContext ttsDbContext, TtsHandler ttsHandler)
        {
            _ttsDbContext = ttsDbContext;
            _ttsHandler = ttsHandler;
        }

        public override async Task<Task> OnConnectedAsync()
        {
            if (!string.IsNullOrEmpty(Context.UserIdentifier))
                await Groups.AddToGroupAsync(Context.ConnectionId, Context.UserIdentifier);
            TtsHandler.ConnectClients.Add(Context.ConnectionId, Context.UserIdentifier);
            Console.WriteLine($"--> Connection Opened: {Context.ConnectionId} (roomId: {Context.UserIdentifier})");
            await Clients.Client(Context.ConnectionId).ConnId(Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            Console.WriteLine($"--> Connection Closed: {Context.ConnectionId} (roomId: {Context.UserIdentifier})");
            TtsHandler.ConnectClients.Remove(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        public void ConfirmTtsFullyPlayed(string id)
        {
            Console.WriteLine($"Confirmed {Context.UserIdentifier} has fully played tts {id}");
            _ttsHandler.ConfirmTtsFullyPlayed(Context.ConnectionId, Context.UserIdentifier, id);
        }

        public void ConfirmTtsSkipped(string id)
        {
            Console.WriteLine($"Confirmed {Context.UserIdentifier} has skipped tts {id}");
            _ttsHandler.ConfirmTtsSkipped(Context.ConnectionId, Context.UserIdentifier, id);
        }

        public static async Task SendTtsRequest(IHubContext<TtsHub, ITtsHub> context, string roomId, TtsRequest request)
        {
            await context.Clients.Group(roomId).TtsPlayRequest(request);
        }
    }
}
