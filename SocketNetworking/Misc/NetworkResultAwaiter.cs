﻿using SocketNetworking.Shared;
using SocketNetworking.Shared.PacketSystem.Packets;

namespace SocketNetworking.Misc
{
    public class NetworkResultAwaiter
    {
        public int CallbackID { get; private set; } = 0;

        public NetworkInvokationResultPacket ResultPacket { get; private set; }

        public bool HasResult
        {
            get
            {
                return ResultPacket != null;
            }
        }

        public NetworkResultAwaiter(int callBackID)
        {
            CallbackID = callBackID;
            NetworkManager.OnNetworkInvocationResult += NetworkInvocationResultArrived;
        }

        private void NetworkInvocationResultArrived(NetworkInvokationResultPacket obj)
        {
            if (obj.CallbackID != CallbackID)
            {
                return;
            }
            ResultPacket = obj;
        }

        void Consume()
        {
            if (ResultPacket != null)
            {
                NetworkManager.ConsumeNetworkInvokationResult(ResultPacket.CallbackID);
            }
        }

        ~NetworkResultAwaiter()
        {
            NetworkManager.OnNetworkInvocationResult -= NetworkInvocationResultArrived;
            CallbackID = 0;
            ResultPacket = null;
            Consume();
        }
    }
}
