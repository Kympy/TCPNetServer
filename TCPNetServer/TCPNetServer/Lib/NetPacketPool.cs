using System.Collections.Generic;

namespace TCPNetServer.Lib
{
    public class NetPacketPool
    {
        private static NetPacketPool instance = null;
        private static readonly object lockObj = new object();

        public static NetPacketPool Instance
        {
            get
            {
                lock (lockObj)
                {
                    return instance;
                }
            }
        }

        public static NetPacketPool CreateInstance(int poolSize = 2000)
        {
            lock (lockObj)
            {
                instance = new NetPacketPool(poolSize);
                return instance;
            }
        }

        private Stack<NetPacket> pool = null;
        private int capacity = 0;
        
        public NetPacketPool(int size)
        {
            pool = new Stack<NetPacket>(size);
            capacity = size;
        }

        ~NetPacketPool()
        {
            pool.Clear();
            pool = null;
        }

        public NetPacket Get(int protocolId)
        {
            NetPacket packet = null;
            lock (lockObj)
            {
                if (pool.Count == 0)
                {
                    for (int i = 0; i < capacity; i++)
                    {
                        pool.Push(new NetPacket());
                    }
                }
                packet = pool.Pop();
            }
            
            packet.SetProtocolId(protocolId);
            return packet;
        }
        
        public void Return(NetPacket packet)
        {
            lock (lockObj)
            {
                pool.Push(packet);
            }
        }
    }
}
