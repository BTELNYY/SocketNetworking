using SocketNetworking.Client;
using SocketNetworking.Shared;
using SocketNetworking.Shared.Attributes;

namespace Werewolf.Shared
{
    public class WerewolfClient : TcpNetworkClient
    {
        public const int MAX_NAME_LENGTH = 32;

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
            NetworkInvokeOnClient(SetName, ClientName);
        }

        [NetworkInvokable(Direction = NetworkDirection.Client)]
        private void SetName(NetworkHandle handle, string name)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            {
                Disconnect("Name cannot be null, empty, or whitespace.");
                return;
            }
            if (name.Length > MAX_NAME_LENGTH)
            {
                Disconnect($"Name is too long. Maximum {MAX_NAME_LENGTH} characters.");
                return;
            }
            PlayerAvatar.Name = name;
        }

        public string ClientName = string.Empty;
    }
}
