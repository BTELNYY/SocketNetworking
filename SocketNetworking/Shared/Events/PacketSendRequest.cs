using SocketNetworking.Misc;
using SocketNetworking.Shared.PacketSystem;

namespace SocketNetworking.Shared.Events
{
    /// <summary>
    /// The <see cref="PacketSendRequest"/> <see cref="ChoiceEvent"/> determines if a packet is sent.
    /// </summary>
    public class PacketSendRequest : ChoiceEvent
    {
        public PacketSendRequest(Packet packet) : base(true)
        {
            Packet = packet;
        }

        public Packet Packet { get; }
    }
}
