using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Common;

namespace Client
{
    internal class Program
    {
        private static NetworkDataHelper _networkHelper;

        static void Main(string[] args)
        {
            Console.WriteLine("Iniciando Cliente...");
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // TODO: Leer IP y Puerto desde un archivo de configuración
            IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000);
            
            try
            {
                clientSocket.Connect(serverEndpoint);
                Console.WriteLine("¡Conectado al servidor!");
                _networkHelper = new NetworkDataHelper(clientSocket);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error al conectar con el servidor: " + e.Message);
                return;
            }

            if (RunAuthMenu())
            {
                RunMainMenu();
            }

            Console.WriteLine("Cerrando conexión...");
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
        }
        
        private static bool RunAuthMenu()
        {
            while (true)
            {
                Console.WriteLine("\n--- Menú de Autenticación ---");
                Console.WriteLine("1. Iniciar Sesión");
                Console.WriteLine("2. Crear Usuario");
                Console.WriteLine("3. Salir");
                Console.Write("Seleccione una opción: ");

                string input = Console.ReadLine();
                Frame requestFrame = null;

                switch (input)
                {
                    case "1": // Iniciar Sesión
                        Console.Write("Usuario: ");
                        string username = Console.ReadLine();
                        Console.Write("Contraseña: ");
                        string password = Console.ReadLine();
                        requestFrame = new Frame
                        {
                            Header = ProtocolConstants.Request,
                            Command = ProtocolConstants.CommandLogin,
                            Data = Encoding.UTF8.GetBytes($"{username}|{password}")
                        };
                        break;
                    case "2": // Crear Usuario
                        Console.Write("Nuevo Usuario: ");
                        string newUsername = Console.ReadLine();
                        Console.Write("Nueva Contraseña: ");
                        string newPassword = Console.ReadLine();
                        requestFrame = new Frame
                        {
                            Header = ProtocolConstants.Request,
                            Command = ProtocolConstants.CommandCreateUser,
                            Data = Encoding.UTF8.GetBytes($"{newUsername}|{newPassword}")
                        };
                        break;
                    case "3": // Salir
                        return false;
                    default:
                        Console.WriteLine("Opción no válida. Intente de nuevo.");
                        continue;
                }

                if (requestFrame != null)
                {
                    // Usamos el helper para recibir la respuesta completa del servidor
                    Frame responseFrame = SendAndReceiveFrame(requestFrame);
                    string responseData = Encoding.UTF8.GetString(responseFrame.Data ?? new byte[0]);

                    if (requestFrame.Command == ProtocolConstants.CommandLogin && responseData.StartsWith("OK"))
                    {
                        ProcessSimpleResponse(responseData);
                        return true; // Login exitoso
                    }
                    else
                    {
                        ProcessSimpleResponse(responseData);
                    }
                }
            }
        }
        
        private static void RunMainMenu()
        {
            bool clientRunning = true;
            while (clientRunning)
            {
                Console.WriteLine("\n--- Menú Principal ---");
                Console.WriteLine("1. Listar todas las clases disponibles");
                Console.WriteLine("2. Crear una clase");
                Console.WriteLine("3. Inscribirse a una clase");
                Console.WriteLine("4. Cancelar inscripción");
                Console.WriteLine("5. Ver mi historial de actividades");
                Console.WriteLine("6. Modificar una clase (creador)");
                Console.WriteLine("7. Eliminar una clase (creador)");
                Console.WriteLine("8. Buscar clases");
                Console.WriteLine("9. Salir");
                Console.Write("Seleccione una opción: ");

                string input = Console.ReadLine();
                Frame requestFrame = null;

                switch (input)
                {
                    case "1":
                        requestFrame = new Frame
                        {
                            Header = ProtocolConstants.Request,
                            Command = ProtocolConstants.SearchAvailableClasses, 
                            Data = null
                        };
                        break;
                    case "2":
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
                    case "3":
                        Console.Write("Ingresa el ID de la clase a la que quieres inscribirte: ");
                        string classId = Console.ReadLine();
    
                        requestFrame = new Frame
                        {
                            Header = ProtocolConstants.Request,
                            Command = ProtocolConstants.CommandSubscribeToClass,
                            Data = Encoding.UTF8.GetBytes(classId)
                        };
                        break;
                    case "4":
                        Console.Write("Ingresa el ID de la clase para cancelar tu inscripción: ");
                        string classIdToCancel = Console.ReadLine();

                        requestFrame = new Frame
                        {
                            Header = ProtocolConstants.Request,
                            Command = ProtocolConstants.CommandCancelSubscription,
                            Data = Encoding.UTF8.GetBytes(classIdToCancel)
                        };
                        break;
                    case "5":
                        requestFrame = new Frame
                        {
                            Header = ProtocolConstants.Request,
                            Command = ProtocolConstants.CommandShowHistory,
                            Data = null 
                        };
                        break;
                    case "6":
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
                    case "7":
                        Console.Write("Ingresa el ID de la clase a eliminar: ");
                        string deleteId = Console.ReadLine();
    
                        requestFrame = new Frame
                        {
                            Header = ProtocolConstants.Request,
                            Command = ProtocolConstants.CommandDeleteClass,
                            Data = Encoding.UTF8.GetBytes(deleteId)
                        };
                        break;
                    case "8":
                        Console.WriteLine("Selecciona el tipo de filtro: 1=Nombre, 2=Descripción, 3=Disponibilidad mínima de cupos 4=Todas las clases existentes");
                        string filtroTipo = Console.ReadLine()?.Trim();
                        switch (filtroTipo)
                        {
                            case "1": 
                                Console.Write("Ingresa el nombre a buscar: ");
                                string filtroNombre = Console.ReadLine();
                                requestFrame = new Frame { Command = ProtocolConstants.SearchClassesByNamwe, Data = Encoding.UTF8.GetBytes(filtroNombre) };
                                break;
                            case "2": 
                                Console.Write("Ingresa la descripción a buscar: ");
                                string filtroDesc = Console.ReadLine();
                                requestFrame = new Frame { Command = ProtocolConstants.SearchClassesByDescription, Data = Encoding.UTF8.GetBytes(filtroDesc) };
                                break;
                            case "3": 
                                Console.Write("Ingresa la cantidad mínima de cupos disponibles: ");
                                string minCuposStr = Console.ReadLine();
                                requestFrame = new Frame { Command = ProtocolConstants.SearchClassesByAvailabilty, Data = Encoding.UTF8.GetBytes(minCuposStr) };
                                break;
                            case "4": 
                                requestFrame = new Frame { Command = ProtocolConstants.CommandListClasses, Data = null };
                                break;
                            default:
                                Console.WriteLine("Opción inválida.");
                                break;
                        }
                        // Asignamos el header
                        if(requestFrame != null) requestFrame.Header = ProtocolConstants.Request;
                        break;
                    case "9":
                        clientRunning = false;
                        continue;
                    default:
                        Console.WriteLine("Opción no válida. Intente de nuevo.");
                        continue;
                }
                
                if (requestFrame != null)
                {
                    Frame responseFrame = SendAndReceiveFrame(requestFrame);
                    ProcessFullResponse(responseFrame); 
                }
            }
        }
        
        private static Frame SendAndReceiveFrame(Frame frame)
        {
            try
            {
                _networkHelper.Send(frame);
                return _networkHelper.Receive();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error de comunicación: " + e.Message);
                byte[] errorData = Encoding.UTF8.GetBytes("ERR|Error de red");
                return new Frame { Command = frame.Command, Data = errorData };
            }
        }
        
        // Para mensajes simples como login, create user, etc.
        private static void ProcessSimpleResponse(string response)
        {
            var parts = response.Split(new[] { '|' }, 2);
            var status = parts[0];
            var data = parts.Length > 1 ? parts[1] : string.Empty;
            Console.WriteLine($"-> Status: {status}\n   Message: {data}");
        }
        
        private static void ProcessFullResponse(Frame responseFrame)
        {
            string responseData = Encoding.UTF8.GetString(responseFrame.Data ?? new byte[0]);
            var parts = responseData.Split(new[] { '|' }, 2);
            var status = parts[0];
            var data = parts.Length > 1 ? parts[1] : string.Empty;

            Console.WriteLine($"-> Status: {status}");

            // Lista de comandos que deben ser mostrados como tabla de clases
            var classTableCommands = new short[]
            {
                ProtocolConstants.CommandListClasses,
                ProtocolConstants.SearchAvailableClasses,
                ProtocolConstants.SearchClassesByNamwe, 
                ProtocolConstants.SearchClassesByDescription,
                ProtocolConstants.SearchClassesByAvailabilty
            };

            // Si el comando es uno de los que lista clases
            if (Array.Exists(classTableCommands, cmd => cmd == responseFrame.Command) && status == "OK")
            {
                if (string.IsNullOrEmpty(data) || !data.Contains("|"))
                {
                    Console.WriteLine($"   Message: {data}");
                }
                else
                {
                    // Lógica para imprimir la tabla de clases
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
            else if (responseFrame.Command == ProtocolConstants.CommandShowHistory && status == "OK")
            {
                // Lógica para imprimir la tabla del historial
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
            else
            {
                Console.WriteLine($"   Message: {data}");
            }
        }
    }
}