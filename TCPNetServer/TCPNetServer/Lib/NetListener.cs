using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TCPNetServer.Lib
{
    public class NetListener
    {
        private Socket listenSocket = null;
        private SocketAsyncEventArgs acceptArgs = null;

        private AutoResetEvent flowControlEvent = null;
    
        public delegate void NewClientDelegate(Socket socket, UserToken token);

        public NewClientDelegate OnNewClient;

        private CancellationTokenSource listenToken;

        ~NetListener()
        {
            OnNewClient = null;
            flowControlEvent = null;
            acceptArgs = null;
            listenSocket = null;
            listenToken = null;
        }
        
        // host 로 접속하는 것을 감지한다.
        public void Start(string host, int port, int backlog)
        {
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPAddress address = host == "0.0.0.0" ? IPAddress.Any : IPAddress.Parse(host);
            IPEndPoint endPoint = new IPEndPoint(address, port);

            try
            {
                NetLog.Log($"Bind...{endPoint}");
                listenSocket.Bind(endPoint);
                listenSocket.Listen(backlog);

                acceptArgs = new SocketAsyncEventArgs();
                acceptArgs.Completed += OnAccepted;

                listenToken = new CancellationTokenSource();
                Task.Run(ListenLoop, listenToken.Token);
            }
            catch (Exception e)
            {
                NetLog.Error(e.ToString());
            }
        }

        // 승인이 나면 생성된 clientSocket 을 보관
        private void OnAccepted(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                NetLog.Log("Client accepted!");
                Socket clientSocket = args.AcceptSocket;

                flowControlEvent.Set();

                if (OnNewClient != null) OnNewClient(clientSocket, args.UserToken as UserToken);

                return;
            }
            
            NetLog.Error($"Client accept is failed. {args.SocketError}");
        }

        private void ListenLoop()
        {
            flowControlEvent = new AutoResetEvent(false);

            while (true)
            {
                if (listenSocket == null)
                {
                    NetLog.Error("Listen Socket is not exist.");
                    return;
                }

                if (listenToken == null || listenToken.IsCancellationRequested)
                {
                    NetLog.Warning("Listen Loop is canceled.");
                    return;
                }
                
                acceptArgs.AcceptSocket = null;

                bool pending = true;

                try
                {
                    pending = listenSocket.AcceptAsync(acceptArgs);
                }
                catch (Exception e)
                {
                    NetLog.Error(e.ToString());
                    continue;
                }

                if (pending == false)
                {
                    OnAccepted(null, acceptArgs);
                }

                flowControlEvent.WaitOne();
            }
        }

        public void Stop()
        {
            if (listenSocket == null) return;
            listenSocket.Close();
            listenSocket = null;
            
            listenToken?.Cancel();
            listenToken = null;
        }
    }
}
