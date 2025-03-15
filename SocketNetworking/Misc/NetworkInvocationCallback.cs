using SocketNetworking.Shared;
using SocketNetworking.Shared.Serialization;
using System;

namespace SocketNetworking.Misc
{
    public class NetworkInvocationCallback<T>
    {
        public NetworkInvocationCallback(int id)
        {
            CallbackIID = id;
            NetworkManager.OnNetworkInvocationResult += (x) =>
            {
                if (x.CallbackID == CallbackIID)
                {
                    if (!Cancelled)
                    {
                        object obj = ByteConvert.Deserialize(x.Result, out _);
                        Callback?.Invoke((T)obj);
                    }
                }
            };
        }

        public int CallbackIID { get; }

        public event Action<T> Callback;

        public bool Cancelled => _cancelled;

        public bool _cancelled;

        public void Cancel()
        {
            _cancelled = true;
        }
    }
}
