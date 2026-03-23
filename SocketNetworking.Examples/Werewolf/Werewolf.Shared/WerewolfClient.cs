using System;
using SocketNetworking.Client;
using SocketNetworking.Shared;
using SocketNetworking.Shared.Attributes;
using SocketNetworking.Shared.Serialization;

namespace Werewolf.Shared
{
    public class WerewolfClient : TcpNetworkClient
    {
        public PlayerAvatar PlayerAvatar
        {
            get
            {
                return Avatar as PlayerAvatar;
            }
        }

        public override void InitLocalClient()
        {
            base.InitLocalClient();
            AvatarChanged += WerewolfClient_AvatarChanged;
        }

        private void WerewolfClient_AvatarChanged(SocketNetworking.Shared.NetworkObjects.INetworkAvatar obj)
        {
            if (CurrentClientLocation != ClientLocation.Local)
            {
                return;
            }
            NetworkInvokeOnClient((Action<NetworkHandle, string>)SetName, ClientName);
        }

        [NetworkInvokable(Direction = NetworkDirection.Client)]
        private void SetName(NetworkHandle handle, string name)
        {
            PlayerAvatar.Name = name;
        }

        public string ClientName = string.Empty;
    }
}
