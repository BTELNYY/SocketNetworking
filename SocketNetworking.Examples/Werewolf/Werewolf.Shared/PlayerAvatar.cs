using System;
using SocketNetworking;
using SocketNetworking.Misc.Console;
using SocketNetworking.Shared;
using SocketNetworking.Shared.Attributes;
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

        private NetworkSyncVar<Team> _team;

        public Team Team
        {
            get
            {
                return _team.Value;
            }
            set
            {
                _team.Value = value;
            }
        }

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

        public event Action<string> NameChanged;

        public event Action<Team> TeamChanged;

        public override void OnBeforeRegister()
        {
            base.OnBeforeRegister();
            _name = new NetworkSyncVar<string>(this, "", nameof(_name), OwnershipMode.Server);
            _name.Changed += (x) =>
            {
                if (NetworkManager.WhereAmI == ClientLocation.Local && x != "")
                {
                    NameChanged?.Invoke(x);
                }
            };
            _team = new NetworkSyncVar<Team>(this, Team.Spectators, nameof(_team), OwnershipMode.Server);
            _team.Changed += (x) =>
            {
                if (NetworkManager.WhereAmI == ClientLocation.Local)
                {
                    TeamChanged?.Invoke(x);
                }
            };
        }

        public void ClientSendMessage(string message)
        {
            this.ThrowIfNotClient();
            NetworkInvoke(ServerReceiveMessage, message);
        }

        [NetworkInvokable(Direction = NetworkDirection.Client)]
        private void ServerReceiveMessage(NetworkHandle handle, string message)
        {
            this.ThrowIfNotServer();
            string cleanMessage = FancyConsole.StripColor(message);
            if (_team.Value == Team.Spectators)
            {
                GameManager.Instance.ServerSendMessageToAll(this, cleanMessage, Team.Spectators);
                return;
            }
            switch (GameManager.Instance.Cycle)
            {
                case DayNightCycle.Night:
                    if (Team != Team.Werewolves)
                    {
                        NetworkInvoke(ClientReceiveMessage, "[Server]: You can't speak at night.");
                        return;
                    }
                    GameManager.Instance.ServerSendMessageToAll(this, message, Team.Werewolves);
                    break;
                case DayNightCycle.Day:
                    GameManager.Instance.ServerSendMessageToAll(this, message);
                    break;
                default:
                    NetworkInvoke(ClientReceiveMessage, "[Server]: You can't speak right now.");
                    break;
            }
        }

        public void ServerSendMessage(string message)
        {
            this.ThrowIfNotServer();
            NetworkInvoke(ClientReceiveMessage, message);
        }

        public event Action<NetworkHandle, string> MessageReceived;

        [NetworkInvokable(Direction = NetworkDirection.Server)]
        public void ClientReceiveMessage(NetworkHandle handle, string message)
        {
            MessageReceived?.Invoke(handle, message);
        }
    }
}
