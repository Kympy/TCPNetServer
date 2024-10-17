using TCPNetServer.Lib;

namespace TCPNetServer
{
    public class GameUser : IPeer
    {
        private UserToken token = null;
        public UserToken Token => token;
        
        public GameUser(UserToken token)
        {
            this.token = token;
            token.SetPeer(this);
        }

        ~GameUser()
        {
            token = null;
        }

        // 유저로부터 받음
        public void OnMessage(byte[] buffer)
        {
            NetLog.Log($"On Message from user!");
        }

        public void Send(NetPacket packet)
        {
            NetLog.Log("Send Packet to user!");
        }

        // 서버가 Disconnect 시킴
        public void Disconnect()
        {
            NetLog.Log("Disconnect User");
            GameManager.Instance.UserManager.RemoveUser(this);
            token.GetSocket().Close();
        }

        public void OnRemoved()
        {
            NetLog.Log("User removed himself.");
            GameManager.Instance.UserManager.RemoveUser(this);
        }
    }
}
