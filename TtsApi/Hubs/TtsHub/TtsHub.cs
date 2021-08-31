using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using TtsApi.Hubs.TtsHub.TransferClasses;
using TtsApi.Hubs.TtsHub.TransformationClasses;
using TtsApi.Model;

namespace TtsApi.Hubs.TtsHub
{
    //https://dotnetplaybook.com/which-is-best-websockets-or-signalr/
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
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
            TtsHandler.ClientDisconnected(Context.ConnectionId, Context.UserIdentifier);
            return base.OnDisconnectedAsync(exception);
        }

        public async Task ConfirmTtsFullyPlayed(string messageId)
        {
            Console.WriteLine($"Confirmed {Context.UserIdentifier} has fully played tts {messageId}");
            await _ttsHandler.ConfirmTtsFullyPlayed(Context.ConnectionId, Context.UserIdentifier, messageId);
        }

        public async Task ConfirmTtsSkipped(string messageId)
        {
            Console.WriteLine($"Confirmed {Context.UserIdentifier} has skipped tts {messageId}");
            await _ttsHandler.ConfirmTtsSkipped(Context.ConnectionId, Context.UserIdentifier, messageId);
        }

        public static async Task SendTtsRequest(IHubContext<TtsHub, ITtsHub> context, string roomId, TtsRequest request)
        {
            await context.Clients.Group(roomId).TtsPlayRequest(request);
        }
    }
}
