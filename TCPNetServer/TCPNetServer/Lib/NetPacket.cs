using System;
using System.Text;

namespace TCPNetServer.Lib
{
    public class NetPacket
    {
        public IPeer Owner { get; private set; }
        public byte[] Buffer { get; private set; } = new byte[NetDefines.OneBufferSize];
        public int BufferIndex { get; private set; }
        
        public int ProtocolId { get; private set; }

        public NetPacket() { BufferIndex = NetDefines.HeaderSize; }

        public NetPacket(byte[] buffer)
        {
            Buffer = buffer;
            BufferIndex = NetDefines.HeaderSize;
        }

        public NetPacket(NetPacket other)
        {
            other.CopyTo(this);
        }

        ~NetPacket()
        {
            Owner = null;
            Buffer = null;
        }

        public void SetProtocolId(int id)
        {
            ProtocolId = id;
            BufferIndex = NetDefines.HeaderSize;
            Push(ProtocolId);
        }

        // 최초에 호출해야 Index 꼬이지 않음.
        public int GetProtocolId()
        {
            // 혹시 몰라 index 를 처음으로 초기화.
            BufferIndex = NetDefines.HeaderSize;
            return PopInt();
        }

        public void CopyTo(NetPacket target)
        {
            target.SetProtocolId(this.ProtocolId);
            target.Overwrite(Buffer, BufferIndex);
        }
        
        private void Overwrite(byte[] source, int index)
        {
            Array.Copy(source, Buffer, source.Length);
            BufferIndex = index;
        }

        public void WriteHeader()
        {
            int bodySize = BufferIndex - NetDefines.HeaderSize;
            byte[] header = BitConverter.GetBytes(bodySize);
            header.CopyTo(Buffer, 0);
        }

        public void Push(int data)
        {
            byte[] temp = BitConverter.GetBytes(data);
            temp.CopyTo(Buffer, BufferIndex);
            BufferIndex += temp.Length;
        }

        public void Push(string data)
        {
            byte[] temp = Encoding.UTF8.GetBytes(data);
            byte[] lengthBuffer = BitConverter.GetBytes(temp.Length);
            
            lengthBuffer.CopyTo(Buffer, BufferIndex);
            BufferIndex += sizeof(int);

            temp.CopyTo(Buffer, BufferIndex);
            BufferIndex += temp.Length;
        }

        public int PopInt()
        {
            int data = BitConverter.ToInt32(Buffer, BufferIndex);
            BufferIndex += sizeof(int);

            return data;
        }

        public string PopString()
        {
            int strLength = BitConverter.ToInt32(Buffer, BufferIndex);
            BufferIndex += sizeof(int);

            string data = Encoding.UTF8.GetString(Buffer, BufferIndex, strLength);
            BufferIndex += strLength;

            return data;
        }
    }
}
