using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.PacketSystem.Packets;

namespace SocketNetworking.Shared.NetworkObjects
{
    public interface INetworkAvatar : INetworkObject
    {
        string PublicKey { get; }

        void SendPrivate(byte[] data);

        void ReceivePrivate(ClientToClientPacket data);
    }
}
