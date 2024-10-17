namespace TCPNetServer
{
    public class GameManager
    {
        private static readonly object lockObject = new object();
        private static GameManager instance = null;

        public static GameManager Instance
        {
            get
            {
                lock(lockObject)
                {
                    return instance;
                }
            }
        }

        public static GameManager CreateInstance()
        {
            lock (lockObject)
            {
                instance = new GameManager();
                return instance;
            }
        }

        ~GameManager()
        {
            UserManager = null;
            instance = null;
        }

        public UserManager UserManager { get; private set; } = new UserManager();
        
        
    }
}
