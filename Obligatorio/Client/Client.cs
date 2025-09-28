using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Cliente;
using Common;

namespace Client
{
    internal class Program
    {
        private static NetworkDataHelper _networkHelper;
        private enum AuthResult { LoginSuccess, Exit }
        private enum MenuStatus { Unknown, LogOut, Escape}

        static void Main(string[] args)
        {
            Console.WriteLine("Iniciando Cliente...");

            SettingsManager settingsMgr = new SettingsManager();

            // IP y puerto del cliente
            IPAddress clientIp = IPAddress.Parse(settingsMgr.ReadSetting(ClientConfig.ClientIpConfigKey));
            int clientPort = int.Parse(settingsMgr.ReadSetting(ClientConfig.ClientPortConfigKey));
            IPEndPoint localEndpoint = new IPEndPoint(clientIp, clientPort);

            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Bind(localEndpoint);

            // IP y puerto del servidor
            IPAddress serverIp = IPAddress.Parse(settingsMgr.ReadSetting(ServerConfig.ServerIpConfigKey));
            int serverPort = int.Parse(settingsMgr.ReadSetting(ServerConfig.SeverPortConfigKey));
            IPEndPoint serverEndpoint = new IPEndPoint(serverIp, serverPort);

            try
            {
                clientSocket.Connect(serverEndpoint);
                Console.WriteLine("¡Conectado al servidor!");
                _networkHelper = new NetworkDataHelper(clientSocket);

                bool appIsRunning = true;
                while (appIsRunning)
                {
                    AuthResult authResult = RunAuthMenu();

                    if (authResult == AuthResult.LoginSuccess)
                    {
                        var menuStatus = RunMainMenu();

                        if (menuStatus != MenuStatus.LogOut)
                        {
                            break;
                        }
                    }
                    else
                    {
                        appIsRunning = false;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Conexión perdida con el servidor: " + e.Message);
                Console.WriteLine("El servidor se ha desconectado.");
            }
            finally
            {
                Console.WriteLine("Cerrando conexión...");
                try
                {
                    if (clientSocket.Connected)
                    {
                        clientSocket.Shutdown(SocketShutdown.Both);
                    }
                }
                catch (SocketException) { }
                catch (ObjectDisposedException) { }
                finally
                {
                    try { clientSocket.Close(); } catch { }
                }

                Console.WriteLine("Cliente finalizado. Presione Enter para salir...");
                Console.ReadLine();
            }
        }

        
        private static AuthResult RunAuthMenu()
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
                        return AuthResult.Exit;
                    default:
                        Console.WriteLine("Opción no válida. Intente de nuevo.");
                        continue;
                }

                if (requestFrame != null)
                {
                    Frame responseFrame = SendAndReceiveFrame(requestFrame);
                    string responseData = Encoding.UTF8.GetString(responseFrame.Data ?? new byte[0]);

                    if (requestFrame.Command == ProtocolConstants.CommandLogin && responseData.StartsWith("OK"))
                    {
                        ProcessSimpleResponse(responseData);
                        return AuthResult.LoginSuccess; 
                    }
                    else
                    {
                        ProcessSimpleResponse(responseData);
                    }
                }
            }
        }
        
        private static MenuStatus RunMainMenu()
        {
            MenuStatus menuStatus = MenuStatus.Unknown;
            bool sessionRunning = true;
            while (sessionRunning)
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
                Console.WriteLine("9. Cerrar sesion");
                Console.WriteLine("10. Salir de la Aplicación");
                Console.WriteLine("11. Descargar portada");
                Console.Write("Seleccione una opción: ");

                string input = Console.ReadLine();
                Frame requestFrame = null;

                switch (input)
                {
                    case "1": //Listar todas las clases disponibles
                        requestFrame = new Frame
                        {
                            Header = ProtocolConstants.Request,
                            Command = ProtocolConstants.SearchAvailableClasses, 
                            Data = null
                        };
                        break;
                    case "2": //Crear una clase
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

                        Console.Write("Ruta de la imagen de la clase (dejar vacío si no se quiere ingresar imagen): ");
                        string imagePath = Console.ReadLine();

                        string payload = $"{name}|{desc}|{capacity}|{duration}|{startDateStr}";
                        Frame classFrame = new Frame
                        {
                            Header = ProtocolConstants.Request,
                            Command = ProtocolConstants.CommandCreateClass,
                            Data = Encoding.UTF8.GetBytes(payload)
                        };
                        Frame classCreated = SendAndReceiveFrame(classFrame);
                        string classCreatedStr = Encoding.UTF8.GetString(classCreated.Data ?? new byte[0]).Trim();

                        int createdClassId = -1;
                        if (classCreatedStr.StartsWith("OK|"))
                        {
                            var p = classCreatedStr.Split('|', 2);
                            if (int.TryParse(p.Length > 1 ? p[1].Trim() : "", out int tmpId1))
                                createdClassId = tmpId1;
                            else // lo hacemos con linq por las dudas por si no llega a agarrar la id
                            {
                                
                                var digits = new string((p.Length > 1 ? p[1] : "").Where(ch => char.IsDigit(ch)).ToArray());
                                int.TryParse(digits, out createdClassId);
                            }
                        }
                        else
                        {
                            int.TryParse(classCreatedStr, out createdClassId);
                        }

                        if (createdClassId <= 0)
                        {
                            Console.WriteLine("No se pudo obtener el ID de la clase creada. Se omitirá la subida de imagen.");
                            requestFrame = null;
                            break;
                        }

                        if (!string.IsNullOrEmpty(imagePath))
                        {
                            bool ok = UploadImage(imagePath, createdClassId);
                            if (!ok)
                            {
                                Console.WriteLine("⚠️ La imagen no pudo subirse. La clase se creó igual sin portada.");
                            }
                        }
                        requestFrame = null; // Para evitar que se vuelva a enviar al final del while
                        break;
                    case "3": //Inscribirse a una clase
                        Console.Write("Ingresa el ID de la clase a la que quieres inscribirte: ");
                        string classId = Console.ReadLine();
    
                        requestFrame = new Frame
                        {
                            Header = ProtocolConstants.Request,
                            Command = ProtocolConstants.CommandSubscribeToClass,
                            Data = Encoding.UTF8.GetBytes(classId)
                        };
                        break;
                    case "4": //Cancelar inscripción
                        Console.Write("Ingresa el ID de la clase para cancelar tu inscripción: ");
                        string classIdToCancel = Console.ReadLine();

                        requestFrame = new Frame
                        {
                            Header = ProtocolConstants.Request,
                            Command = ProtocolConstants.CommandCancelSubscription,
                            Data = Encoding.UTF8.GetBytes(classIdToCancel)
                        };
                        break;
                    case "5": //Ver mi historial de actividades
                        requestFrame = new Frame
                        {
                            Header = ProtocolConstants.Request,
                            Command = ProtocolConstants.CommandShowHistory,
                            Data = null 
                        };
                        break;
                    case "6": //Modificar una clase (creador)
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
                        Console.Write("Ruta de nueva imagen (vacío para no cambiar): ");
                        string modImagePath = Console.ReadLine();

                        string modPayload = $"{modId}|{modName}|{modDesc}|{modCap}|{modDur}|{modDate}";
    
                        requestFrame = new Frame
                        {
                            Header = ProtocolConstants.Request,
                            Command = ProtocolConstants.CommandModifyClass,
                            Data = Encoding.UTF8.GetBytes(modPayload)
                        };
                        Frame modResponse = SendAndReceiveFrame(requestFrame);
                        string modRespStr = Encoding.UTF8.GetString(modResponse.Data ?? new byte[0]);
                        ProcessSimpleResponse(modRespStr);
                        if (modRespStr.StartsWith("OK") && !string.IsNullOrEmpty(modImagePath))
                        {
                            bool ok = UploadImage(modImagePath, int.Parse(modId));
                            if (!ok)
                            {
                                Console.WriteLine("La imagen no pudo subirse. La clase se creó igual sin portada.");
                            }
                        }
                        requestFrame = null;
                        break;
                    case "7": //Eliminar una clase (creador)
                        Console.Write("Ingresa el ID de la clase a eliminar: ");
                        string deleteId = Console.ReadLine();
    
                        requestFrame = new Frame
                        {
                            Header = ProtocolConstants.Request,
                            Command = ProtocolConstants.CommandDeleteClass,
                            Data = Encoding.UTF8.GetBytes(deleteId)
                        };
                        break;
                    case "8": //Buscar clases
                        Console.WriteLine("Selecciona el tipo de filtro: 1=Nombre, 2=Descripción, 3=Disponibilidad mínima de cupos, 4=Todas las clases existentes");
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
                        if(requestFrame != null) requestFrame.Header = ProtocolConstants.Request;
                        break;
                    case "9": // Cerrar Sesión
                        requestFrame = new Frame
                        {
                            Header = ProtocolConstants.Request,
                            Command = ProtocolConstants.CommandLogout,
                            Data = null
                        };
                        Frame response = SendAndReceiveFrame(requestFrame);
                        ProcessSimpleResponse(Encoding.UTF8.GetString(response.Data));
                        menuStatus = MenuStatus.LogOut;
                        sessionRunning = false;
                        continue;
                    case "10": // Salir de la Aplicación
                        SendAndReceiveFrame(new Frame { Command = ProtocolConstants.CommandLogout, Header = ProtocolConstants.Request });
                        menuStatus = MenuStatus.Escape;
                        sessionRunning = false;
                        continue;
                    case "11": // Descargar portada
                    {
                        try
                        {
                            Console.Write("Ingresa el ID de la clase: ");
                            string downloadId = Console.ReadLine();

                            Frame downloadFrame = new Frame
                            {
                                Header = ProtocolConstants.Request,
                                Command = ProtocolConstants.CommandDownloadImage,
                                Data = Encoding.UTF8.GetBytes(downloadId)
                            };

                            _networkHelper.Send(downloadFrame);

                            Frame metaFrame = _networkHelper.Receive();
                            string metaStr = Encoding.UTF8.GetString(metaFrame.Data ?? new byte[0]);
                            if (!metaStr.StartsWith("OK|"))
                            {
                                Console.WriteLine($"Error: {metaStr}");
                                break;
                            }

                            string[] parts = metaStr.Substring(3).Split('|'); // "OK|filename|filesize"
                            if (parts.Length < 2)
                            {
                                Console.WriteLine("Error en metadata recibida.");
                                break;
                            }

                            string fileName = parts[0];
                            if (!long.TryParse(parts[1], out long fileSize))
                            {
                                Console.WriteLine("Tamaño de archivo inválido.");
                                break;
                            }

                            string imagesPath = Path.Combine(AppContext.BaseDirectory, "ClienteImages");
                            Directory.CreateDirectory(imagesPath);
                            string filePath = Path.Combine(imagesPath, fileName);

                            if (File.Exists(filePath))
                            {
                                File.Delete(filePath);
                                Console.WriteLine($"Archivo existente '{fileName}' eliminado antes de descargar.");
                            }

                            long offset = 0;
                            long partCount = ProtocolConstants.CalculateFileParts(fileSize);
                            long currentPart = 1;

                            FileStreamHelper fsh = new FileStreamHelper();

                            while (offset < fileSize)
                            {
                                byte[] buffer;
                                bool isLastPart = (currentPart == partCount);

                                if (!isLastPart)
                                {
                                    buffer = _networkHelper.Receive(ProtocolConstants.MaxFilePartSize);
                                    offset += ProtocolConstants.MaxFilePartSize;
                                }
                                else
                                {
                                    long lastPartSize = fileSize - offset;
                                    buffer = _networkHelper.Receive((int)lastPartSize);
                                    offset += lastPartSize;
                                }

                                fsh.Write(filePath, buffer);
                                currentPart++;
                            }

                            Console.WriteLine($"Imagen descargada en: {filePath}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error al descargar la portada: {ex.Message}");
                        }

                        requestFrame = null;
                        break;
                    }

                        

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
            return menuStatus;
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
                Console.WriteLine("Conexión perdida con el servidor: " + e.Message);
                throw;
            }
        }

        
        private static void SendFrame(Frame frame)
        {
            try
            {
                _networkHelper.Send(frame);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error de comunicación: " + e.Message);
                byte[] errorData = Encoding.UTF8.GetBytes("ERR|Error de red");
            }
        }
        
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

            var classTableCommands = new short[]
            {
                ProtocolConstants.CommandListClasses,
                ProtocolConstants.SearchAvailableClasses,
                ProtocolConstants.SearchClassesByNamwe, 
                ProtocolConstants.SearchClassesByDescription,
                ProtocolConstants.SearchClassesByAvailabilty
            };

            if (Array.Exists(classTableCommands, cmd => cmd == responseFrame.Command) && status == "OK")
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
            else if (responseFrame.Command == ProtocolConstants.CommandShowHistory && status == "OK")
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
            else
            {
                Console.WriteLine($"   Message: {data}");
            }
        }
        
        private static bool UploadImage(string imagePath, int classId)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                {
                    Console.WriteLine("No se encontró el archivo de imagen.");
                    return false;
                }

                FileInfo fi = new FileInfo(imagePath);
                string fileName = fi.Name;
                byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
                int fileNameLength = fileNameBytes.Length;
                long fileSize = fi.Length;

                FileStreamHelper fsh = new FileStreamHelper();
                Frame enviarImagen = new Frame
                {
                    Header = ProtocolConstants.Request,
                    Command = ProtocolConstants.CommandUploadImage,
                    Data = Array.Empty<byte>()
                };
                SendFrame(enviarImagen);

                _networkHelper.Send(BitConverter.GetBytes(classId));
                _networkHelper.Send(BitConverter.GetBytes(fileNameLength));
                _networkHelper.Send(fileNameBytes);
                _networkHelper.Send(BitConverter.GetBytes(fileSize));

                Frame metaResponse = _networkHelper.Receive();
                string metaRespStr = Encoding.UTF8.GetString(metaResponse.Data ?? Array.Empty<byte>());
                ProcessSimpleResponse(metaRespStr);

                if (!metaRespStr.StartsWith("OK"))
                {
                    Console.WriteLine("El servidor rechazó la subida de la imagen.");
                    return false;
                }

                long offset = 0;
                long partCount = ProtocolConstants.CalculateFileParts(fileSize);
                long currentPart = 1;

                while (offset < fileSize)
                {
                    byte[] buffer;
                    bool isLastPart = (currentPart == partCount);

                    if (!isLastPart)
                    {
                        buffer = fsh.Read(imagePath, offset, ProtocolConstants.MaxFilePartSize);
                        offset += ProtocolConstants.MaxFilePartSize;
                    }
                    else
                    {
                        long lastPartSize = fileSize - offset;
                        buffer = fsh.Read(imagePath, offset, (int)lastPartSize);
                        offset += lastPartSize;
                    }

                    _networkHelper.Send(buffer);
                    Console.WriteLine($"Enviando segmento {currentPart}/{partCount}...");
                    currentPart++;
                }

                Frame imageResponse = _networkHelper.Receive();
                string imageRespStr = Encoding.UTF8.GetString(imageResponse.Data ?? Array.Empty<byte>());
                ProcessSimpleResponse(imageRespStr);

                if (!imageRespStr.StartsWith("OK"))
                {
                    Console.WriteLine("El servidor no confirmó la imagen.");
                    return false;
                }

                Console.WriteLine("Imagen subida correctamente.");
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error al enviar la imagen: " + e.Message);
                return false;
            }
        }


        
    }
}