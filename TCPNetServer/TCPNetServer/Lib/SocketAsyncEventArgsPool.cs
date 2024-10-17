using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;

namespace TCPNetServer.Lib
{
    public class SocketAsyncEventArgsPool
    {
        private Stack<SocketAsyncEventArgs> pool;
        public SocketAsyncEventArgsPool(int maxConnections)
        {
            pool = new Stack<SocketAsyncEventArgs>(maxConnections);
        }

        ~SocketAsyncEventArgsPool()
        {
            pool = null;
        }

        public SocketAsyncEventArgs Get()
        {
            if (pool.Count == 0)
            {
                throw new TargetParameterCountException();
            }

            lock (pool)
            {
                return pool.Pop();
            }
        }

        public void Return(SocketAsyncEventArgs args)
        {
            if (args == null) throw new ArgumentNullException();
            lock (pool)
            {
                pool.Push(args);
            }
        }
    }
}
