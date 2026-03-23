using System;
using SocketNetworking;
using SocketNetworking.Shared;
using SocketNetworking.Shared.Attributes;
using SocketNetworking.Shared.NetworkObjects;
using SocketNetworking.Shared.Serialization;
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

        private NetworkSyncVar<Team> _team;

        public void ServerSetTeam(Team team)
        {
            this.ThrowIfNotServer();
            _team.Value = team;
        }

        public event Action<string> OnPlayerDeath;

        /// <summary>
        /// Kills the player.
        /// </summary>
        public void ServerKill(string reason)
        {
            this.ThrowIfNotServer();
            ServerSetTeam(Team.Spectators);
            NetworkInvoke(ClientOnKilled, reason);
        }

        [NetworkInvokable(Direction = NetworkDirection.Server)]
        private void ClientOnKilled(NetworkHandle handle, string reason)
        {
            OnPlayerDeath?.Invoke(reason);
        }

        public event Action OnPlayerTurn;

        /// <summary>
        /// Turns a <see cref="PlayerAvatar"/> into a werewolf.
        /// </summary>
        public void ServerTurn()
        {
            this.ThrowIfNotServer();
            ServerSetTeam(Team.Werewolves);
            NetworkInvoke(ClientOnTurned);
        }

        [NetworkInvokable(Direction = NetworkDirection.Server)]
        private void ClientOnTurned(NetworkHandle handle)
        {
            OnPlayerTurn?.Invoke();
        }

        public override void OnBeforeRegister()
        {
            base.OnBeforeRegister();
            _name = new NetworkSyncVar<string>(this, "", nameof(_name), OwnershipMode.Server);
            _name.Changed += (x) =>
            {
                if (NetworkManager.WhereAmI == ClientLocation.Local)
                {
                    Log.GlobalInfo($"Your name was updated to: " + x);
                }
            };
            _team = new NetworkSyncVar<Team>(this, Team.Neutral, nameof(_team), OwnershipMode.Server);
        }

        public void ClientSendMessage(string message)
        {
            NetworkInvoke(ServerReceiveMessage, message);
        }

        [NetworkInvokable(Direction = NetworkDirection.Client)]
        private void ServerReceiveMessage(NetworkHandle handle, string message)
        {

        }

        public void ServerSendMessage(string message, Team to = (Team)byte.MaxValue)
        {

        }

        [NetworkInvokable(Direction = NetworkDirection.Server)]
        private void ClientReceiveMessage(NetworkHandle handle, string message)
        {

        }
    }
}
