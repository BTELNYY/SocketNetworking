﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.Exceptions
{
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
