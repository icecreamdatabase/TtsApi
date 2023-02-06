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
        private readonly TtsRequestHandler _ttsRequestHandler;

        public TtsHub(TtsDbContext ttsDbContext, TtsRequestHandler ttsRequestHandler)
        {
            _ttsDbContext = ttsDbContext;
            _ttsRequestHandler = ttsRequestHandler;
        }

        public override async Task<Task> OnConnectedAsync()
        {
            if (string.IsNullOrEmpty(Context.UserIdentifier))
                throw new Exception($"{nameof(Context.UserIdentifier)} is null or empty");

            //bool xxx = Context.User?.HasClaim("xxx", "true") ?? false;

            await Groups.AddToGroupAsync(Context.ConnectionId, Context.UserIdentifier);
            TtsRequestHandler.ConnectClients.Add(Context.ConnectionId, Context.UserIdentifier);
            Console.WriteLine($"--> Connection Opened: {Context.ConnectionId} (roomId: {Context.UserIdentifier})");
            await Clients.Client(Context.ConnectionId).ConnId(Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            if (string.IsNullOrEmpty(Context.UserIdentifier))
                throw new Exception($"{nameof(Context.UserIdentifier)} is null or empty");

            Console.WriteLine($"--> Connection Closed: {Context.ConnectionId} (roomId: {Context.UserIdentifier})");
            TtsRequestHandler.ConnectClients.Remove(Context.ConnectionId);
            TtsRequestHandler.ClientDisconnected(Context.ConnectionId, Context.UserIdentifier);
            return base.OnDisconnectedAsync(exception);
        }

        public async Task ConfirmTtsFullyPlayed(string redemptionId)
        {
            if (string.IsNullOrEmpty(Context.UserIdentifier))
                throw new Exception($"{nameof(Context.UserIdentifier)} is null or empty");

            Console.WriteLine($"Confirmed {Context.UserIdentifier} has fully played tts {redemptionId}");
            await _ttsRequestHandler.ConfirmTtsFullyPlayed(Context.ConnectionId, Context.UserIdentifier, redemptionId);
        }

        public async Task ConfirmTtsSkipped(string redemptionId)
        {
            if (string.IsNullOrEmpty(Context.UserIdentifier))
                throw new Exception($"{nameof(Context.UserIdentifier)} is null or empty");

            Console.WriteLine($"Confirmed {Context.UserIdentifier} has skipped tts {redemptionId}");
            await _ttsRequestHandler.ConfirmTtsSkipped(Context.ConnectionId, Context.UserIdentifier, redemptionId);
        }

        public static async Task SendTtsRequest(IHubContext<TtsHub, ITtsHub> context, string roomId, TtsRequest request)
        {
            await context.Clients.Group(roomId).TtsPlayRequest(request);
        }
    }
}
