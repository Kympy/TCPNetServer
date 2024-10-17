using System;

namespace TCPNetServer.Lib
{
    public class NetLog
    {
        public static void Log(string msg)
        {
            Console.WriteLine($"[Log] {msg}");
        }

        public static void Warning(string msg)
        {
            Console.WriteLine($"[War] {msg}");
        }

        public static void Error(string msg)
        {
            Console.WriteLine($"[Err] {msg}");
        }
    }
}
