using System.Security.Cryptography;
using SocketNetworking.Client;
using SocketNetworking.Shared.PacketSystem.Packets;
using SocketNetworking.Shared.SyncVars;

namespace SocketNetworking.Shared.NetworkObjects
{
    /// <summary>
    /// The <see cref="NetworkAvatarBase"/> is the recommended base class for <see cref="INetworkAvatar"/>s.
    /// </summary>
    public class NetworkAvatarBase : NetworkObjectBase, INetworkAvatar
    {
        public string PublicKey => _pubKey.Value;

        private NetworkSyncVar<string> _pubKey;

        /// <summary>
        /// This represents the ping or latency of the <see cref="NetworkClient"/> which owns this object. It is akin to <see cref="NetworkClient.Latency"/>, but represents the ping of someone other than the local client.
        /// </summary>
        public long OwnerLatency => _ping.Value;

        private NetworkSyncVar<long> _ping;

        public NetworkAvatarBase()
        {
            _pubKey = new NetworkSyncVar<string>(this, OwnershipMode.Client);
            _ping = new NetworkSyncVar<long>(this, OwnershipMode.Server, 0);
            _pubKey.Changed += (x) =>
            {
                if (x != null)
                {
                    _provider.FromXmlString(x);
                }
                else
                {
                    Log.GlobalDebug(x);
                }
            };
        }

        public override void OnOwnerDisconnected(NetworkClient client)
        {
            base.OnOwnerDisconnected(client);
            this.NetworkDestroy();
        }

        public override void OnOwnerNetworkSpawned(NetworkClient spawner)
        {
            base.OnOwnerNetworkSpawned(spawner);
            spawner.LatencyChanged += (x) =>
            {
                _ping.Value = x;
            };
        }

        public override void OnLocalSpawned(ObjectManagePacket packet)
        {
            base.OnLocalSpawned(packet);
            if (NetworkManager.WhereAmI == ClientLocation.Local)
            {
                _provider.FromXmlString(NetworkClient.LocalClient.EncryptionManager.MyRSA.ToXmlString(true));
            }
            if (NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                if (OwnerClient == null)
                {
                    this.NetworkDestroy();
                    return;
                }
                _provider.FromXmlString(OwnerClient.EncryptionManager.OthersPublicKey);
                _pubKey.ValueRaw = _provider.ToXmlString(false);
            }
        }

        public override void OnSyncVarChanged(NetworkClient client, INetworkSyncVar what)
        {
            base.OnSyncVarChanged(client, what);
        }

        RSACryptoServiceProvider _provider = new RSACryptoServiceProvider();

        /// <summary>
        /// This method is used by the owner of the object to decrypt data sent to it.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual byte[] Encrypt(byte[] data)
        {
            return _provider.Encrypt(data, RSAEncryptionPadding.Pkcs1);
        }

        /// <summary>
        /// This method is used to encrypt data to send to the owner of this object.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual byte[] Decrypt(byte[] data)
        {
            return _provider.Decrypt(data, RSAEncryptionPadding.Pkcs1);
        }

        public void ReceivePrivate(ClientToClientPacket data)
        {
            byte[] decrypted = Decrypt(data.Data);
            HandlePrivate(data, decrypted);
        }

        /// <summary>
        /// This method handles data which was encrypted using <see cref="PublicKey"/>. Called on the local client.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="decrypted"></param>
        protected virtual void HandlePrivate(ClientToClientPacket packet, byte[] decrypted)
        {

        }

        public void SendPrivate(byte[] data)
        {
            ClientToClientPacket packet = new ClientToClientPacket();
            packet.NetworkIDTarget = NetworkID;
            packet.Data = Encrypt(data);
            NetworkClient.LocalClient.Send(packet);
        }
    }
}
