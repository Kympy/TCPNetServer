using System.Collections.Generic;
using TCPNetServer.Lib;

namespace TCPNetServer
{
    public class UserManager
    {
        private List<GameUser> userList = new List<GameUser>();

        ~UserManager()
        {
            userList.Clear();
            userList = null;
        }
        
        public void NewUserConnected(UserToken token)
        {
            NetLog.Log($"User connected.");
            GameUser user = new GameUser(token);
            userList.Add(user);
        }

        public void RemoveUser(GameUser user)
        {
            if (userList.Contains(user) == false)
            {
                NetLog.Error("User is not exist in list.");
                return;
            }
            userList.Remove(user);
        }

        public void DisconnectAll()
        {
            NetLog.Log("Disconnect All Users.");
            for (int i = 0; i < userList.Count; i++)
            {
                userList[i].Disconnect();
            }
        }
    }
}
