using System;
using System.Net.Sockets;

namespace TCPNetServer.Lib
{
    public class NetworkService
    {
        private NetListener clientListener;

        // 소켓 이벤트 풀, 송수신용
        private SocketAsyncEventArgsPool receiveEventPool;
        private SocketAsyncEventArgsPool sendEventPool;

        private BufferManager bufferManager;

        public delegate void SessionDelegate(UserToken token);
        
        public SessionDelegate OnNewClientConnected;
        public SessionDelegate OnClientDisconnected;

        private int maxConnections = 100;
        private int oneBufferSize = 1024;

        ~NetworkService()
        {
            clientListener = null;
            receiveEventPool = null;
            sendEventPool = null;
            bufferManager = null;
            
            OnNewClientConnected = null;
            
            NetLog.Log("Network Service is disposed.");
        }

        public void Initialize(int bufSize = 1024, int maxUser = 100)
        {
            NetLog.Log("Initialize Network Service...");
            this.oneBufferSize = bufSize;
            this.maxConnections = maxUser;

            bufferManager = new BufferManager();
            // 버퍼 바이트 크기 = 최대 동접 수 * 버퍼 하나 크기 * 2배 (수신용, 송신용)
            bufferManager.Initialize(maxConnections * oneBufferSize * 2, oneBufferSize);
            
            CreateArgsPool();
        }

        private void CreateArgsPool()
        {
            receiveEventPool = new SocketAsyncEventArgsPool(maxConnections);
            sendEventPool = new SocketAsyncEventArgsPool(maxConnections);

            for (int i = 0; i < maxConnections; i++)
            {
                // 토큰은 송수신용도 공유
                UserToken token = new UserToken();
            
                SocketAsyncEventArgs receiveArgs = new SocketAsyncEventArgs();
                receiveArgs.UserToken = token;
                receiveArgs.Completed += OnReceiveCompleted;
                bufferManager.AllocateBuffer(receiveArgs);
                receiveEventPool.Return(receiveArgs);

                SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs();
                sendArgs.UserToken = token;
                sendArgs.Completed += OnSendCompleted;
                bufferManager.AllocateBuffer(sendArgs);
                sendEventPool.Return(sendArgs);
            }
        }

        private void OnReceiveCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.LastOperation == SocketAsyncOperation.Receive)
            {
                NetLog.Log("On Receive Completed...");
                ProcessReceive(args);
                return;
            }

            throw new ArgumentException($"On receive completed : Last Operation is {args.LastOperation}");
        }

        private void OnSendCompleted(object sender, SocketAsyncEventArgs args)
        {
            NetLog.Log("On Send Completed...");
            UserToken token = args.UserToken as UserToken;
            // TODO: args 가 결국 token 이 가지고 있는 자기 자신 것 같은데 매개변수가 필요없지 않을지 고민해볼것.
            token.OnSendCompleted(args);
        }

        // host 서버에 port 번호로 접속하는 클라이언트를 감지한다.
        public void StartListen(string host, int port, int backlog)
        {
            NetLog.Log($"Start Listen... (host : {host} port : {port} backlog : {backlog})");
            if (clientListener == null) clientListener = new NetListener();
            clientListener.OnNewClient += NewClientConnected;
            clientListener.Start(host, port, backlog);
        }

        // 새로운 클라이언트가 접속 성공 했을 때
        private void NewClientConnected(Socket clientSocket, UserToken token)
        {
            NetLog.Log($"New Client Connected!");
            SocketAsyncEventArgs receiveArgs = receiveEventPool.Get();
            SocketAsyncEventArgs sendArgs = sendEventPool.Get();

            if (OnNewClientConnected != null)
            {
                UserToken userToken = receiveArgs.UserToken as UserToken;
                OnNewClientConnected(userToken);
            }
            // 클라이언트로부터 수신 시작
            BeginReceive(clientSocket, receiveArgs, sendArgs);
        }

        private void BeginReceive(Socket clientSocket, SocketAsyncEventArgs receiveArgs, SocketAsyncEventArgs sendArgs)
        {
            NetLog.Log("Begin Receive...");
            UserToken token = receiveArgs.UserToken as UserToken;
            token.InitEventArgs(receiveArgs, sendArgs);
            token.InitSocket(clientSocket);

            bool pending = clientSocket.ReceiveAsync(receiveArgs);
            if (pending == false)
            {
                ProcessReceive(receiveArgs);
            }
        }

        private void ProcessReceive(SocketAsyncEventArgs receiveArgs)
        {
            NetLog.Log("Process Receive...");
            UserToken token = receiveArgs.UserToken as UserToken;
            
            if (receiveArgs.BytesTransferred > 0 && receiveArgs.SocketError == SocketError.Success)
            {
                token.ProcessReceive(receiveArgs.Buffer, receiveArgs.Offset, receiveArgs.BytesTransferred);

                bool pending = token.GetSocket().ReceiveAsync(receiveArgs);
                if (pending == false)
                {
                    ProcessReceive(receiveArgs);
                }
            }
            // 정상 Disconnect
            else if (receiveArgs.BytesTransferred == 0)
            {
                DisconnectClient(token);
            }
            else
            {
                NetLog.Error($"SocketError {receiveArgs.SocketError} / BytesTransferred : {receiveArgs.BytesTransferred}");
            }
        }

        private void DisconnectClient(UserToken token)
        {
            NetLog.Log($"Disconnect Client Socket Called.");
            if (OnClientDisconnected != null) OnClientDisconnected(token);
            token.OnRemoved();
            
            if (receiveEventPool != null)
                receiveEventPool.Return(token.ReceiveArgs);
            if (sendEventPool != null)
                sendEventPool.Return(token.SendArgs);
        }
        

        // C -> S 클라이언트가 서버에 접속 성공 시 호출
        public void OnConnectedToServer(Socket socket, UserToken token)
        {
            NetLog.Log("On Connected To Server()...");
            SocketAsyncEventArgs receiveArgs = new SocketAsyncEventArgs();
            SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs();

            receiveArgs.Completed += OnReceiveCompleted;
            receiveArgs.UserToken = token;
            receiveArgs.SetBuffer(new byte[NetDefines.OneBufferSize], 0, NetDefines.OneBufferSize);

            sendArgs.Completed += OnSendCompleted;
            sendArgs.UserToken = token;
            sendArgs.SetBuffer(new byte[NetDefines.OneBufferSize], 0, NetDefines.OneBufferSize);
            
            BeginReceive(socket, receiveArgs, sendArgs);
        }

        public void Quit()
        {
            if (clientListener != null)
                clientListener.Stop();
        }
    }
}
