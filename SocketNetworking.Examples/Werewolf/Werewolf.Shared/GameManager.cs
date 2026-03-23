using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking;
using SocketNetworking.Client;
using SocketNetworking.Server;
using SocketNetworking.Shared.NetworkObjects;
using SocketNetworking.Shared.SyncVars;

namespace Werewolf.Shared
{
    public class GameManager : NetworkObjectBase
    {
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
        }

        public event Action DayNightCycleUpdated;
    }

    public enum DayNightCycle : byte
    {
        Dawn = 0,
        Day = 1,
        Dusk = 2,
        Night = 3,
    }

    [Flags]
    public enum Team : byte
    {
        Villagers = 0,
        Werewolves = 1,
        Neutral = 2,
        Spectators = 3,
    }
}
