namespace Werewolf.Shared
{
    public interface IRole
    {
        public Team Team { get; }

    }

    public enum Team : byte
    {
        Villagers = 0,
        Werewolves = 1,
        Nuetral = 2,
        Spectators = 3,
    }
}
