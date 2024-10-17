
using System;
using System.Threading;
using TCPNetServer.Lib;

namespace TCPNetServer
{
    internal class Program
    {
        [System.Runtime.InteropServices.DllImport("User32.dll")]
        public static extern short GetAsyncKeyState(int input);
        
        public const int TargetFrame = 60;
        public const int FrameTime = 1000 / TargetFrame;
        
        public static void Main(string[] args)
        {
            GameManager.CreateInstance();
            
            NetPacketPool.CreateInstance(2000);
            
            NetworkService service = new NetworkService();
            service.OnNewClientConnected += GameManager.Instance.UserManager.NewUserConnected;
            
            service.Initialize();
            service.StartListen("127.0.0.1", 1414, 100);

            while (true)
            {
                if ((GetAsyncKeyState((int)ConsoleKey.Tab) & 0x8000) != 0)
                {
                    //0x51
                    if ((GetAsyncKeyState((int)ConsoleKey.Q) & 0x8000) != 0)
                    {
                        break;
                    }
                }
                Thread.Sleep(FrameTime);
            }
            
            service.Quit();
            GameManager.Instance.UserManager.DisconnectAll();
            
            NetLog.Log("Server down.");
        }
    }
}
