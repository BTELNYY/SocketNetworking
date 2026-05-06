using System;
using System.Linq;
using SocketNetworking;
using SocketNetworking.Client;
using SocketNetworking.Misc.Console;
using SocketNetworking.Server;
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

        private NetworkSyncVar<int> _vote;

        public int Vote
        {
            get
            {
                return _vote.Value;
            }
            set
            {
                _vote.Value = value;
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

        private NetworkSyncVar<Team> _originalTeam;

        public Team OriginalTeam
        {
            get
            {
                return _originalTeam.Value;
            }
            set
            {
                _originalTeam.Value = value;
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
            _originalTeam.Value = _team.Value;
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

        private bool _joinSent = false;

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
                else if (x != "")
                {
                    if (!_joinSent)
                    {
                        _joinSent = true;
                        GameManager.Instance.ServerSendBroadcast($"{FancyConsole.SpecialMarker}e{(OwnerAvatar as PlayerAvatar).Name} joined.");
                    }
                    else
                    {
                        GameManager.Instance.ServerSendBroadcast($"{FancyConsole.SpecialMarker}e{(OwnerAvatar as PlayerAvatar).Name} changed their name.");
                    }
                }
            };
            _team = new NetworkSyncVar<Team>(this, Team.Spectators, nameof(_team), OwnershipMode.Server);
            _team.VisibilityMode = ObjectVisibilityMode.OwnerAndServer;
            _team.Changed += (x) =>
            {
                if (NetworkManager.WhereAmI == ClientLocation.Local)
                {
                    TeamChanged?.Invoke(x);
                }
            };
            _vote = new NetworkSyncVar<int>(this, 0, nameof(_vote), OwnershipMode.Client);
            _originalTeam = new NetworkSyncVar<Team>(this, Team.Spectators, nameof(_originalTeam), OwnershipMode.Server);
        }

        public override void OnOwnerDisconnected(NetworkClient client)
        {
            base.OnOwnerDisconnected(client);
            PlayerAvatar avatar = client.Avatar as PlayerAvatar;
            GameManager.Instance.ServerSendBroadcast($"{FancyConsole.SpecialMarker}e{(avatar).Name} left.");
        }

        public void ClientSendMessage(string message)
        {
            this.ThrowIfNotClient();
            NetworkInvoke(ServerReceiveMessage, message);
        }

        private void HandleCommand(NetworkHandle handle, string command)
        {
            string[] parts = command.Split(" ").ToArray();
            //simple command system.
            switch (parts[0])
            {
                case "help":
                    ServerSendMessage("[Server] Help:\n\t- /vote <playerId>: Vote for someone to be executed, or eaten.\n\t- /list: lists all players.\n\t- /help: Print this menu.");
                    break;
                case "vote":
                    if (parts.Length < 2)
                    {
                        ServerSendMessage($"{FancyConsole.BuildColor(ConsoleColor.Red)}[Server]: Not enough arguments!{FancyConsole.BuildColor(ConsoleColor.White)}");
                        return;
                    }
                    if (GameManager.Instance.Cycle != DayNightCycle.Day || GameManager.Instance.Cycle != DayNightCycle.Night)
                    {
                        ServerSendMessage($"{FancyConsole.BuildColor(ConsoleColor.Red)}[Server]: You can't do that.{FancyConsole.BuildColor(ConsoleColor.White)}");
                        return;
                    }
                    if (Team == Team.Spectators)
                    {
                        ServerSendMessage($"{FancyConsole.BuildColor(ConsoleColor.Red)}[Server]: Dead men tell no tales.{FancyConsole.BuildColor(ConsoleColor.White)}");
                        return;
                    }
                    if (Team != Team.Werewolves && GameManager.Instance.Cycle == DayNightCycle.Night)
                    {
                        ServerSendMessage($"{FancyConsole.BuildColor(ConsoleColor.Red)}[Server]: You are sleeping.{FancyConsole.BuildColor(ConsoleColor.White)}");
                        return;
                    }
                    if (!int.TryParse(parts[1], out int value))
                    {
                        ServerSendMessage($"{FancyConsole.BuildColor(ConsoleColor.Red)}[Server]: Bad client ID. Hint: Its the number beside their chat messages, use /list if you need a list..{FancyConsole.BuildColor(ConsoleColor.White)}");
                        return;
                    }
                    WerewolfClient target = (WerewolfClient)NetworkServer.GetClient(value);
                    if (target == null)
                    {
                        ServerSendMessage($"{FancyConsole.BuildColor(ConsoleColor.Red)}[Server]: Bad client ID. Hint: Its the number beside their chat messages, use /list if you need a list..{FancyConsole.BuildColor(ConsoleColor.White)}");
                        return;
                    }
                    Vote = value;
                    ServerSendMessage($"[Server] Voted for {target.PlayerAvatar.Name}.");
                    break;
                case "list":
                    string computed = "";
                    switch (Team)
                    {
                        case Team.Spectators:
                            foreach (PlayerAvatar avatar in NetworkServer.Clients.Cast<WerewolfClient>().Select(x => x.PlayerAvatar))
                            {
                                computed += $"Name: {FancyConsole.SpecialMarker}{GameManager.GetTeamColor(avatar.Team)}{avatar.Name}{FancyConsole.BuildColor(ConsoleColor.White)}, ID: {avatar.OwnerClientID}\n";
                            }
                            ServerSendMessage(computed);
                            break;
                        default:
                            foreach (PlayerAvatar avatar in NetworkServer.Clients.Cast<WerewolfClient>().Select(x => x.PlayerAvatar))
                            {
                                computed += $"Name: {avatar.Name}, ID: {avatar.OwnerClientID}\n";
                            }
                            ServerSendMessage(computed);
                            break;
                    }
                    break;
            }
        }

        [NetworkInvokable(Direction = NetworkDirection.Client)]
        private void ServerReceiveMessage(NetworkHandle handle, string message)
        {
            this.ThrowIfNotServer();
            if (string.IsNullOrEmpty(message))
            {
                return;
            }
            string cleanMessage = FancyConsole.StripColor(message);
            if (cleanMessage.StartsWith('/'))
            {
                HandleCommand(handle, cleanMessage.Substring(1));
                return;
            }
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
