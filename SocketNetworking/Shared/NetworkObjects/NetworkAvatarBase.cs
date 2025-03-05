using SocketNetworking.Client;
using SocketNetworking.PacketSystem.Packets;
using SocketNetworking.Shared.SyncVars;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.Shared.NetworkObjects
{
    public class NetworkAvatarBase : NetworkObjectBase, INetworkAvatar
    {
        public string PublicKey => _pubKey.Value;

        private NetworkSyncVar<string> _pubKey;

        public NetworkAvatarBase()
        {
            _pubKey = new NetworkSyncVar<string>(this, OwnershipMode.Client);
        }

        public override void OnDisconnected(NetworkClient client)
        {
            base.OnDisconnected(client);
            if(client.ClientID == OwnerClientID)
            {
                this.NetworkDestroy();
            }
        }

        public override void OnNetworkSpawned(NetworkClient spawner)
        {
            base.OnNetworkSpawned(spawner);
            if(spawner.ClientID == OwnerClientID)
            {
                _pubKey.Value = spawner.EncryptionManager.OthersPublicKey;
            }
        }

        public override void OnLocalSpawned(ObjectManagePacket packet)
        {
            base.OnLocalSpawned(packet);
            if(NetworkManager.WhereAmI == ClientLocation.Local)
            {
                _provider.FromXmlString(NetworkClient.LocalClient.EncryptionManager.MyRSA.ToXmlString(true));
            }
        }

        public override void OnSyncVarsChanged()
        {
            base.OnSyncVarsChanged();
            _provider.FromXmlString(this.PublicKey);
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
