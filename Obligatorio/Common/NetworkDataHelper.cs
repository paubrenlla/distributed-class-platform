using System.Net.Sockets;

namespace Common
{
    public class NetworkDataHelper
    {
        public readonly Socket _socket;

        public NetworkDataHelper(Socket socket)
        {
            _socket = socket;
        }

        public byte[] Receive(int length)
        {
            byte[] buffer = new byte[length];
            int offset = 0;

            while (offset < length)
            {
                int received = _socket.Receive(buffer, offset, length - offset, SocketFlags.None);
                if (received == 0)
                {
                    throw new SocketException();
                }
                offset += received;
            }

            return buffer;
        }

        public void Send(byte[] buffer)
        {
            int length = buffer.Length;
            int offset = 0;

            while (offset < length)
            {
                int sent = _socket.Send(buffer, offset, length - offset, SocketFlags.None);
                if (sent == 0)
                {
                    throw new SocketException();
                }
                offset += sent;
            }
        }
    }
}