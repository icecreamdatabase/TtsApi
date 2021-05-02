using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using TtsApi.Hubs.TransferClasses;
using TtsApi.Model;
using TtsApi.Model.Schema;

namespace TtsApi.Hubs
{
    //https://dotnetplaybook.com/which-is-best-websockets-or-signalr/
    public class TtsHub : Hub
    {
        private List<string> _connectedIds = new();
        private TtsDbContext _ttsDbContext;

        public TtsHub(TtsDbContext ttsDbContext)
        {
            _ttsDbContext = ttsDbContext;
        }

        public override async Task<Task> OnConnectedAsync()
        {
            if (!string.IsNullOrEmpty(Context.UserIdentifier))
                await Groups.AddToGroupAsync(Context.ConnectionId, Context.UserIdentifier);
            _connectedIds.Add(Context.ConnectionId);
            Console.WriteLine($"--> Connection Opened: {Context.ConnectionId} (roomId: {Context.UserIdentifier})");
            await Clients.Client(Context.ConnectionId).SendAsync("ReceiveConnID", Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            Console.WriteLine($"--> Connection Closed: {Context.ConnectionId} (roomId: {Context.UserIdentifier})");
            _connectedIds.Remove(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        public void ConfirmTtsFullyPlayed(string id)
        {
            Console.WriteLine($"Confirmed {Context.UserIdentifier} has fully played tts {id}");
        }

        public void ConfirmTtsSkipped(string id)
        {
            Console.WriteLine($"Confirmed {Context.UserIdentifier} has skipped tts {id}");
        }

        public static async Task SendTtsRequest(IHubContext<TtsHub> context, string roomId, TtsRequest request)
        {
            await context.Clients.Group(roomId).SendAsync("ReceiveTtsRequest", request);
        }

        public static async Task SendTtsRequest(IHubContext<TtsHub> context, RequestQueueIngest request)
        {
        }
    }
}
