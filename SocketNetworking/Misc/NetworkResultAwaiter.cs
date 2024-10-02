using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.PacketSystem.Packets;
using SocketNetworking.Shared;

namespace SocketNetworking.Misc
{
    public class NetworkResultAwaiter
    {
        public int CallbackID { get; private set; } = 0;

        public NetworkInvocationResultPacket ResultPacket { get; private set; }

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

        private void NetworkInvocationResultArrived(NetworkInvocationResultPacket obj)
        {
            if(obj.CallbackID != CallbackID)
            {
                return;
            }
            ResultPacket = obj;
        }
        
        void Consume()
        {
            if(ResultPacket != null)
            {
                NetworkManager.ConsumeNetworkInvocationResult(ResultPacket.CallbackID);
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
