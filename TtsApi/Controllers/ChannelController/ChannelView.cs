using System;
using System.Diagnostics.CodeAnalysis;
using TtsApi.Model.Schema;

namespace TtsApi.Controllers.ChannelController
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class ChannelView
    {
        public int RoomId { get; }
        public string ChannelName { get; }
        public bool IsTwitchPartner { get; }
        public int MaxIrcMessageLength { get; }
        public int MaxMessageTimeSeconds { get; }
        public int MaxTtsCharactersPerRequest { get; }
        public int MinCooldown { get; }
        public DateTime AddDate { get; }
        public bool IrcMuted { get; }
        public bool IsQueueMessages { get; }
        public bool AllowNeuralVoices { get; }
        public int Volume { get; }
        public bool AllModsAreEditors { get; }

        public ChannelView(Channel channel)
        {
            RoomId = channel.RoomId;
            ChannelName = channel.ChannelName;
            IsTwitchPartner = channel.IsTwitchPartner;
            MaxIrcMessageLength = channel.MaxIrcMessageLength;
            MaxMessageTimeSeconds = channel.MaxMessageTimeSeconds;
            MaxTtsCharactersPerRequest = channel.MaxTtsCharactersPerRequest;
            MinCooldown = channel.MinCooldown;
            AddDate = channel.AddDate;
            IrcMuted = channel.IrcMuted;
            IsQueueMessages = channel.IsQueueMessages;
            AllowNeuralVoices = channel.AllowNeuralVoices;
            Volume = channel.Volume;
            AllModsAreEditors = channel.AllModsAreEditors;
        }
    }
}
