using System.Reflection;
using SocketNetworking.Shared;

namespace Werewolf.Shared
{
    public class Utility
    {
        public static void ImportHelper()
        {
            NetworkManager.ImportAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
