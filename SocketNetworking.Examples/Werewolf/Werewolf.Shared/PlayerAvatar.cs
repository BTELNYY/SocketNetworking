using SocketNetworking;
using SocketNetworking.Shared;
using SocketNetworking.Shared.NetworkObjects;
using SocketNetworking.Shared.SyncVars;

namespace Werewolf.Shared
{
    public class PlayerAvatar : NetworkAvatarBase
    {
        private NetworkSyncVar<string> _name;

        public string Name
        {
            get
            {
                return _name.Value;
            }
            set
            {
                _name.Value = value;
            }
        }

        public override void OnBeforeRegister()
        {
            base.OnBeforeRegister();
            _name = new NetworkSyncVar<string>(this, "", nameof(_name), SocketNetworking.Shared.OwnershipMode.Server);
            _name.Changed += (x) =>
            {
                if (NetworkManager.WhereAmI == ClientLocation.Local)
                {
                    Log.GlobalInfo($"Your name was updated to: " + x);
                }
            };
        }
    }
}
