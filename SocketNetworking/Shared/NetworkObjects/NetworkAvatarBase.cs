using SocketNetworking.Client;
using SocketNetworking.Shared.PacketSystem.Packets;
using SocketNetworking.Shared.SyncVars;
using System.Security.Cryptography;

namespace SocketNetworking.Shared.NetworkObjects
{
    public class NetworkAvatarBase : NetworkObjectBase, INetworkAvatar
    {
        public string PublicKey => _pubKey.Value;

        private NetworkSyncVar<string> _pubKey;

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

        protected virtual byte[] Encrypt(byte[] data)
        {
            return _provider.Encrypt(data, RSAEncryptionPadding.Pkcs1);
        }

        protected virtual byte[] Decrypt(byte[] data)
        {
            return _provider.Decrypt(data, RSAEncryptionPadding.Pkcs1);
        }

        public void ReceivePrivate(ClientToClientPacket data)
        {
            byte[] decrypted = Decrypt(data.Data);
            HandlePrivate(data, decrypted);
        }

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
