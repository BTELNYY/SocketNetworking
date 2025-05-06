using System;
using SocketNetworking.Shared.PacketSystem;

namespace SocketNetworking.Shared.Exceptions
{
    /// <summary>
    /// The <see cref="CustomPacketCollisionException"/> is usually thrown when more than one <see cref="Packet"/> are trying to use the same ID.
    /// </summary>
    public class CustomPacketCollisionException : Exception
    {
        int packetId;

        Type collidingType;

        Type collider;

        public override string Message => GetMessage();

        string GetMessage()
        {
            string message = $"Custom packet ID collision error. Type {collidingType.FullName} already reserved ID {packetId} while {collider.FullName} is trying to take it.";
            return message;
        }

        public CustomPacketCollisionException(int packetid, Type collidingtype, Type collider)
        {
            packetId = packetid;
            collidingType = collidingtype;
            this.collider = collider;
        }
    }
}
