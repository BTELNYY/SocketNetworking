using SocketNetworking.Client;
using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.Shared
{
    /// <summary>
    /// Relays information about who invoked a certain method via a <see cref="Attributes.PacketListener"/> or <see cref="Attributes.NetworkInvocable"/>.
    /// </summary>
    public class NetworkHandle
    {
        public NetworkHandle(NetworkClient client, Packet invocationPacket)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            InvocationPacket = invocationPacket ?? throw new ArgumentNullException(nameof(invocationPacket));
            InvocationMode = invocationPacket is NetworkInvocationPacket packet ? InvocationMode.RemoteProcedureCall : InvocationMode.Listener;
        }

        internal NetworkHandle() { }

        public NetworkClient Client { get; internal set; } 

        public Packet InvocationPacket { get; internal set; }

        public InvocationMode InvocationMode { get; internal set; }

        public bool WasEncrypted
        {
            get
            {
                return InvocationPacket.Flags.HasFlag(PacketFlags.AsymtreicalEncrypted) || InvocationPacket.Flags.HasFlag(PacketFlags.SymetricalEncrypted);
            }
        }
    }
}
