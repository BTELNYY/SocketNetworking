using System;
using System.Collections.Generic;
using System.Linq;
using SocketNetworking;
using SocketNetworking.Misc;
using SocketNetworking.Server;
using SocketNetworking.Shared.NetworkObjects;
using SocketNetworking.Shared.SyncVars;

namespace Werewolf.Shared
{
    public class GameManager : NetworkObjectBase
    {
        public static GameManager Instance { get; private set; }

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
        }

        public void ServerStartRound()
        {
            this.ThrowIfNotServer();
            _cycle.Value = DayNightCycle.Dawn;
            _nights.Value = 0;
            foreach (PlayerAvatar avatar in NetworkServer.Clients.Cast<WerewolfClient>().Select(x => x.PlayerAvatar))
            {
                avatar.ServerSetTeam(Team.Villagers);
            }
            CallbackTimer<GameManager> callback = new CallbackTimer<GameManager>((manager) =>
            {
                manager.Advance();
            }, this, CycleTime[Cycle]);
        }

        public Dictionary<DayNightCycle, int> CycleTime = new Dictionary<DayNightCycle, int>()
        {
            [DayNightCycle.Dawn] = 5,
            [DayNightCycle.Day] = 240,
            [DayNightCycle.Dusk] = 5,
            [DayNightCycle.Night] = 120,
        };

        /// <summary>
        /// Advance the phase of the day.
        /// </summary>
        public void Advance()
        {
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

        private void OnDayBegin()
        {

        }

        private void OnDuskBegin()
        {

        }

        private void OnNightBegin()
        {

        }

        private void OnDawnBegin()
        {

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
            message = $"<{avatar.Name} ({OwnerClient.ClientID})>: {message}";
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
