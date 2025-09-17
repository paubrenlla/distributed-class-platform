namespace Common;

using System;
using System.Net.Sockets;
using System.Text;

public class Frame
{
    public string Header { get; set; } 
    public short Command { get; set; }
    public byte[] Data { get; set; }
}

public class NetworkDataHelper
{
    private readonly Socket _socket;

    public NetworkDataHelper(Socket socket)
    {
        _socket = socket;
    }
    
    public void Send(Frame frame)
    {
        byte[] headerBytes = Encoding.ASCII.GetBytes(frame.Header);
        
        byte[] commandBytes = BitConverter.GetBytes(frame.Command);

        int dataLength = frame.Data?.Length ?? 0;
        byte[] dataLengthBytes = BitConverter.GetBytes(dataLength);

        // Concatenar en un solo paquete
        // [HEADER (3)] + [CMD (2)] + [LARGO (4)] + [DATOS (...)]
        int totalPacketSize = ProtocolConstants.FixedHeaderSize + dataLength;
        byte[] packet = new byte[totalPacketSize];
        
        Buffer.BlockCopy(headerBytes, 0, packet, 0, headerBytes.Length);
        Buffer.BlockCopy(commandBytes, 0, packet, ProtocolConstants.HeaderLength, commandBytes.Length);
        Buffer.BlockCopy(dataLengthBytes, 0, packet, ProtocolConstants.HeaderLength + ProtocolConstants.CommandLength, dataLengthBytes.Length);

        if (dataLength > 0)
        {
            Buffer.BlockCopy(frame.Data, 0, packet, ProtocolConstants.FixedHeaderSize, frame.Data.Length);
        }
        
        // Enviar el paquete completo usando el método Send original
        Send(packet);
    }
    
    public Frame Receive()
    {
        byte[] fixedHeader = Receive(ProtocolConstants.FixedHeaderSize);

        string header = Encoding.ASCII.GetString(fixedHeader, 0, ProtocolConstants.HeaderLength);
        short command = BitConverter.ToInt16(fixedHeader, ProtocolConstants.HeaderLength);
        int dataLength = BitConverter.ToInt32(fixedHeader, ProtocolConstants.HeaderLength + ProtocolConstants.CommandLength);
        
        byte[] data = null;
        if (dataLength > 0)
        {
            data = Receive(dataLength);
        }

        return new Frame { Header = header, Command = command, Data = data };
    }

    private void Send(byte[] buffer)
    {
        int length = buffer.Length;
        int offset = 0;
        while (offset < length)
        {
            int sent = _socket.Send(buffer, offset, length - offset, SocketFlags.None);
            if (sent == 0) throw new SocketException();
            offset += sent;
        }
    }

    private byte[] Receive(int length)
    {
        byte[] buffer = new byte[length];
        int offset = 0;
        while (offset < length)
        {
            int received = _socket.Receive(buffer, offset, length - offset, SocketFlags.None);
            if (received == 0) throw new SocketException();
            offset += received;
        }
        return buffer;
    }
}