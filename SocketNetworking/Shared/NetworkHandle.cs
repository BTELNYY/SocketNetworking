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
    /// Relays information about who invoked a certain method via a <see cref="Attributes.PacketListener"/> or <see cref="Attributes.NetworkInvokable"/>.
    /// </summary>
    public class NetworkHandle
    {
        public NetworkHandle(NetworkClient client, Packet invocationPacket)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            InvocationPacket = invocationPacket ?? throw new ArgumentNullException(nameof(invocationPacket));
            if(invocationPacket is NetworkInvokationPacket packet)
            {
                InvocationMode = InvocationMode.RemoteProcedureCall;
            }
            else if(invocationPacket is CustomPacket customPacket)
            {
                InvocationMode = InvocationMode.Listener;
            }
            else
            {
                InvocationMode = InvocationMode.InternalCall;
            }
        }

        public NetworkHandle(NetworkClient client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public NetworkClient Client { get; } 

        public Packet InvocationPacket { get; }

        public InvocationMode InvocationMode { get; }

        public bool WasEncrypted
        {
            get
            {
                return InvocationPacket.Flags.HasFlag(PacketFlags.AsymetricalEncrypted) || InvocationPacket.Flags.HasFlag(PacketFlags.SymetricalEncrypted);
            }
        }
    }
}
