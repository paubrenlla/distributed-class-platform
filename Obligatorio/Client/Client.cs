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
                Console.WriteLine("\nType a command (create/login/listclasses/createclass/subscribe/cancel/history/availableclasses/searchclasses/exit):");

                var input = Console.ReadLine()?.Trim().ToLower();

                if (input == "exit")
                {
                    clientRunning = false;
                    continue;
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
                            Console.Write("Fecha y hora de inicio (formato AAAA-MM-DD HH:MM): ");
                            string startDateStr = Console.ReadLine();

                            string payload = $"{name}|{desc}|{capacity}|{duration}|{startDateStr}";
                
                            requestFrame = new Frame
                            {
                                Header = ProtocolConstants.Request,
                                Command = ProtocolConstants.CommandCreateClass,
                                Data = Encoding.UTF8.GetBytes(payload)
                            };
                            break;
                        case "subscribe":
                            Console.Write("Ingresa el ID de la clase a la que quieres inscribirte: ");
                            string classId = Console.ReadLine();
    
                            requestFrame = new Frame
                            {
                                Header = ProtocolConstants.Request,
                                Command = ProtocolConstants.CommandSubscribeToClass,
                                Data = Encoding.UTF8.GetBytes(classId)
                            };
                            break;
                        case "cancel":
                            Console.Write("Ingresa el ID de la clase para cancelar tu inscripción: ");
                            string classIdToCancel = Console.ReadLine();
    
                            requestFrame = new Frame
                            {
                                Header = ProtocolConstants.Request,
                                Command = ProtocolConstants.CommandCancelSubscription,
                                Data = Encoding.UTF8.GetBytes(classIdToCancel)
                            };
                            break;
                        case "history":
                            requestFrame = new Frame
                            {
                                Header = ProtocolConstants.Request,
                                Command = ProtocolConstants.CommandShowHistory,
                                Data = null 
                            };
                            break;
 
                        case "availableclasses":
                            requestFrame = new Frame
                            {
                                Header = ProtocolConstants.Request,
                                Command = ProtocolConstants.SearchAvailableClasses,
                                Data = null
                            };
                            break;

                        case "searchclasses":
                            Console.WriteLine("Selecciona el tipo de filtro: 1=Nombre, 2=Descripción, 3=Disponibilidad mínima de cupos");
                            string filtroTipo = Console.ReadLine()?.Trim();

                            Frame searchFrame = null;

                            switch (filtroTipo)
                            {
                                case "1": 
                                    Console.Write("Ingresa el nombre a buscar: ");
                                    string filtroNombre = Console.ReadLine();
                                    searchFrame = new Frame
                                    {
                                        Header = ProtocolConstants.Request,
                                        Command = ProtocolConstants.SearchClassesByNamwe,
                                        Data = Encoding.UTF8.GetBytes(filtroNombre)
                                    };
                                    break;
                                case "2": 
                                    Console.Write("Ingresa la descripción a buscar: ");
                                    string filtroDesc = Console.ReadLine();
                                    searchFrame = new Frame
                                    {
                                        Header = ProtocolConstants.Request,
                                        Command = ProtocolConstants.SearchClassesByDescription,
                                        Data = Encoding.UTF8.GetBytes(filtroDesc)
                                    };
                                    break;
                                case "3": 
                                    Console.Write("Ingresa la cantidad mínima de cupos disponibles: ");
                                    string minCuposStr = Console.ReadLine();
                                    searchFrame = new Frame
                                    {
                                        Header = ProtocolConstants.Request,
                                        Command = ProtocolConstants.SearchClassesByAvailabilty,
                                        Data = Encoding.UTF8.GetBytes(minCuposStr)
                                    };
                                    break;
                                default:
                                    Console.WriteLine("Opción inválida.");
                                    break;
                            }

                            if (searchFrame != null)
                                requestFrame = searchFrame;
                            break;


                        case "modify":
                            Console.Write("Ingresa el ID de la clase a modificar: ");
                            string modId = Console.ReadLine();
                            Console.Write("Nuevo nombre: ");
                            string modName = Console.ReadLine();
                            Console.Write("Nueva descripción: ");
                            string modDesc = Console.ReadLine();
                            Console.Write("Nuevo cupo: ");
                            string modCap = Console.ReadLine();
                            Console.Write("Nueva duración (min): ");
                            string modDur = Console.ReadLine();
                            Console.Write("Nueva fecha (AAAA-MM-DD HH:MM): ");
                            string modDate = Console.ReadLine();

                            string modPayload = $"{modId}|{modName}|{modDesc}|{modCap}|{modDur}|{modDate}";
    
                            requestFrame = new Frame
                            {
                                Header = ProtocolConstants.Request,
                                Command = ProtocolConstants.CommandModifyClass,
                                Data = Encoding.UTF8.GetBytes(modPayload)
                            };
                            break;

                        case "delete":
                            Console.Write("Ingresa el ID de la clase a eliminar: ");
                            string deleteId = Console.ReadLine();
    
                            requestFrame = new Frame
                            {
                                Header = ProtocolConstants.Request,
                                Command = ProtocolConstants.CommandDeleteClass,
                                Data = Encoding.UTF8.GetBytes(deleteId)
                            };
                            break;
 
                        default:
                            Console.WriteLine("Command not recognized.");
                            break;
                    }
                    
                    if (requestFrame != null)
                    {
                        networkDataHelper.Send(requestFrame);
                        Console.WriteLine("Request sent to server...");

                        Frame serverResponse = networkDataHelper.Receive();
                        string responseData = "No data received.";
                        if (serverResponse.Data != null)
                        {
                           responseData = Encoding.UTF8.GetString(serverResponse.Data);
                        }

                        ProcessServerResponse(responseData, serverResponse.Command);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred: " + e.Message);
                    clientRunning = false;
                }
            }

            Console.WriteLine("Closing connection...");
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
        }
        
        static void ProcessServerResponse(string response, short command)
        {
            var parts = response.Split(new[] { '|' }, 2);
            var status = parts[0];
            var data = parts.Length > 1 ? parts[1] : string.Empty;

            Console.WriteLine($"-> Status: {status}");

            if (command == ProtocolConstants.CommandListClasses && status == "OK")
            {
                if (string.IsNullOrEmpty(data) || !data.Contains("|"))
                {
                    Console.WriteLine($"   Message: {data}");
                }
                else
                {
                    var classLines = data.Split('\n');
                    Console.WriteLine("  ID | Nombre            | Fecha de Inicio     | Cupos   | Portada");
                    Console.WriteLine("  ---|-------------------|---------------------|---------|---------");
                    foreach (var line in classLines)
                    {
                        if (string.IsNullOrEmpty(line)) continue;
                        var classData = line.Split('|');
                        if (classData.Length >= 6)
                        {
                            string cupos = $"{classData[3]}/{classData[4]}";
                            Console.WriteLine($"  {classData[0],-2} | {classData[1],-17} | {classData[2],-19} | {cupos,-7} | {classData[5]}");
                        }

                    }
                }
            }
            else if (command == ProtocolConstants.CommandShowHistory && status == "OK")
            {
                if (string.IsNullOrEmpty(data) || !data.Contains("|"))
                {
                    Console.WriteLine($"   Message: {data}");
                }
                else
                {
                    var historyLines = data.Split('\n');
                    Console.WriteLine("  Nombre de la Clase    | Fecha de Inicio     | Estado");
                    Console.WriteLine("  ----------------------|---------------------|-----------");
                    foreach (var line in historyLines)
                    {
                        if (string.IsNullOrEmpty(line)) continue;
                        var historyData = line.Split('|');
                        if (historyData.Length >= 3)
                        {
                            Console.WriteLine($"  {historyData[0],-21} | {historyData[1],-19} | {historyData[2]}");
                        }
                    }
                }
            }
            else if ((command == ProtocolConstants.CommandListClasses ||
                      command == ProtocolConstants.SearchAvailableClasses ||
                      command == ProtocolConstants.SearchClassesByNamwe ||
                      command == ProtocolConstants.SearchClassesByDescription ||
                      command == ProtocolConstants.SearchClassesByAvailabilty)
                     && status == "OK")
            {
                if (string.IsNullOrEmpty(data) || !data.Contains("|"))
                {
                    Console.WriteLine($"   Message: {data}");
                }
                else
                {
                    var classLines = data.Split('\n');
                    Console.WriteLine("  ID | Nombre            | Fecha de Inicio     | Cupos   | Portada");
                    Console.WriteLine("  ---|-------------------|---------------------|---------|---------");
                    foreach (var line in classLines)
                    {
                        if (string.IsNullOrEmpty(line)) continue;
                        var classData = line.Split('|');
                        if (classData.Length >= 6)
                        {
                            string cupos = $"{classData[3]}/{classData[4]}";
                            Console.WriteLine($"  {classData[0],-2} | {classData[1],-17} | {classData[2],-19} | {cupos,-7} | {classData[5]}");
                        }
                    }
                }
            }

            else
            {
                Console.WriteLine($"   Message: {data}");
            }
        }
    }
}