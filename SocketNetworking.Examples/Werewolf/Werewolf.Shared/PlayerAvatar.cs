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
            _name = new NetworkSyncVar<string>(this, SocketNetworking.Shared.OwnershipMode.Server, "");
        }
    }
}
