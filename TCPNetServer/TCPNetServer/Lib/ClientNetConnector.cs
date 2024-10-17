using System;
using System.Net;
using System.Net.Sockets;

namespace TCPNetServer.Lib
{
    // 클라이언트가 서버에 접속하기 위해 사용
    // 접속하려는 서버 당 1대1 대응되어 생성
    public class ClientNetConnector
    {
        private Socket clientSocket = null;
        private NetworkService netService = null;
    
        public delegate void ConnectDelegate(UserToken token);

        public ConnectDelegate OnConnected = null;

        public ClientNetConnector(NetworkService service)
        {
            netService = service;
            NetLog.Log("Client Net Connector is created.");
        }

        ~ClientNetConnector()
        {
            clientSocket = null;
            netService = null;
            OnConnected = null;
            NetLog.Log("Client Net Connector is disposed.");
        }

        public void Connect(IPEndPoint endPoint)
        {
            NetLog.Log($"Try connect to {endPoint.Address}");
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += OnConnectCompleted;
            args.RemoteEndPoint = endPoint;
            bool pending = clientSocket.ConnectAsync(args);
            if (pending == false)
            {
                OnConnectCompleted(null, args);
            }
        }

        private void OnConnectCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                UserToken token = new UserToken();
                netService.OnConnectedToServer(clientSocket, token);

                if (OnConnected != null) OnConnected(token);

                NetLog.Log("Client connected to server successfully.");
                return;
            }

            NetLog.Log($"Socket Error : {args.SocketError}");
        }
    }
}
