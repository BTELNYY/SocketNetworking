using System;
using SocketNetworking.Shared;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Misc
{
    /// <summary>
    /// The <see cref="NetworkInvocationCallback{T}"/> class is responsible for managing the response of a Network Invocation.
    /// </summary>
    /// <typeparam name="T"></typeparam>
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

        /// <summary>
        /// The CallbackID
        /// </summary>
        public int CallbackIID { get; }

        /// <summary>
        /// The <see cref="Action{T}"/> which represents what to do when the callback is called.
        /// </summary>
        public event Action<T> Callback;

        /// <summary>
        /// If the callback has been cancelled.
        /// </summary>
        public bool Cancelled => _cancelled;

        public bool _cancelled;

        /// <summary>
        /// Cancels the callback and prevents <see cref="Callback"/> from being called.
        /// </summary>
        public void Cancel()
        {
            _cancelled = true;
        }
    }
}
