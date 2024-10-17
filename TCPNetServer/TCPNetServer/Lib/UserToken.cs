using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace TCPNetServer.Lib
{
    public class UserToken
    {
        private Socket socket = null;
        public Socket GetSocket() => socket;
        public SocketAsyncEventArgs ReceiveArgs { get; private set; }= null;
        public SocketAsyncEventArgs SendArgs { get; private set; } = null;

        private MessageResolver resolver = new MessageResolver();

        private IPeer peer = null;
        private Queue<NetPacket> sendQueue = new Queue<NetPacket>();
        private readonly object lockObj = new object();

        public UserToken()
        {
            resolver.OnMessageResolved += OnMessageResolved;
        }

        ~UserToken()
        {
            ReceiveArgs = null;
            SendArgs = null;
            socket = null;
            resolver = null;
            peer = null;
        }
        
        public void InitEventArgs(SocketAsyncEventArgs receive, SocketAsyncEventArgs send)
        {
            ReceiveArgs = receive;
            SendArgs = send;
        }

        public void InitSocket(Socket argSocket)
        {
            socket = argSocket;
        }

        public void SetPeer(IPeer argPeer)
        {
            peer = argPeer;
        }

        public void ProcessReceive(byte[] buffer, int index, int byteTransferred)
        {
            NetLog.Log("Process Receive...");
            resolver.Resolve(buffer, index, byteTransferred);
        }

        // 메세지가 모여 하나의 패킷으로 만들어졌을 때
        private void OnMessageResolved(byte[] buffer)
        {
            NetLog.Log("Messaged Resolved!");

            if (peer != null)
            {
                peer.OnMessage(buffer);
            }
        }

        public void Send(NetPacket packet)
        {
            // 복사를 하는 이유는 Send 를 호출하는 쪽에서 Packet 객체 재활용을 위해 
            // 바로 Pool 에 리턴 시키기 때문이다.
            // 따라서 값만 복사해서 사용한다.
            
            // TODO: 하지만 복사를 하면서 여기서도 new 가 발생하는데 이 부분은 어떻게 막으면 좋을지 싶다.
            NetPacket copy = new NetPacket(packet);

            lock (lockObj)
            {
                if (sendQueue.Count == 0)
                {
                    sendQueue.Enqueue(copy);
                    SendAsync();
                    return;
                }
                
                sendQueue.Enqueue(copy);
            }
        }

        private void SendAsync()
        {
            lock (lockObj)
            {
                NetPacket packet = sendQueue.Peek();
                // 헤더 작성
                packet.WriteHeader();
                int length = packet.BufferIndex;
                // 버퍼 크기 설정
                SendArgs.SetBuffer(SendArgs.Offset, length);
                Array.Copy(packet.Buffer, 0, SendArgs.Buffer, SendArgs.Offset, length);

                bool pending = socket.SendAsync(SendArgs);
                if (pending == false)
                    OnSendCompleted(SendArgs);
            }
        }

        public void OnSendCompleted(SocketAsyncEventArgs sendArgs)
        {
            if (sendArgs.SocketError == SocketError.Success)
            {
                NetLog.Log("Send Success.");
                return;
            }

            lock (lockObj)
            {
                if (sendQueue.Count == 0) throw new Exception();

                int length = sendQueue.Peek().BufferIndex;
                if (sendArgs.BytesTransferred != length)
                {
                    NetLog.Error($"Transferred and MsgLength is different. Transferred {sendArgs.BytesTransferred} / Length : {length}");
                    return;
                }

                sendQueue.Dequeue();
                if (sendQueue.Count > 0)
                {
                    SendAsync();
                }
            }
        }

        public void OnRemoved()
        {
            NetLog.Log("Token removed...");
            sendQueue.Clear();
            
            if (peer != null)
                peer.OnRemoved();
        }

        // 토큰으로 직접 종료할 경우에만
        public void Disconnect()
        {
            NetLog.Log("Token Disconnect...");
            sendQueue.Clear();

            if (peer != null)
            {
                peer.Disconnect();
            }

            try
            {
                socket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception e)
            {
                NetLog.Error(e.ToString());
            }
            socket.Close();
        }
    }
}
