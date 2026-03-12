using System.Reflection;

namespace QuicShared
{
    public class Utility
    {
        public static Assembly GetAssembly()
        {
            return Assembly.GetExecutingAssembly();
        }
    }
}
