﻿using SocketNetworking.Misc;
using SocketNetworking.Shared.PacketSystem;

namespace SocketNetworking.Shared.Events
{
    public class PacketSendRequest : ChoiceEvent
    {
        public PacketSendRequest(Packet packet) : base(true)
        {
            Packet = packet;
        }

        public Packet Packet { get; }
    }
}
