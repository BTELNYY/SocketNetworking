using SocketNetworking.Shared;
using SocketNetworking.Shared.PacketSystem.Packets;

namespace SocketNetworking.Misc
{
    /// <summary>
    /// The <see cref="NetworkResultAwaiter"/> class is responsible for waiting for results from network calls.
    /// </summary>
    public class NetworkResultAwaiter
    {
        /// <summary>
        /// Callback ID
        /// </summary>
        public int CallbackID { get; private set; } = 0;

        /// <summary>
        /// The <see cref="NetworkInvokationResultPacket"/> which may or may not be set.
        /// </summary>
        public NetworkInvokationResultPacket ResultPacket { get; private set; }

        /// <summary>
        /// Does the awaiter have a result?
        /// </summary>
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
