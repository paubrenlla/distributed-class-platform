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
            
            // TODO: Leer IP y Puerto desde un archivo de configuración
            IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000);
            
            try
            {
                clientSocket.Connect(serverEndpoint);
                Console.WriteLine("Connected to server!!");
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to connect to server: " + e.Message);
                return;
            }

            NetworkDataHelper networkDataHelper = new NetworkDataHelper(clientSocket);
            bool clientRunning = true;

            while (clientRunning)
            {
                Console.WriteLine("\nType a command (create/login/listclasses/createclass/exit):");
                var input = Console.ReadLine()?.Trim().ToLower();

                if (input == "exit")
                {
                    clientRunning = false;
                    continue; // Salta al final del bucle para cerrar la conexión
                }

                Frame requestFrame = null;

                try
                {
                    switch (input)
                    {
                        case "create":
                            Console.Write("Enter new username: ");
                            string newUsername = Console.ReadLine();
                            Console.Write("Enter new password: ");
                            string newPassword = Console.ReadLine();
                
                            string createPayload = $"{newUsername}|{newPassword}";
                
                            requestFrame = new Frame
                            {
                                Header = ProtocolConstants.Request,
                                Command = ProtocolConstants.CommandCreateUser,
                                Data = Encoding.UTF8.GetBytes(createPayload)
                            };
                            break;

                        case "login":
                            Console.Write("Username: ");
                            string username = Console.ReadLine();
                            Console.Write("Password: ");
                            string password = Console.ReadLine();
                
                            string loginPayload = $"{username}|{password}";
                
                            requestFrame = new Frame
                            {
                                Header = ProtocolConstants.Request,
                                Command = ProtocolConstants.CommandLogin,
                                Data = Encoding.UTF8.GetBytes(loginPayload)
                            };
                            break;

                        case "listclasses":
                            requestFrame = new Frame
                            {
                                Header = ProtocolConstants.Request,
                                Command = ProtocolConstants.CommandListClasses,
                                Data = null
                            };
                            break;

                        case "createclass":
                            Console.Write("Nombre de la clase: ");
                            string name = Console.ReadLine();
                            Console.Write("Descripción: ");
                            string desc = Console.ReadLine();
                            Console.Write("Cupo máximo: ");
                            string capacity = Console.ReadLine();
                            Console.Write("Duración (minutos): ");
                            string duration = Console.ReadLine();

                            string payload = $"{name}|{desc}|{capacity}|{duration}";
                
                            requestFrame = new Frame
                            {
                                Header = ProtocolConstants.Request,
                                Command = ProtocolConstants.CommandCreateClass,
                                Data = Encoding.UTF8.GetBytes(payload)
                            };
                            break;
                        default:
                            Console.WriteLine("Command not recognized.");
                            break;
                    }
                    
                    if (requestFrame != null)
                    {
                        // Enviar la trama de solicitud al servidor
                        networkDataHelper.Send(requestFrame);
                        Console.WriteLine("Request sent to server...");

                        // Esperar y recibir la trama de respuesta
                        Frame serverResponse = networkDataHelper.Receive();
                        string responseData = "No data received.";
                        if (serverResponse.Data != null)
                        {
                           responseData = Encoding.UTF8.GetString(serverResponse.Data);
                        }
                        
                        Console.WriteLine($"-> Server Response (CMD {serverResponse.Command}): {responseData}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred: " + e.Message);
                    clientRunning = false; // Termina el cliente si hay un error de red
                }
            }

            Console.WriteLine("Closing connection...");
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
        }
        
        static void ProcessServerResponse(string response)
        {
            var parts = response.Split(new[] { '|' }, 2);
            var status = parts[0];
            var data = parts.Length > 1 ? parts[1] : string.Empty;

            Console.WriteLine($"-> Status: {status}");
            if (!string.IsNullOrEmpty(data))
            {
                // Si la respuesta es una lista de clases, la formateamos
                if (response.Contains("\n")) 
                {
                    var classLines = data.Split('\n');
                    Console.WriteLine("  ID | Nombre de la Clase | Cupos | ¿Tiene Portada?");
                    Console.WriteLine("  ---|--------------------|-------|----------------");
                    foreach (var line in classLines)
                    {
                        var classData = line.Split('|');
                        // Formato: Id|Nombre|CuposOcupados|CupoMaximo|TieneImagen
                        Console.WriteLine($"  {classData[0],-2} | {classData[1],-18} | {classData[2]}/{classData[3],-4} | {classData[4]}");
                    }
                }
                else
                {
                    Console.WriteLine($"   Message: {data}");
                }
            }
        }
    }
}