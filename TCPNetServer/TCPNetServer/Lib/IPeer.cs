namespace TCPNetServer.Lib
{
    // 서버 클라 공통 세션 객체
    
    // 서버에서는 접속한 클라이언트를 의미
    // 클라에서는 접속한 서버를 의미
    
    public interface IPeer
    {
        // 수신
        void OnMessage(byte[] buffer);

        // 전송
        void Send(NetPacket packet);

        // 연결 끊기
        void Disconnect();

        // 연결 끊김
        void OnRemoved();
    }
}
