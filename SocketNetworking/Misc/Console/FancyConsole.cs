using System;
using System.Collections.Generic;
using System.IO;

namespace SocketNetworking.Misc.Console
{
    public static class FancyConsole
    {
        static object locker = new object();
        static List<char> buffer = new List<char>();

        static TextReader oldIn = null;

        static TextWriter oldOut = null;

        static FancyConsole()
        {
            oldIn = System.Console.In;
            oldOut = System.Console.Out;
        }

        public static void WriteLine(string message, ConsoleColor color)
        {
            lock (locker)
            {
                ConsoleColor oldColor = System.Console.ForegroundColor;
                System.Console.ForegroundColor = color;
                WriteLine(message);
                System.Console.ForegroundColor = oldColor;
            }
        }

        public static void WriteLine(string message)
        {
            lock (locker)
            {
                oldOut.Write(new string('\b', buffer.Count));
                var msg = $"{message}";
                var excess = buffer.Count - msg.Length;
                if (excess > 0) msg += new string(' ', excess);
                oldOut.WriteLine(msg);
                oldOut.Write(new string(buffer.ToArray()));
            }
        }

        public static void Write(string message)
        {
            lock (locker)
            {
                oldOut.Write(new string('\b', buffer.Count));
                var msg = $"{message}";
                var excess = buffer.Count - msg.Length;
                if (excess > 0) msg += new string(' ', excess);
                oldOut.Write(msg);
                oldOut.Write("\n" + new string(buffer.ToArray()));
            }
        }

        public static string ReadLine(string cursor = "> ")
        {
            char[] cursorArray = cursor.ToCharArray();
            while (true)
            {
                var k = System.Console.ReadKey();
                if (k.Key == ConsoleKey.Enter && buffer.Count > 0)
                {
                    lock (locker)
                    {
                        bool stripCursor = true;
                        for(int i = 0; i < cursorArray.Length; i++)
                        {
                            if(buffer[i] != cursorArray[i])
                            {
                                stripCursor = false;
                                break;
                            }
                        }
                        if(stripCursor)
                        {
                            buffer.RemoveRange(0, cursorArray.Length);
                        }
                        string result = string.Join("", buffer);
                        oldOut.WriteLine();
                        buffer.Clear();
                        buffer.AddRange(cursorArray);
                        oldOut.Write(buffer.ToArray());
                        return result;
                    }
                }
                else
                {
                    if (k.Key == ConsoleKey.Backspace)
                    {
                        if(buffer.Count == cursorArray.Length)
                        {
                            oldOut.Write(cursorArray[cursorArray.Length - 1]);
                            continue;
                        }
                        buffer.RemoveAt(buffer.Count - 1);
                        oldOut.Write(' ');
                        oldOut.Write('\b');
                        continue;
                    }
                    buffer.Add(k.KeyChar);
                }
            }
        }
    }
}
