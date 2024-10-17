using System;

namespace TCPNetServer.Lib
{
    public class MessageResolver
    {
        public delegate void MessageCompletedDelegate(byte[] buffer);

        public MessageCompletedDelegate OnMessageResolved;

        private byte[] tempBuffer = new byte[1024];

        private int remainBytes = 0;
        private int originBufferIndex = 0;
        private int currentIndex = 0;
        private int targetIndex = 0;
    
        ~MessageResolver()
        {
            OnMessageResolved = null;
            tempBuffer = null;
        }

        public void Resolve(byte[] buffer, int index, int byteTransferred)
        {
            NetLog.Log("Resolve...");
            // 초기화
            remainBytes = byteTransferred;
            originBufferIndex = index;

            // 남은게 없을 때 까지
            while (remainBytes > 0)
            {
                // 헤더를 못읽었다면
                if (currentIndex < NetDefines.HeaderSize)
                {
                    targetIndex = NetDefines.HeaderSize;

                    EReadResult result = Read(buffer, index, byteTransferred);
                    if (result == EReadResult.Left)
                    {
                        return;
                    }
                    
                    // 헤더를 다 읽었다면, 다음 인덱스는 헤더 + 메세지 길이
                    targetIndex = GetBodyLength() + NetDefines.HeaderSize;
                }

                EReadResult bodyResult = Read(buffer, index, byteTransferred);
                if (bodyResult == EReadResult.End)
                {
                    if (OnMessageResolved != null)
                        OnMessageResolved(tempBuffer);
                    ClearTempBuffer();
                }
            }
        }

        private enum EReadResult
        {
            Left,
            End,
        }

        private EReadResult Read(byte[] buffer, int index, int byteTransferred)
        {
            if (currentIndex >= index + byteTransferred)
            {
                return EReadResult.End;
            }
            // 읽어올 크기
            int readSize = targetIndex - currentIndex;
            // 남은 게 더 적다면 남은거 만큼 읽기
            if (readSize > remainBytes)
            {
                readSize = remainBytes;
            }
            
            // 원본 데이터를 temp 에 복사해서 저장
            Array.Copy(buffer, originBufferIndex, tempBuffer, currentIndex, readSize);

            originBufferIndex += readSize;
            currentIndex += readSize;

            remainBytes -= readSize;

            if (currentIndex < targetIndex)
            {
                return EReadResult.Left;
            }

            return EReadResult.End;
        }

        private int GetBodyLength()
        {
            Type headerType = NetDefines.HeaderSize.GetType();
            if (headerType == typeof(short))
            {
                return BitConverter.ToInt16(tempBuffer, 0);
            }

            return BitConverter.ToInt32(tempBuffer, 0);
        }

        private void ClearTempBuffer()
        {
            Array.Clear(tempBuffer, 0, tempBuffer.Length);
            currentIndex = 0;
        }
    }
}
