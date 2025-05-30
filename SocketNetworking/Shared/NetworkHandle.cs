﻿using System;
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
    }
}
