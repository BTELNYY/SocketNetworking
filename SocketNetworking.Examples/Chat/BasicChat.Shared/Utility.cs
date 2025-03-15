using System.Reflection;

namespace BasicChat.Shared
{
    public class Utility
    {
        public static Assembly GetAssembly()
        {
            return Assembly.GetExecutingAssembly();
        }
    }
}
