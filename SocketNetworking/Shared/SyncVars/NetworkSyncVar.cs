using SocketNetworking.Client;
using SocketNetworking.Exceptions;
using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.Packets;
using SocketNetworking.Server;
using System;
using System.Collections.Generic;
using SocketNetworking.Shared.NetworkObjects;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.Shared.SyncVars
{
    public class NetworkSyncVar<T> : IEquatable<T>, ICloneable, INetworkSyncVar
    {
        public INetworkObject OwnerObject { get; }

        /// <summary>
        /// Sets who is allowed to set the value of this Sync var.
        /// </summary>
        public OwnershipMode SyncOwner { get; }

        T value = default(T);

        public virtual T Value
        {
            get
            {
                return value;
            }
            set
            {
                this.value = value;
                ValueRaw = value;
            }
        }

        public object ValueRaw 
        {
            get => (object)Value;
            set
            {
                this.value = (T)value;
                Sync();
            }
        }

        void Sync()
        {
            SerializedData data = NetworkConvert.Serialize(value);
            SyncVarData syncVarData = new SyncVarData()
            {
                NetworkIDTarget = OwnerObject.NetworkID,
                Data = data,
                TargetVar = Name,
            };
            SyncVarUpdatePacket packet = new SyncVarUpdatePacket()
            {
                Data = new List<SyncVarData> { syncVarData },
            };
            if(NetworkManager.WhereAmI == ClientLocation.Local)
            {
                if(NetworkClient.LocalClient == null)
                {
                    throw new InvalidOperationException("Tried to modify a SyncVar while the local client was null!");
                }
                if ((SyncOwner == OwnershipMode.Client && OwnerObject.OwnerClientID != NetworkClient.LocalClient.ClientID) || SyncOwner == OwnershipMode.Server)
                {
                    throw new InvalidOperationException("Tried to modify a SyncVar without permission.");
                }
                NetworkClient.LocalClient.Send(packet);
            }
            if(NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                if(SyncOwner == OwnershipMode.Client)
                {
                    if(OwnerObject.ObjectVisibilityMode == ObjectVisibilityMode.OwnerAndServer)
                    {
                        NetworkClient owner = NetworkServer.GetClient(OwnerObject.OwnerClientID);
                        if(owner == null)
                        {
                            throw new InvalidOperationException("Can't find the owner of this object!");
                        }
                        owner.Send(packet);
                    }
                    else if(OwnerObject.ObjectVisibilityMode == ObjectVisibilityMode.Everyone)
                    {
                        NetworkServer.SendToAll(packet);
                    }
                }
                else
                {
                    NetworkServer.SendToAll(packet);
                }
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }



        string _name = string.Empty;

        public bool Equals(T other)
        {
            return other.Equals(value);
        }

        public object Clone()
        {
            return new NetworkSyncVar<T>(OwnerObject, SyncOwner, value);
        }

        public void RawSet(object value, NetworkClient who)
        {
            if(value is T t)
            {
                this.value = t;
            }
        }

        public NetworkSyncVar(INetworkObject owner, T value)
        {
            OwnerObject = owner;
            this.value = value;
            SyncOwner = OwnershipMode.Server;
        }

        public NetworkSyncVar(INetworkObject ownerObject, OwnershipMode syncDirection, T value)
        {
            OwnerObject = ownerObject;
            SyncOwner = syncDirection;
            this.value = value;
        }

        public NetworkSyncVar(INetworkObject ownerObject, OwnershipMode syncOwner, T value, string name) : this(ownerObject, syncOwner, value)
        {
            _name = name;
        }

        public NetworkSyncVar(INetworkObject ownerObject, T value, string name) : this(ownerObject, value)
        {
            _name = name;
        }
    }
}
