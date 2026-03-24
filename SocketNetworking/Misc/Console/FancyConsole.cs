using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SocketNetworking.Misc.Console
{
    /// <summary>
    /// The <see cref="FancyConsole"/> class allows some comfort features like the command not being broken in half by new data being pushed to the console buffer. It is a utility class.
    /// </summary>
    public static class FancyConsole
    {
        private static Dictionary<char, ConsoleColor> ColorArray = new Dictionary<char, ConsoleColor>()
        {
            ['0'] = ConsoleColor.Black,
            ['1'] = ConsoleColor.DarkBlue,
            ['2'] = ConsoleColor.DarkGreen,
            ['3'] = ConsoleColor.DarkCyan,
            ['4'] = ConsoleColor.DarkRed,
            ['5'] = ConsoleColor.DarkMagenta,
            ['6'] = ConsoleColor.DarkYellow,
            ['7'] = ConsoleColor.Gray,
            ['8'] = ConsoleColor.DarkGray,
            ['9'] = ConsoleColor.Blue,
            ['a'] = ConsoleColor.Green,
            ['b'] = ConsoleColor.Cyan,
            ['c'] = ConsoleColor.Red,
            ['d'] = ConsoleColor.Magenta,
            ['e'] = ConsoleColor.Yellow,
            ['f'] = ConsoleColor.White,
        };

        public static char SpecialMarker = '&';

        static object locker = new object();
        static List<char> buffer = new List<char>();

        static TextReader oldIn = null;

        static TextWriter oldOut = null;

        static FancyConsole()
        {
            oldIn = System.Console.In;
            oldOut = System.Console.Out;
        }

        public static string StripColor(string message)
        {
            string result = string.Empty;
            char[] chars = message.ToCharArray();
            bool lastCharSpecial = false;
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] == SpecialMarker)
                {
                    lastCharSpecial = true;
                    continue;
                }
                if (lastCharSpecial)
                {
                    lastCharSpecial = false;
                    continue;
                }
                result += chars[i];
            }
            return result;
        }

        public static void WriteLineAutoColor(string message)
        {
            WriteAutoColor(message);
            Write("\n");
        }

        public static void WriteAutoColor(string message)
        {
            ConsoleColor color = System.Console.ForegroundColor;
            string[] strs = message.Split(SpecialMarker);
            foreach (string str in strs)
            {
                if (ColorArray.ContainsKey(str.First()))
                {
                    System.Console.ForegroundColor = ColorArray[str.First()];
                    Write(str.Remove(0, 1));
                    System.Console.ForegroundColor = color;
                    return;
                }
                Write(str);
            }
            System.Console.ForegroundColor = color;
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
                oldOut.Write(new string(' ', buffer.Count));
                oldOut.Write(new string('\b', buffer.Count));
                string msg = $"{message}";
                int excess = buffer.Count - msg.Length;
                //if (excess > 0) msg += new string(' ', excess);
                oldOut.WriteLine(msg);
                oldOut.Write(new string(buffer.ToArray()));
            }
        }

        public static void Write(string message)
        {
            lock (locker)
            {
                oldOut.Write(new string('\b', buffer.Count));
                oldOut.Write(new string(' ', buffer.Count));
                oldOut.Write(new string('\b', buffer.Count));
                string msg = $"{message}";
                int excess = buffer.Count - msg.Length;
                //if (excess > 0) msg += new string(' ', excess);
                oldOut.Write(msg);
                oldOut.Write(new string(buffer.ToArray()));
            }
        }

        public static void Write(string message, ConsoleColor color)
        {
            lock (locker)
            {
                ConsoleColor oldColor = System.Console.ForegroundColor;
                System.Console.ForegroundColor = color;
                Write(message);
                System.Console.ForegroundColor = oldColor;
            }
        }

        public static string ReadLine(string cursor = "> ")
        {
            char[] cursorArray = cursor.ToCharArray();
            while (true)
            {
                ConsoleKeyInfo k = System.Console.ReadKey();
                if (k.Key == ConsoleKey.Enter && buffer.Count > 0)
                {
                    lock (locker)
                    {
                        bool stripCursor = true;
                        for (int i = 0; i < cursorArray.Length; i++)
                        {
                            if (buffer[i] != cursorArray[i])
                            {
                                stripCursor = false;
                                break;
                            }
                        }
                        if (stripCursor)
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
                        if (buffer.Count == cursorArray.Length)
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
