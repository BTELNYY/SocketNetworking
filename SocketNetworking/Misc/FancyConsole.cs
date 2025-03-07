using System;
using System.Collections.Generic;

namespace SocketNetworking.Misc
{
    public class FancyConsole
    {
        static object locker = new object();
        static List<char> buffer = new List<char>();

        public static void WriteLine(string message, ConsoleColor color)
        {
            lock(locker)
            {
                ConsoleColor oldColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                WriteLine(message);
                Console.ForegroundColor = oldColor;
            }
        }

        public static void WriteLine(string message)
        {
            lock (locker)
            {
                Console.Write(new string('\b', buffer.Count));
                var msg = $"{message}";
                var excess = buffer.Count - msg.Length;
                if (excess > 0) msg += new string(' ', excess);
                Console.WriteLine(msg);
                Console.Write(new string(buffer.ToArray()));
            }
        }

        public static string ReadLine()
        {
            while (true)
            {
                var k = Console.ReadKey();
                if (k.Key == ConsoleKey.Enter && buffer.Count > 0)
                {
                    lock (locker)
                    {
                        if (buffer[0] == '>')
                        {
                            buffer.RemoveRange(0, Math.Min(2, buffer.Count));
                        }
                        string result = string.Join("", buffer);
                        Console.WriteLine();
                        buffer.Clear();
                        buffer.AddRange("> ");
                        Console.Write(buffer.ToArray());
                        return result;
                    }
                }
                else
                {
                    if (k.Key == ConsoleKey.Backspace)
                    {
                        buffer.RemoveAt(buffer.Count - 1);
                    }
                    buffer.Add(k.KeyChar);
                }
            }
        }
    }
}
