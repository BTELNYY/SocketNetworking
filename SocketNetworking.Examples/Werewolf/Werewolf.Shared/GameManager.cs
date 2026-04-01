using System;
using System.Collections.Generic;
using System.Linq;
using SocketNetworking;
using SocketNetworking.Client;
using SocketNetworking.Misc;
using SocketNetworking.Misc.Console;
using SocketNetworking.Server;
using SocketNetworking.Shared.NetworkObjects;
using SocketNetworking.Shared.SyncVars;

namespace Werewolf.Shared
{
    public class GameManager : NetworkObjectBase
    {
        public static GameManager Instance { get; private set; }

        //So WerewolfRatio / MinimumPlayers
        public static int MinimumPlayers = 5;

        public static int WerewolfRatio = 1;

        private NetworkSyncVar<bool> _gameRunning;

        public bool GameRunning
        {
            get
            {
                return _gameRunning.Value;
            }
            set
            {
                _gameRunning.Value = value;
            }
        }

        private NetworkSyncVar<DayNightCycle> _cycle;

        public DayNightCycle Cycle
        {
            get
            {
                return _cycle.Value;
            }
            set
            {
                _cycle.Value = value;
            }
        }

        private NetworkSyncVar<int> _nights;

        public int Nights
        {
            get
            {
                return _nights.Value;
            }
            set
            {
                _nights.Value = value;
            }
        }

        public override void OnBeforeRegister()
        {
            base.OnBeforeRegister();
            if (Instance == null)
            {
                Instance = this;
            }
            _cycle = new NetworkSyncVar<DayNightCycle>(this, DayNightCycle.Dawn, nameof(_cycle), SocketNetworking.Shared.OwnershipMode.Server);
            _cycle.Changed += (x) =>
            {
                DayNightCycleUpdated?.Invoke();
            };
            _nights = new NetworkSyncVar<int>(this, 0, nameof(_nights), SocketNetworking.Shared.OwnershipMode.Server);
            _gameRunning = new NetworkSyncVar<bool>(this, false, nameof(_gameRunning), SocketNetworking.Shared.OwnershipMode.Server);
        }

        public void ServerStartRound()
        {
            this.ThrowIfNotServer();
            if (NetworkServer.Clients.Where(x => x.Ready).Count() < MinimumPlayers)
            {
                Log.GlobalInfo("Not enough players to start.");
                return;
            }
            _cycle.Value = DayNightCycle.Dawn;
            _nights.Value = 0;
            _gameRunning.Value = true;
            foreach (PlayerAvatar avatar in NetworkServer.Clients.Cast<WerewolfClient>().Select(x => x.PlayerAvatar))
            {
                avatar.ServerSetTeam(Team.Villagers);
                avatar.OriginalTeam = Team.Villagers;
            }
            Random random = new Random();
            List<PlayerAvatar> werewolves = NetworkServer.Clients.Cast<WerewolfClient>().Select(x => x.PlayerAvatar).OrderBy(x => random.Next()).Take((WerewolfRatio / MinimumPlayers) * NetworkServer.Clients.Count).ToList();
            foreach (PlayerAvatar werewolf in werewolves)
            {
                werewolf.ServerSetTeam(Team.Werewolves);
                werewolf.OriginalTeam = Team.Werewolves;
            }
            _logicCallback = new CallbackTimer<GameManager>((manager) =>
            {
                if (!GameRunning)
                {
                    return;
                }
                if (NetworkServer.Clients.Where(x => x.Ready).Count() < MinimumPlayers)
                {
                    ServerSendBroadcast("Game cancelled. Not enough players!");
                    return;
                }
                manager.RunLogic();
            }, this, CycleTime[Cycle]);
            _logicCallback.Start();
        }

        public Dictionary<DayNightCycle, int> CycleTime = new Dictionary<DayNightCycle, int>()
        {
            [DayNightCycle.Dawn] = 5,
            [DayNightCycle.Day] = 240,
            [DayNightCycle.Dusk] = 5,
            [DayNightCycle.Night] = 120,
        };

        private CallbackTimer<GameManager> _logicCallback;

        /// <summary>
        /// Advance the phase of the day.
        /// </summary>
        public void Advance()
        {
            RunVictoryConditions();
            _logicCallback = new CallbackTimer<GameManager>((a) =>
            {
                if (!GameRunning)
                {
                    return;
                }
                a.RunLogic();
            }, this, CycleTime[Cycle]);
            _logicCallback.Start();
        }

        private void RunVictoryConditions()
        {
            this.ThrowIfNotServer();
            int werewolfCount = NetworkServer.Clients.Cast<WerewolfClient>().Where(x => x.PlayerAvatar.Team == Team.Werewolves).Count();
            int otherCount = NetworkServer.Clients.Cast<WerewolfClient>().Where(x => x.PlayerAvatar.Team != Team.Werewolves).Count();
            if (werewolfCount >= otherCount)
            {
                Win(Team.Werewolves);
                return;
            }
            if (werewolfCount == 0)
            {
                Win(Team.Villagers);
                return;
            }
        }

        public void Win(Team team)
        {
            this.ThrowIfNotServer();
            switch (team)
            {
                case Team.Werewolves:
                    ServerSendBroadcast($"{FancyConsole.BuildColor(ConsoleColor.Red)}Game over, werewolves win. You are all going to die.{FancyConsole.BuildColor(ConsoleColor.White)}");
                    ServerEndRound();
                    break;
                case Team.Villagers:
                    ServerSendBroadcast($"{FancyConsole.BuildColor(ConsoleColor.Green)}Game over, Villagers win, all werewolves were executed.{FancyConsole.BuildColor(ConsoleColor.White)}");
                    ServerEndRound();
                    break;
                default:
                    break;
            }
        }

        public void ServerEndRound()
        {
            this.ThrowIfNotServer();
            _gameRunning.Value = false;
            ServerSendBroadcast($"Round over. Took {_nights.Value} nights.");
            foreach (PlayerAvatar p in NetworkServer.Clients.Cast<WerewolfClient>().Select(x => x.PlayerAvatar))
            {
                if (p.OriginalTeam == Team.Spectators)
                {
                    continue;
                }
                ServerSendBroadcast($"{p.Name} was a {FancyConsole.SpecialMarker}{GetTeamColor(p.OriginalTeam)}{p.OriginalTeam}{FancyConsole.BuildColor(ConsoleColor.White)}.");
                p.ServerSetTeam(Team.Spectators);
                p.OriginalTeam = Team.Spectators;
            }
            _logicCallback?.Abort();
        }

        private void RunLogic()
        {
            foreach (PlayerAvatar p in NetworkServer.Clients.Cast<WerewolfClient>().Select(x => x.PlayerAvatar))
            {
                p.Vote = -1;
            }
            switch (Cycle)
            {
                case DayNightCycle.Dawn:
                    Cycle = DayNightCycle.Day;
                    OnDayBegin();
                    break;
                case DayNightCycle.Day:
                    Cycle = DayNightCycle.Dusk;
                    OnDuskBegin();
                    break;
                case DayNightCycle.Dusk:
                    Cycle = DayNightCycle.Night;
                    OnNightBegin();
                    break;
                case DayNightCycle.Night:
                    Cycle = DayNightCycle.Dawn;
                    OnDawnBegin();
                    break;
            }
        }

        public override void OnReady(NetworkClient client, bool isReady)
        {
            base.OnReady(client, isReady);
            if (isReady)
            {
                if (NetworkServer.Clients.Where(x => x.Ready).Count() >= MinimumPlayers)
                {
                    ServerSendBroadcast("Round starting in 30 seconds.");
                    CallbackTimer<GameManager> timer = new CallbackTimer<GameManager>(x =>
                    {
                        x.ServerStartRound();
                    }, this, 30f);
                }
                else
                {
                    ServerSendBroadcast($"Not enough players to start ({NetworkServer.Clients.Count(x => x.Ready)}/{MinimumPlayers})");
                }
            }
        }

        private void OnDayBegin()
        {
            ServerSendBroadcast("It is now day again.");
            ServerSendBroadcast($"You have {CycleTime[Cycle]} seconds, vote someone to be executed using /vote!");
            Advance();
        }

        private void OnDuskBegin()
        {
            int deadVillager = GetLivingVotes();
            ServerSendBroadcast("Darkness falls upon the village, it is dusk.");
            if (deadVillager == -1)
            {
                ServerSendBroadcast("Nobody is executed.");
            }
            else
            {
                PlayerAvatar avatar = NetworkServer.GetClient(deadVillager)?.Avatar as PlayerAvatar;
                if (avatar == null)
                {
                    ServerSendBroadcast("Nobody is executed.");
                }
                else
                {
                    ServerSendBroadcast($"{avatar.Name} is hung by their neck. They were a {FancyConsole.SpecialMarker}{GetTeamColor(avatar.Team)}{avatar.Team}{FancyConsole.BuildColor(ConsoleColor.White)}");
                    avatar.ServerKill("Executed.");
                }
            }
            Advance();
        }

        private void OnNightBegin()
        {
            ServerSendBroadcast("The night has begun, are you sure you are safe?");
            ServerSendBroadcastToTeam($"{FancyConsole.SpecialMarker}{GetTeamColor(Team.Werewolves)}Werewolves, {FancyConsole.SpecialMarker}fnight has fallen. Use /vote <playerId> to vote for who to kill.", Team.Werewolves);
            Advance();
        }

        private void OnDawnBegin()
        {
            int deadVillager = GetWerewolfVotes();
            ServerSendBroadcast("The sun rises in the east, creeping over the trees and waking you.");
            if (deadVillager == -1)
            {
                ServerSendBroadcast("You find nobody dead.");
            }
            else
            {
                PlayerAvatar avatar = NetworkServer.GetClient(deadVillager)?.Avatar as PlayerAvatar;
                if (avatar == null)
                {
                    ServerSendBroadcast("You find nobody dead.");
                }
                else
                {
                    avatar.ServerKill("Eaten by werewolves.");
                    ServerSendBroadcast($"You find {avatar.Name} dead.");
                }
            }
            Advance();
        }

        private int GetLivingVotes()
        {
            Dictionary<int, int> idsToVotes = new Dictionary<int, int>();
            foreach (PlayerAvatar avatar in NetworkServer.Clients.Cast<WerewolfClient>().Select(x => x.PlayerAvatar).Where(x => x.Team != Team.Spectators))
            {
                if (idsToVotes.ContainsKey(avatar.Vote))
                {
                    idsToVotes[avatar.Vote]++;
                }
                else
                {
                    idsToVotes.Add(avatar.Vote, 1);
                }
            }
            List<KeyValuePair<int, int>> sorted = (from entry in idsToVotes orderby entry.Value ascending select entry).ToList();
            if (sorted[0].Value == sorted[1].Value)
            {
                return -1;
            }
            return sorted[0].Key;
        }

        private int GetWerewolfVotes()
        {
            Dictionary<int, int> idsToVotes = new Dictionary<int, int>();
            foreach (PlayerAvatar avatar in NetworkServer.Clients.Cast<WerewolfClient>().Select(x => x.PlayerAvatar).Where(x => x.Team == Team.Werewolves))
            {
                if (idsToVotes.ContainsKey(avatar.Vote))
                {
                    idsToVotes[avatar.Vote]++;
                }
                else
                {
                    idsToVotes.Add(avatar.Vote, 1);
                }
            }
            List<KeyValuePair<int, int>> sorted = (from entry in idsToVotes orderby entry.Value ascending select entry).ToList();
            if (sorted[0].Value == sorted[1].Value)
            {
                return -1;
            }
            return sorted[0].Key;
        }

        public event Action DayNightCycleUpdated;

        public static char GetTeamColor(Team team)
        {
            switch (team)
            {
                case Team.Werewolves:
                    return 'c';
                case Team.Spectators:
                    return '8';
                case Team.Villagers:
                    return 'a';
                default:
                    return 'f';
            }
        }

        public void ServerSendMessageToAll(PlayerAvatar avatar, string message, Team to = Team.Everyone)
        {
            this.ThrowIfNotServer();
            message = $"<{avatar.Name} ({avatar.OwnerClient.ClientID})>: {message}";
            if (to == Team.Everyone)
            {
                NetworkServer.NetworkInvokeOnAll(avatar.ClientReceiveMessage, (x => true), message);
                return;
            }
            NetworkServer.NetworkInvokeOnAll(avatar.ClientReceiveMessage, (x => x.Avatar is PlayerAvatar pl && pl.Team == to), message);
        }

        public void ServerSendBroadcast(string message)
        {
            foreach (PlayerAvatar avatar in NetworkServer.Clients.Cast<WerewolfClient>().Select(x => x.PlayerAvatar))
            {
                avatar.ServerSendMessage($"[Server]: {message}");
            }
        }

        public void ServerSendBroadcastToTeam(string message, Team to)
        {
            foreach (PlayerAvatar avatar in NetworkServer.Clients.Cast<WerewolfClient>().Select(x => x.PlayerAvatar).Where(x => x.Team == to))
            {
                avatar.ServerSendMessage($"[Server]: {message}");
            }
        }
    }

    public enum DayNightCycle : byte
    {
        Dawn = 0,
        Day = 1,
        Dusk = 2,
        Night = 3,
    }

    public enum Team : byte
    {
        Villagers = 0,
        Werewolves = 1,
        Neutral = 2,
        Spectators = 3,
        Everyone = 4,
    }
}
