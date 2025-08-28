using System;
using SocketNetworking.Client;
using SocketNetworking.Shared.PacketSystem;
using SocketNetworking.Shared.PacketSystem.Packets;

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
            if (invocationPacket is NetworkInvocationPacket packet)
            {
                InvocationMode = InvocationMode.RemoteProcedureCall;
            }
            else if (invocationPacket is CustomPacket customPacket)
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

        /// <summary>
        /// The <see cref="NetworkClient"/> which owns this handle.
        /// </summary>
        public NetworkClient Client { get; }

        /// <summary>
        /// The <see cref="Packet"/> which casued this handle to be generated.
        /// </summary>
        public Packet InvocationPacket { get; }

        /// <summary>
        /// The <see cref="Client"/>s <see cref="NetworkClient.ClientID"/>.
        /// </summary>
        public int ClientID
        {
            get
            {
                if (Client == null)
                {
                    return 0;
                }
                return Client.ClientID;
            }
        }

        /// <summary>
        /// The <see cref="ClientLocation"/> of the <see cref="Client"/>.
        /// </summary>
        public ClientLocation ClientLocation
        {
            get
            {
                return Client.CurrentClientLocation;
            }
        }

        /// <summary>
        /// The <see cref="SocketNetworking.Shared.InvocationMode"/> of the handle.
        /// </summary>
        public InvocationMode InvocationMode { get; }

        /// <summary>
        /// Determiens if the <see cref="InvocationPacket"/> was encrypted by checking its <see cref="Packet.Flags"/> for <see cref="PacketFlags.AsymmetricalEncrypted"/> or <see cref="PacketFlags.SymmetricalEncrypted"/>.
        /// </summary>
        public bool WasEncrypted
        {
            get
            {
                return InvocationPacket.Flags.HasFlag(PacketFlags.AsymmetricalEncrypted) || InvocationPacket.Flags.HasFlag(PacketFlags.SymmetricalEncrypted);
            }
        }

        /// <summary>
        /// Sends a packet to the <see cref="Client"/>.
        /// </summary>
        /// <param name="packet"></param>
        public void Send(Packet packet)
        {
            Client.Send(packet);
        }

        /// <summary>
        /// Sends a packet to the <see cref="Client"/>.
        /// </summary>
        /// <param name="packet"></param>
        public void SendImmediate(Packet packet)
        {
            Client.SendImmediate(packet);
        }

        /// <summary>
        /// Disconnects the <see cref="Client"/>
        /// </summary>
        public void Disconnect()
        {
            Client.Disconnect();
        }

        /// <summary>
        /// Disconnects the <see cref="Client"/> with a <paramref name="reason"/>.
        /// </summary>
        /// <param name="reason"></param>
        public void Disconnect(string reason)
        {
            Client.Disconnect(reason);
        }

        public override string ToString()
        {
            return $"ClientID: {ClientID}, Packet: {InvocationPacket}";
        }
    }
}
