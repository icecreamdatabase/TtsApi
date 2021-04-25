using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using TtsApi.Model;

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

        public override Task OnConnectedAsync()
        {
            HttpContext httpContext = Context.GetHttpContext();
            Console.WriteLine($"--> Connection Opened: {Context.ConnectionId} ({Context.UserIdentifier})") ;
            Clients.Client(Context.ConnectionId).SendAsync("ReceiveConnID", Context.ConnectionId);
            _connectedIds.Add(Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            ClaimsIdentity identity = (ClaimsIdentity)Context.User?.Identity;
            Console.WriteLine($"--> Connection Closed: {Context.ConnectionId} ({Context.UserIdentifier})") ;
            Console.WriteLine(identity?.Name);
            _connectedIds.Remove(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        public async Task Register(string roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            await Clients.Caller.SendAsync("ReceiveMessage", "ok");
        }

        public async Task SendMessageAsync(string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", message);
        }

        public static async Task SendToChannel(IHubContext<TtsHub> context, string roomId, string message)
        {
            await context.Clients.Group(roomId).SendAsync("ReceiveMessage", message);
        }
    }
}
