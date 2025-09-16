using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Common;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Client Application..");

            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint localEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0);
            clientSocket.Bind(localEndpoint);

            IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000);
            clientSocket.Connect(serverEndpoint);

            Console.WriteLine("Connected to server!!");
            NetworkDataHelper networkDataHelper = new NetworkDataHelper(clientSocket);

            bool clientRunning = true;
            while (clientRunning)
            {
                Console.WriteLine("Type command (create/list/exit):");
                var cmd = Console.ReadLine()?.Trim().ToLower();
                if (cmd == "exit")
                {
                    clientRunning = false;
                    break;
                }

                if (cmd == "create")
                {
                    Console.Write("Username: ");
                    string username = Console.ReadLine();
                    Console.Write("Password: ");
                    string password = Console.ReadLine();
                    Console.Write("Display name (optional): ");
                    string displayName = Console.ReadLine();

                    string message = $"CREATE_USER|{username}|{password}|{displayName}";
                    SendMessage(networkDataHelper, message);

                    string serverResponse = ReceiveResponse(networkDataHelper);
                    Console.WriteLine("Server: " + serverResponse);
                }
                else if (cmd == "list")
                {
                    string message = "LIST_USERS";
                    SendMessage(networkDataHelper, message);
                    string serverResponse = ReceiveResponse(networkDataHelper);
                    Console.WriteLine("Server: " + serverResponse);
                }
                else
                {
                    Console.WriteLine("Comando no reconocido. Usa create, list o exit.");
                }
            }

            Console.WriteLine("Closing connection...");
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
        }

        static void SendMessage(NetworkDataHelper helper, string message)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            ushort messageLength = (ushort)messageBytes.Length;
            byte[] messageLengthBytes = BitConverter.GetBytes(messageLength);

            helper.Send(messageLengthBytes);
            helper.Send(messageBytes);
            Console.WriteLine("Sent message...");
        }

        static string ReceiveResponse(NetworkDataHelper helper)
        {
            byte[] responseLenBuffer = helper.Receive(2);
            ushort responseLen = BitConverter.ToUInt16(responseLenBuffer);
            byte[] responseBuffer = helper.Receive(responseLen);
            string response = Encoding.UTF8.GetString(responseBuffer);
            return response;
        }
    }
}
