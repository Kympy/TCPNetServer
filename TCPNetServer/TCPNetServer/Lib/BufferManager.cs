using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace TCPNetServer.Lib
{
    public class BufferManager
    {
        private byte[] buffer;

        // 비어있는 버퍼 인덱스를 가리키는 풀
        private Stack<int> freeIndexPool = new Stack<int>();

        private int currentIndex = 0;

        // 버퍼 하나 크기
        private int oneBufferSize;

        ~BufferManager()
        {
            buffer = null;
            freeIndexPool = null;
        }
        
        public void Initialize(int totalBytes, int argOneBufferSize)
        {
            buffer = new byte[totalBytes];
            oneBufferSize = argOneBufferSize;
            
            freeIndexPool.Clear();
        }

        // args 에 버퍼 할당.
        public bool AllocateBuffer(SocketAsyncEventArgs args)
        {
            // 빈 곳이 있다면,
            if (freeIndexPool.Count > 0)
            {
                args.SetBuffer(buffer, freeIndexPool.Pop(), oneBufferSize);
                return true;
            }

            // 최대 버퍼의 현재 인덱스에서부터 남은 크기가 버퍼 1개의 크기보다 작다면
            if (buffer.Length - currentIndex < oneBufferSize)
            {
                throw new IndexOutOfRangeException($"TotalLength : {buffer.Length} / CurrentIndex : {currentIndex} / oneBuffer : {oneBufferSize}");
            }
            
            // 현재 인덱스 부터 버퍼 한개 길이 만큼 버퍼 할당.
            args.SetBuffer(buffer, currentIndex, oneBufferSize);
            currentIndex += oneBufferSize;

            return true;
        }
    }
}
