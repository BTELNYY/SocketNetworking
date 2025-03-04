using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Misc;
using SocketNetworking.PacketSystem;

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
