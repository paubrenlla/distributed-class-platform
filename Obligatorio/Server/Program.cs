using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Common;
using Repository;
using Domain;

namespace Server
{
    internal class Program
    {
        static UserRepository userRepo = new UserRepository();

        static void Main(string[] args)
        {
            Console.WriteLine("Starting Server Application..");

            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000); //esto deberia ser 0 en vez de 5000?

            serverSocket.Bind(serverEndpoint);
            serverSocket.Listen();

            Console.WriteLine("Waiting for clients to connect...");

            while (true) //Ver como hacer para cerrar el server o dejar de acpetar clientes
            {
                Socket clientSocket = serverSocket.Accept();
                Thread t = new Thread(() => HandleClient(clientSocket));
                t.Start();
            }
        }

        static void HandleClient(Socket clientSocket)
        {
            Console.WriteLine("Client connected: " + clientSocket.RemoteEndPoint);
            bool clientActive = true;
            NetworkDataHelper networkDataHelper = new NetworkDataHelper(clientSocket);

            while (clientActive)
            {
                try
                {
                    // Leer largo (2 bytes)
                    byte[] messageLengthBuffer = networkDataHelper.Receive(2);
                    ushort messageLength = BitConverter.ToUInt16(messageLengthBuffer);

                    // Leer mensaje
                    byte[] buffer = networkDataHelper.Receive(messageLength);
                    string message = Encoding.UTF8.GetString(buffer);

                    Console.WriteLine($"Client sent: {message}");

                    // Procesar comando simple: CREATE_USER|username|password|displayName
                    string response = ProcessCommand(message);
                    SendResponse(networkDataHelper, response);
                }
                catch (SocketException)
                {
                    clientActive = false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error processing client: " + ex.Message);
                    try
                    {
                        SendResponse(new NetworkDataHelper(clientSocket), "ERR|Server error: " + ex.Message);
                    }
                    catch { }
                    clientActive = false;
                }
            }

            Console.WriteLine("Client disconnected: " + clientSocket.RemoteEndPoint);
            try
            {
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
            }
            catch { }
        }

        static string ProcessCommand(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return "ERR|Empty message";

            var parts = message.Split('|');
            var cmd = parts[0].ToUpperInvariant();

            if (cmd == "CREATE_USER")
            {
                // Esperamos al menos 3 partes: CREATE_USER|username|password (displayName opcional)
                if (parts.Length < 3) return "ERR|CREATE_USER requires username and password";

                string username = parts[1].Trim();
                string password = parts[2].Trim();
                string displayName = parts.Length >= 4 ? parts[3].Trim() : username;

                try
                {
                    var usuario = new User(username, password, displayName);
                    userRepo.Add(usuario);
                    Console.WriteLine($"Usuario creado: {usuario.Username} (id={usuario.Id})");
                    return $"OK|{usuario.Id}";
                }
                catch (Exception ex)
                {
                    return $"ERR|{ex.Message}";
                }
            }

            // Podés agregar más comandos aquí (GET_USER, LIST_USERS, DELETE_USER, etc.)
            if (cmd == "LIST_USERS")
            {
                var all = userRepo.GetAll();
                // armar respuesta simple separada por ';' cada usuario "id:username"
                var list = string.Join(";", all.ConvertAll(u => $"{u.Id}:{u.Username}"));
                return $"OK|{list}";
            }

            return "ERR|Unknown command";
        }

        static void SendResponse(NetworkDataHelper helper, string response)
        {
            byte[] responseBytes = Encoding.UTF8.GetBytes(response);
            ushort respLen = (ushort)responseBytes.Length;
            byte[] respLenBytes = BitConverter.GetBytes(respLen);

            helper.Send(respLenBytes);
            helper.Send(responseBytes);
        }
    }
}
