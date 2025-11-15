using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;
using Common;
using Repository;
using Domain;

namespace Server
{
    internal class Program
    {
        static UserRepository userRepo = new UserRepository();
        static readonly OnlineClassRepository classRepo = new OnlineClassRepository();
        static readonly InscriptionRepository inscriptionRepo = new InscriptionRepository();
        public static readonly SemaphoreSlim ImageSemaphore = new SemaphoreSlim(1, 1); //esto es para hacer mutua exlucion con await en subir imagenes y eliminar clases
        static bool isRunning = true;
        static Socket serverSocket;
        static readonly List<Socket> connectedClients = new List<Socket>();
        static readonly object clientListLock = new object();
        static int maxClients = 3;
        static readonly ConcurrentDictionary<Socket, CancellationTokenSource> activeReportTasks = new ConcurrentDictionary<Socket, CancellationTokenSource>();
        private static CancellationTokenSource serverReportCts = null;
        
        static async Task Main(string[] args)
        {
            SeedData();
            await LogPublisher.InitAsync();
            Console.WriteLine("Server starting with preloaded data...");
            string serverHostnameString = Environment.GetEnvironmentVariable(ServerConfig.ServerIpConfigKey) ?? "0.0.0.0";
            string serverPortString = Environment.GetEnvironmentVariable(ServerConfig.SeverPortConfigKey) ?? "5000";

            IPAddress serverIp;
            if (serverHostnameString == "0.0.0.0")
            {
                serverIp = IPAddress.Any;
            }
            else
            {
                IPAddress[] addresses = Dns.GetHostAddresses(serverHostnameString);
                serverIp = addresses.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                           ?? throw new Exception($"Cannot resolve hostname: {serverHostnameString}");
            }

            int serverPort = int.Parse(serverPortString);
            IPEndPoint serverEndpoint = new IPEndPoint(serverIp, serverPort);

            
            string receiveDirectory = Environment.GetEnvironmentVariable(ServerConfig.ReceivedFilesFolder) ?? "ServerImages";
            if (!Directory.Exists(receiveDirectory))
            {
                Directory.CreateDirectory(receiveDirectory);
            }
            Console.WriteLine($"Carpeta de imágenes del servidor: {Path.GetFullPath(receiveDirectory)}");


            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(serverEndpoint);
            serverSocket.Listen(10);
            Console.WriteLine($"Servidor escuchando en {serverIp}:{serverPort}");
            Console.WriteLine("Presiona Ctrl+C para cerrar (en Docker, se detiene con docker stop)");

            Task acceptClientsTask = AcceptClients();
            
            Task consoleListenerTask = ListenForServerCommands();
            
            await Task.WhenAny(acceptClientsTask, consoleListenerTask);
            
            isRunning = false;
            serverReportCts?.Cancel();
            Console.WriteLine("Cerrando sockets...");
            CloseAllClientSockets(); 
            try { serverSocket?.Close(); } catch { }
            Console.WriteLine("Servidor detenido.");
        }

        static async Task AcceptClients()
        {
            while (isRunning)
            {
                try
                {
                    Console.WriteLine("Esperando nuevo cliente...");
                    Socket clientSocket = await serverSocket.AcceptAsync();
                    Console.WriteLine($"Nuevo socket aceptado: {clientSocket.RemoteEndPoint}");

                    lock (clientListLock)
                    {
                        connectedClients.Add(clientSocket);
                    }

                    Console.WriteLine($"Cliente conectado: {clientSocket.RemoteEndPoint}");

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await HandleClient(clientSocket);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Excepción en cliente {clientSocket.RemoteEndPoint}] {ex.Message}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error en AcceptClients] {ex.Message}");
                }
            }
        }
        
        static void CloseAllClientSockets()
        {
            lock (clientListLock)
            {
                foreach (var clientSocket in connectedClients)
                {
                    try { clientSocket.Shutdown(SocketShutdown.Both); } catch { }
                    try { clientSocket.Close(); } catch { }
                }
                connectedClients.Clear();
            }
        }
        
        static async Task HandleClient(Socket clientSocket)        {
            string clientId;
            try
            {
                clientId = clientSocket.RemoteEndPoint?.ToString() ?? "desconocido";
            }
            catch
            {
                clientId = "desconocido";
            }

            Console.WriteLine("Client connected: " + clientId);

            bool clientActive = true;
            NetworkDataHelper networkDataHelper = new NetworkDataHelper(clientSocket);
            User loggedInUser = null;
            
            while (clientActive)
            {
                Frame receivedFrame = null;
                try
                {
                    receivedFrame = await networkDataHelper.Receive();
                    Console.WriteLine($"Client {clientId} sent command: {receivedFrame.Command}");
                    
                    (var responseFrame, loggedInUser) = await ProcessCommand(
                        receivedFrame, loggedInUser, networkDataHelper, clientSocket, null); // null cts
        
                    if (responseFrame != null)
                        await networkDataHelper.Send(responseFrame);
                }
                catch (SocketException)
                {
                    clientActive = false;
                }
                catch (ObjectDisposedException)
                {
                    clientActive = false; // cierre controlado
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing client {clientId} request: {ex.Message}");
                    clientActive = false;
                }
            }

            try
            {
                Console.WriteLine("Client disconnected: " + clientId);
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
            }
            catch { }
        }
        
        static async Task<(Frame Frame, User UpdatedUser)> ProcessCommand(
            Frame frame,
            User loggedInUser,
            NetworkDataHelper networkDataHelper,
            Socket clientSocket,
            CancellationToken? cancellationToken 
        )
        {
            byte[] responseData;
            string responseMessage = null;
            if (loggedInUser != null && 
                frame.Command != ProtocolConstants.CommandLogin &&
                frame.Command != ProtocolConstants.CommandCreateUser
                )
            {
                Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss}] Usuario '{loggedInUser.Username}' ejecutó comando {frame.Command}");
            }

                    
            switch (frame.Command)
            {
                case ProtocolConstants.CommandCreateUser:
                    try
                    {
                        string payload = Encoding.UTF8.GetString(frame.Data);
                        var parts = payload.Split('|');
                        if (parts.Length < 2) throw new Exception("Formato incorrecto. Se esperaba 'usuario|clave'.");

                        var user = new User(parts[0], parts[1]);
                        userRepo.Add(user);
                        
                        responseMessage = $"OK|Usuario '{parts[0]}' creado exitosamente.";
                        _ = LogPublisher.Publish($"Usuario Creado {user.Username} ts={DateTimeOffset.UtcNow:o}");
                    }
                    catch (Exception ex)
                    {
                        responseMessage = $"ERR|{ex.Message}";
                    }
                    break;

                case ProtocolConstants.CommandLogin:
                    if (loggedInUser != null)
                    {
                        responseMessage = $"ERR|Ya has iniciado sesión como '{loggedInUser.Username}'.";
                        break;
                    }
                    try
                    {
                        string credentials = Encoding.UTF8.GetString(frame.Data);
                        var parts = credentials.Split('|');
                        if (parts.Length < 2) throw new Exception("Formato incorrecto. Se esperaba 'usuario|clave'.");

                        var user = userRepo.GetByUsername(parts[0]);
                        if (user != null && user.VerificarPassword(parts[1]))
                        {
                            loggedInUser = user;
                            responseMessage = $"OK|Bienvenido, {user.Username}!";
                            _ = LogPublisher.Publish($"Usuario logeado {user.Username} ts={DateTimeOffset.UtcNow:o}");
                        }
                        else
                        {
                            responseMessage = "ERR|Usuario o contraseña incorrectos.";
                        }
                    }
                    catch(Exception ex)
                    {
                        responseMessage = $"ERR|{ex.Message}";
                    }
                    break;
                    
                case ProtocolConstants.CommandListClasses:
                    if (loggedInUser == null)
                    {
                        responseMessage = "ERR|Debes iniciar sesión para ver las clases.";
                        break;
                    }

                    var allClasses = classRepo.GetAll();
                    if (allClasses.Count == 0)
                    {
                        responseMessage = "OK|No hay clases disponibles por el momento.";
                    }
                    else
                    {
                        var stringBuilder = new System.Text.StringBuilder();
                        stringBuilder.Append("OK|");
                        foreach (var onlineClass in allClasses)
                        {
                            var occupiedSlots = inscriptionRepo.GetActiveClassByClassId(onlineClass.Id).Count;
                            stringBuilder.Append($"{onlineClass.Id}|{onlineClass.Name}|{onlineClass.Description}|{onlineClass.Link}|{onlineClass.StartDate:dd/MM/yyyy HH:mm}|{occupiedSlots}|{onlineClass.MaxCapacity}|{onlineClass.Image != null}\n");
                        }
                        responseMessage = stringBuilder.ToString().TrimEnd('\n');
                    }
                    break;

                case ProtocolConstants.CommandCreateClass:
                    if (loggedInUser == null)
                    {
                        responseMessage = "ERR|Debes iniciar sesión para crear una clase.";
                        break;
                    }
                    try
                    {
                        string payload = Encoding.UTF8.GetString(frame.Data);
                        var parts = payload.Split('|');
                        if (parts.Length < 4) throw new Exception("Datos incompletos para crear la clase.");

                        string name = parts[0];
                        string description = parts[1];
                        int maxCapacity = int.Parse(parts[2]);
                        int duration = int.Parse(parts[3]);
                        DateTimeOffset startDate = DateTimeOffset.Parse(parts[4]);

                        
                        var newClass = new OnlineClass(name, description, maxCapacity, startDate, duration, loggedInUser);
                        classRepo.Add(newClass);

                        responseMessage = $"OK|Clase creada con éxito con el ID: {newClass.Id}";
                    }
                    catch (FormatException)
                    {
                        responseMessage = "ERR|El formato de la fecha es incorrecto. Use AAAA-MM-DD HH:MM";
                    }
                    catch (Exception ex)
                    {
                        responseMessage = $"ERR|{ex.Message}";
                    }
                    break;
                
                case ProtocolConstants.CommandSubscribeToClass:
                    if (loggedInUser == null)
                    {
                        responseMessage = "ERR|Debes iniciar sesión para inscribirte.";
                        break;
                    }
                    try
                    {
                        int classId = int.Parse(Encoding.UTF8.GetString(frame.Data));
                            var classToJoin = classRepo.GetById(classId);
                            if (classToJoin == null) throw new Exception("La clase no existe.");

                            if (inscriptionRepo.GetActiveByUserAndClass(loggedInUser.Id, classId) != null)
                                throw new Exception("Ya estás inscrito en esta clase.");

                            var activeInscriptions = inscriptionRepo.GetActiveClassByClassId(classId);
                            if (activeInscriptions.Count >= classToJoin.MaxCapacity)
                                throw new Exception("La clase no tiene cupos disponibles.");

                            var newInscription = new Inscription(loggedInUser, classToJoin);
                            inscriptionRepo.Add(newInscription);

                            responseMessage = $"OK|Inscripción a '{classToJoin.Name}' realizada con éxito.";
                        
                    }
                    catch (Exception ex)
                    {
                        responseMessage = $"ERR|{ex.Message}";
                    }
                    break;

                case ProtocolConstants.CommandCancelSubscription:
                    if (loggedInUser == null)
                    {
                        responseMessage = "ERR|Debes iniciar sesión para cancelar una inscripción.";
                        break;
                    }
                    try
                    {
                        int classId = int.Parse(Encoding.UTF8.GetString(frame.Data));
        
                        
                            var inscription = inscriptionRepo.GetActiveByUserAndClass(loggedInUser.Id, classId);
                            if (inscription == null) 
                                throw new Exception("No estás inscrito en esta clase.");

                            var remainingTime = inscription.Class.StartDate - DateTimeOffset.UtcNow;
                            if (remainingTime.TotalMinutes < 2)
                                throw new InvalidOperationException("No se puede cancelar la inscripción con menos de 2 minutos de antelación.");

                            inscription.Cancel();
            
                            responseMessage = $"OK|Tu inscripción a la clase '{inscription.Class.Name}' ha sido cancelada.";
                        
                    }
                    catch (Exception ex)
                    {
                        responseMessage = $"ERR|{ex.Message}";
                    }
                    break;

                case ProtocolConstants.CommandShowHistory:
                    if (loggedInUser == null)
                    {
                        responseMessage = "ERR|Debes iniciar sesión para ver tu historial.";
                        break;
                    }
                    
                    var userInscriptions = inscriptionRepo.GetByUser(loggedInUser.Id);
                    if (userInscriptions.Count == 0)
                    {
                        responseMessage = "OK|No tienes actividad en tu historial.";
                    }
                    else
                    {
                        var stringBuilder = new System.Text.StringBuilder();
                        stringBuilder.Append("OK|");
                        foreach (var insp in userInscriptions)
                        {
                            stringBuilder.Append($"{insp.Class.Name}|{insp.Class.StartDate:dd/MM/yyyy HH:mm}|{insp.Status}\n");
                        }
                        responseMessage = stringBuilder.ToString().TrimEnd('\n');
                    }
                    break;
                
                case ProtocolConstants.SearchAvailableClasses:
                {
                    if (loggedInUser == null)
                    {
                        responseMessage = "ERR|Debes iniciar sesión para buscar clases.";
                        break;
                    }

                    var disponibles = classRepo
                        .GetAll()
                        .Where(c => DateTimeOffset.UtcNow < c.StartDate && (c.MaxCapacity - inscriptionRepo.GetActiveClassByClassId(c.Id).Count) > 0)
                        .ToList();
                    
                    if (disponibles.Count == 0)
                    {
                        responseMessage = "OK|No hay clases disponibles por el momento.";
                    }
                    else
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.Append("OK|");
                        foreach (var c in disponibles)
                        {
                            var occupiedSlots = inscriptionRepo.GetActiveClassByClassId(c.Id).Count;
                            string imageName = string.IsNullOrEmpty(c.Image) ? "-" : c.Image;
                            sb.Append($"{c.Id}|{c.Name}|{c.Description}|{c.Link}|{c.StartDate:dd/MM/yyyy HH:mm}|{occupiedSlots}|{c.MaxCapacity}|{imageName}\n");
                        }
                        responseMessage = sb.ToString().TrimEnd('\n');

                    }
                    break;
                }

                case ProtocolConstants.SearchClassesByNamwe:
                {
                    if (loggedInUser == null)
                    {
                        responseMessage = "ERR|Debes iniciar sesión para buscar clases.";
                        break;
                    }

                    string filtroNombre = Encoding.UTF8.GetString(frame.Data);
                    var clases = classRepo.GetAll()
                        .Where(c => c.Name.Contains(filtroNombre, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (clases.Count == 0)
                    {
                        responseMessage = $"OK|No hay clases con nombre que contenga '{filtroNombre}'.";
                    }
                    else
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.Append("OK|");
                        foreach (var c in clases)
                        {
                            var occupiedSlots = inscriptionRepo.GetActiveClassByClassId(c.Id).Count;
                            sb.Append($"{c.Id}|{c.Name}|{c.Description}|{c.Link}|{c.StartDate:dd/MM/yyyy HH:mm}|{occupiedSlots}|{c.MaxCapacity}|{c.Image != null}\n");
                        }
                        responseMessage = sb.ToString().TrimEnd('\n');
                    }
                    break;
                }

                case ProtocolConstants.SearchClassesByDescription:
                {
                    if (loggedInUser == null)
                    {
                        responseMessage = "ERR|Debes iniciar sesión para buscar clases.";
                        break;
                    }

                    string filtroDesc = Encoding.UTF8.GetString(frame.Data);
                    var clases = classRepo.GetAll()
                        .Where(c => c.Description.Contains(filtroDesc, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (clases.Count == 0)
                    {
                        responseMessage = $"OK|No hay clases con descripción que contenga '{filtroDesc}'.";
                    }
                    else
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.Append("OK|");
                        foreach (var c in clases)
                        {
                            var occupiedSlots = inscriptionRepo.GetActiveClassByClassId(c.Id).Count;
                            sb.Append($"{c.Id}|{c.Name}|{c.Description}|{c.Link}|{c.StartDate:dd/MM/yyyy HH:mm}|{occupiedSlots}|{c.MaxCapacity}|{c.Image != null}\n");
                        }
                        responseMessage = sb.ToString().TrimEnd('\n');
                    }
                    break;
                }

                case ProtocolConstants.SearchClassesByAvailabilty:
                {
                    if (loggedInUser == null)
                    {
                        responseMessage = "ERR|Debes iniciar sesión para buscar clases.";
                        break;
                    }

                    if (int.TryParse(Encoding.UTF8.GetString(frame.Data), out int minCupos))
                    {
                        var clases = classRepo.GetAll()
                            .Where(c => DateTimeOffset.UtcNow < c.StartDate && (c.MaxCapacity - inscriptionRepo.GetActiveClassByClassId(c.Id).Count) > minCupos)
                            .ToList();

                        if (clases.Count == 0)
                        {
                            responseMessage = "OK|No hay clases con cupos disponibles.";
                        }
                        else
                        {
                            var sb = new System.Text.StringBuilder();
                            sb.Append("OK|");
                            foreach (var c in clases)
                            {
                                var occupiedSlots = inscriptionRepo.GetActiveClassByClassId(c.Id).Count;
                                sb.Append($"{c.Id}|{c.Name}|{c.Description}|{c.Link}|{c.StartDate:dd/MM/yyyy HH:mm}|{occupiedSlots}|{c.MaxCapacity}|{c.Image != null}\n");
                            }
                            responseMessage = sb.ToString().TrimEnd('\n');
                        }
                    }
                    else
                    {
                        responseMessage = "ERR|Parámetro inválido (se esperaba un número).";
                    }
                    break;
                }
                
                case ProtocolConstants.CommandModifyClass:
                    if (loggedInUser == null)
                    {
                        responseMessage = "ERR|Debes iniciar sesión para modificar una clase.";
                        break;
                    }
                    try
                    {
                        string payload = Encoding.UTF8.GetString(frame.Data);
                        var parts = payload.Split('|');
                        if (parts.Length < 6) throw new Exception("Datos incompletos para modificar la clase.");

                        int classId = int.Parse(parts[0]);
                 
                        
                            var classToModify = classRepo.GetById(classId);
                            if (classToModify == null) throw new Exception("La clase no existe.");
    
                            if (classToModify.Creator.Id != loggedInUser.Id)
                                throw new Exception("No tienes permiso para modificar esta clase.");

                            int activeInscriptions = inscriptionRepo.GetActiveClassByClassId(classId).Count;

                            string newName = parts[1];
                            string newDesc = parts[2];
                            string newCapacity = parts[3];
                            string newDuration = parts[4];
                            string newDate = parts[5];

                            classToModify.Modificar(newName, newDesc, newCapacity, newDate, newDuration, activeInscriptions);

                            responseMessage = $"OK|Clase '{newName}' modificada con éxito.";
                        
                    }
                    catch (Exception ex)
                    {
                        responseMessage = $"ERR|{ex.Message}";
                    }
                    break;
                
                case ProtocolConstants.CommandDeleteClass:
                    if (loggedInUser == null)
                    {
                        responseMessage = "ERR|Debes iniciar sesión para eliminar una clase.";
                        break;
                    }
                    try
                    {
                        int classId = int.Parse(Encoding.UTF8.GetString(frame.Data));
        
                        await Program.ImageSemaphore.WaitAsync();
                        try
                        {
                            var classToDelete = classRepo.GetById(classId);
                            if (classToDelete == null) throw new Exception("La clase no existe.");

                            if (classToDelete.Creator.Id != loggedInUser.Id)
                                throw new Exception("No tienes permiso para eliminar esta clase.");
            
                            if (inscriptionRepo.GetActiveClassByClassId(classId).Any())
                                throw new Exception("No se puede eliminar una clase con usuarios inscriptos.");
                            
                            if (!string.IsNullOrEmpty(classToDelete.Image))
                            {
                                string imagesPath = Path.Combine(AppContext.BaseDirectory, "ServerImages");
                                string imagePath = Path.Combine(imagesPath, classToDelete.Image);

                                if (File.Exists(imagePath))
                                {
                                    try
                                    {
                                        File.Delete(imagePath);
                                        Console.WriteLine($"Imagen '{classToDelete.Image}' eliminada del servidor.");
                                    }
                                    catch (Exception ex) 
                                    {
                                        Console.WriteLine($"No se pudo eliminar la imagen '{classToDelete.Image}': {ex.Message}");
                                    }
                                }
                            }

                            classToDelete.Eliminar(); 
                            classRepo.Delete(classId);

                            responseMessage = $"OK|La clase '{classToDelete.Name}' ha sido eliminada.";
                        }
                        finally
                        {
                            Program.ImageSemaphore.Release();
                        }
                    }
                    catch (Exception ex)
                    {
                        responseMessage = $"ERR|{ex.Message}";
                    }
                    break;
                
                case ProtocolConstants.CommandUploadImage:
                    if (loggedInUser == null)
                    {
                        responseMessage = "ERR|Debes iniciar sesión para subir una imagen.";
                        break;
                    }

                    try
                    {
                        await Program.ImageSemaphore.WaitAsync();
                        try
                        {
                            byte[] classIdImageBytes = await networkDataHelper.Receive(ProtocolConstants.ClassIdSize);
                            int classIdImage = BitConverter.ToInt32(classIdImageBytes);
                            OnlineClass classToAddImage = classRepo.GetById(classIdImage);
                            if (classToAddImage == null) throw new Exception("La clase no existe.");

                            if (classToAddImage.Creator.Id != loggedInUser.Id)
                                throw new Exception("No tienes permiso para modificar esta clase.");

                            byte[] fileNameLengthBuffer =
                                await networkDataHelper.Receive(ProtocolConstants.FileNameLengthSize);
                            int fileNameLength = BitConverter.ToInt32(fileNameLengthBuffer);

                            byte[] fileNameBytes = await networkDataHelper.Receive(fileNameLength);
                            string fileName = Encoding.UTF8.GetString(fileNameBytes);

                            byte[] fileSizeBuffer = await networkDataHelper.Receive(ProtocolConstants.FileLengthSize);
                            long fileSize = BitConverter.ToInt64(fileSizeBuffer);

                            string imagesPath = Path.Combine(AppContext.BaseDirectory, "ServerImages");
                            Directory.CreateDirectory(imagesPath);
                            string filePath = Path.Combine(imagesPath, fileName);

                            var otherClassWithSameImage = classRepo
                                .GetAll()
                                .FirstOrDefault(c => c.Id != classToAddImage.Id && c.Image == fileName);

                            if (otherClassWithSameImage != null)
                            {
                                responseMessage =
                                    $"ERR|El nombre de imagen '{fileName}' ya está siendo usado por la clase {otherClassWithSameImage.Id}.";
                                return (
                                    new Frame
                                    {
                                        Header = ProtocolConstants.Response,
                                        Command = ProtocolConstants.CommandUploadImage,
                                        Data = Encoding.UTF8.GetBytes(responseMessage)
                                    },
                                    loggedInUser
                                );

                            }

                            responseMessage = "OK|Listo para recibir imagen";
                            await networkDataHelper.Send(new Frame
                            {
                                Header = ProtocolConstants.Response,
                                Command = ProtocolConstants.CommandUploadImage,
                                Data = Encoding.UTF8.GetBytes(responseMessage)
                            });

                            string oldImageName = classToAddImage.Image;

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
                                    Console.WriteLine(
                                        $"Receiving segment #{currentPart} of size {ProtocolConstants.MaxFilePartSize}");
                                    buffer = await networkDataHelper.Receive(ProtocolConstants.MaxFilePartSize);
                                    offset += ProtocolConstants.MaxFilePartSize;
                                }
                                else
                                {
                                    long lastPartSize = fileSize - offset;
                                    Console.WriteLine($"Receiving segment #{currentPart} of size {lastPartSize}");
                                    buffer = await networkDataHelper.Receive((int)lastPartSize);
                                    offset += lastPartSize;
                                }

                                await fsh.Write(filePath, buffer);
                                currentPart++;
                            }

                            if (!string.IsNullOrEmpty(oldImageName) && oldImageName != fileName)
                            {
                                string oldPath = Path.Combine(imagesPath, oldImageName);
                                if (File.Exists(oldPath))
                                {
                                    File.Delete(oldPath);
                                    Console.WriteLine($"Imagen anterior '{oldImageName}' eliminada.");
                                }
                            }

                            Console.WriteLine($"Imagen recibida y guardada en: {filePath}");
                            classToAddImage.Image = fileName;

                            responseMessage =
                                $"OK|Imagen '{fileName}' recibida y asociada a la clase {classToAddImage.Id}";
                        }
                        finally
                        {
                            Program.ImageSemaphore.Release();
                        }
                    }
                    catch (Exception ex)
                    {
                        responseMessage = $"ERR|{ex.Message}";
                    }
                    break;

                case ProtocolConstants.CommandDownloadImage:
                    if (loggedInUser == null)
                    {
                        responseMessage = "ERR|Debes iniciar sesión para descargar imágenes.";
                        break;
                    }

                    try
                    {
                        int classIdDownload = int.Parse(Encoding.UTF8.GetString(frame.Data));
                        OnlineClass classToDownload = classRepo.GetById(classIdDownload);
                        if (classToDownload == null) throw new Exception("La clase no existe.");
                    if (string.IsNullOrEmpty(classToDownload.Image))
                    {
                        responseMessage = "ERR|Esta clase no tiene portada.";
                        break;
                    }

                    string imagesPath = Path.Combine(AppContext.BaseDirectory, "ServerImages");
                    string filePath = Path.Combine(imagesPath, classToDownload.Image);
                    if (!File.Exists(filePath))
                    {
                        responseMessage = "ERR|El archivo de portada no se encontró en el servidor.";
                        break;
                    }

                    FileInfo fi = new FileInfo(filePath);
                    string fileName = fi.Name;
                    long fileSize = fi.Length;

               
                    string metaPayload = $"{fileName}|{fileSize}";
                    responseMessage = "OK|" + metaPayload;

                   
                    responseData = Encoding.UTF8.GetBytes(responseMessage);
                    var metaFrame = new Frame
                    {
                        Header = ProtocolConstants.Response,
                        Command = ProtocolConstants.CommandDownloadImage,
                        Data = responseData
                    };
                    
                    await networkDataHelper.Send(metaFrame);
                    
                    FileStreamHelper fsh = new FileStreamHelper();
                    long offset = 0;
                    long partCount = ProtocolConstants.CalculateFileParts(fileSize);
                    long currentPart = 1;

                    while (offset < fileSize)
                    {
                        byte[] buffer;
                        bool isLastPart = (currentPart == partCount);

                        if (!isLastPart)
                        {
                            buffer = await fsh.Read(filePath, offset, ProtocolConstants.MaxFilePartSize);
                            offset += ProtocolConstants.MaxFilePartSize;
                        }
                        else
                        {
                            long lastPartSize = fileSize - offset;
                            buffer = await fsh.Read(filePath, offset, (int)lastPartSize);
                            offset += lastPartSize;
                        }

                        await networkDataHelper.Send(buffer);
                        currentPart++;
                    }
                    

                    Console.WriteLine($"Imagen '{fileName}' enviada al cliente.");
                    }
                    catch (Exception ex)
                    {
                        responseMessage = $"ERR|{ex.Message}";
                    }
                    return (null, loggedInUser);

                case ProtocolConstants.CommandLogout:
                    if (loggedInUser == null)
                    {
                        responseMessage = "ERR|No hay ninguna sesión activa para cerrar.";
                    }
                    else
                    {
                        responseMessage = $"OK|Sesión de '{loggedInUser.Username}' cerrada correctamente.";
                        loggedInUser = null;
                    }
                    break;
                
                default:
                    responseMessage = $"ERR|Comando desconocido o no implementado: {frame.Command}";
                    break;
            }
            
            responseData = Encoding.UTF8.GetBytes(responseMessage);
            
            return (new Frame
            {
                Header = ProtocolConstants.Response,
                Command = frame.Command,
                Data = responseData
            }, loggedInUser);
        }
        
        private static async Task ListenForServerCommands()
        {
            Console.WriteLine("Escriba 'report' para generar el reporte diario, 'cancel' para detenerlo, o 'exit' para cerrar el servidor.");
            while (isRunning)
            {
                string command = await Task.Run(() => Console.ReadLine()?.Trim().ToLower());

                if (!isRunning) break; // Salir si el servidor se está deteniendo

                switch (command)
                {
                    case "report":
                        if (serverReportCts != null)
                        {
                            Console.WriteLine("Ya hay un reporte generándose.");
                        }
                        else
                        {
                            serverReportCts = new CancellationTokenSource();
                            Console.WriteLine("Iniciando generación de reporte...");
                            _ = GenerateAndPrintReportAsync(serverReportCts.Token);
                        }
                        break;

                    case "cancel":
                        if (serverReportCts != null)
                        {
                            Console.WriteLine("Enviando señal de cancelación...");
                            serverReportCts.Cancel();
                        }
                        else
                        {
                            Console.WriteLine("No hay ningún reporte en ejecución para cancelar.");
                        }
                        break;

                    case "exit":
                        Console.WriteLine("Iniciando cierre del servidor...");
                        isRunning = false;
                        serverReportCts?.Cancel();
                        try { serverSocket?.Close(); } catch { }
                        break;

                    default:
                        if (!string.IsNullOrEmpty(command))
                            Console.WriteLine($"Comando desconocido: '{command}'. Comandos válidos: report, cancel, exit.");
                        break;
                }
            }
        }
        private static async Task GenerateAndPrintReportAsync(CancellationToken token)        {
            try
            {
                string responseMessage = null;
                
                List<OnlineClass> todayClasses = classRepo.GetAll().Where(c => c.StartDate.Date == DateTimeOffset.Now.Date).ToList();
            
                if (todayClasses.Count == 0)
                {
                    Console.WriteLine("\n--- Reporte del Día ---");
                    Console.WriteLine("No hay clases programadas para hoy.");
                    Console.WriteLine("-----------------------\n");
                    return;
                }
                
                token.ThrowIfCancellationRequested();

                if (todayClasses.Count == 0)
                {
                    Console.WriteLine("\n--- Reporte del Día ---");
                    Console.WriteLine("No hay clases programadas para hoy.");
                    Console.WriteLine("-----------------------\n");
                    return; // Termina
                }

                int totalClasses = todayClasses.Count;
                double avgDuration = todayClasses.Average(c => c.Duration);
        
                int totalInscriptions = 0;
                foreach (var c in todayClasses)
                {
                    totalInscriptions += inscriptionRepo.GetActiveClassByClassId(c.Id).Count;
                }
                double avgInscriptions = (totalClasses > 0) ? (double)totalInscriptions / totalClasses : 0;
                
                token.ThrowIfCancellationRequested();

                List<OnlineClass> classesWithImages = todayClasses.Where(c => !string.IsNullOrEmpty(c.Image)).ToList();            int totalImages = classesWithImages.Count;
                long totalSize = 0;
                double avgSize = 0;
                
                if (totalImages > 0)
                {
                    var sizeTasks = new List<Task<long>>();
                    string imagesPath = Path.Combine(AppContext.BaseDirectory, "ServerImages");

                    foreach (var c in classesWithImages)
                    {
                        sizeTasks.Add(GetFileSizeAsync(Path.Combine(imagesPath, c.Image), token));                
                    }

                    token.ThrowIfCancellationRequested();

                    long[] sizes = await Task.WhenAll(sizeTasks);

                    totalSize = sizes.Sum();
                    avgSize = (double)totalSize / totalImages;
                }
                
                token.ThrowIfCancellationRequested();
                
                Console.WriteLine("OK|--- Reporte de Clases del Día ---");
                Console.WriteLine($"Total de Clases: {totalClasses}");
                Console.WriteLine($"Duración Promedio: {avgDuration:F2} min");
                Console.WriteLine($"Total de Inscriptos: {totalInscriptions}");
                Console.WriteLine($"Promedio de Inscriptos: {avgInscriptions:F2}");
                Console.WriteLine($"Clases con Portada: {totalImages}");
                Console.WriteLine($"Tamaño Total de Portadas: {totalSize} bytes");
                Console.WriteLine($"Tamaño Promedio de Portadas: {avgSize:F2} bytes");
                Console.WriteLine("-------------------------------\n");
                Console.WriteLine("Reporte generado con éxito.");
                
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("\n*** La generación del reporte fue cancelada. ***\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n*** Error al generar el reporte: {ex.Message} ***\n");
            }
            finally
            {
                serverReportCts?.Dispose();
                serverReportCts = null;
                Console.Write("Ingrese comando (report, cancel, exit): ");
            }
        }
        
        private static async Task<long> GetFileSizeAsync(string filePath, CancellationToken cancellationToken)
        {
            await Task.Delay(3000, cancellationToken); // TODO just for testing purpose
            
            cancellationToken.ThrowIfCancellationRequested();
            
            if (!File.Exists(filePath))
            {
                return 0;
            }
            return new FileInfo(filePath).Length;
        }
        
        private static async Task ProcessAndSendReport(Frame frame, User loggedInUser, NetworkDataHelper networkDataHelper, Socket clientSocket)
        {
            var cts = new CancellationTokenSource();
            
            if (!activeReportTasks.TryAdd(clientSocket, cts))
            {
                try
                {
                    byte[] errorData = Encoding.UTF8.GetBytes("ERR|Ya tienes un reporte en progreso.");
                    await networkDataHelper.Send(new Frame { Header = ProtocolConstants.Response, Command = frame.Command, Data = errorData });
                }
                catch (Exception ex) { Console.WriteLine($"Error al notificar al cliente (reporte duplicado): {ex.Message}"); }
                return;
            }

            try
            {
                (var responseFrame, _) = await ProcessCommand(
                    frame, loggedInUser, networkDataHelper, clientSocket, cts.Token);
                
                if (responseFrame != null)
                    await networkDataHelper.Send(responseFrame);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error en Tarea Reporte] {ex.Message}");
                try
                {
                    byte[] errorData = Encoding.UTF8.GetBytes($"ERR|Error interno al generar reporte: {ex.Message}");
                    await networkDataHelper.Send(new Frame { Header = ProtocolConstants.Response, Command = frame.Command, Data = errorData });
                }
                catch { }
            }
            finally
            {
                activeReportTasks.TryRemove(clientSocket, out _);
                cts.Dispose();
            }
        }
        
        private static void SeedData()
        {
            try
            {
                Console.WriteLine("Seeding initial data...");

                // --- 1. Creación de Usuarios ---
                var pau = new User("pau", "pau");
                var teo = new User("teo", "teo");
                var romi = new User("romi", "romi");
                userRepo.Add(pau);
                userRepo.Add(teo);
                userRepo.Add(romi);
                Console.WriteLine("Users created: pau, teo, romi");

                // --- 2. Creación de Clases (Generales) ---
                var classPast = new OnlineClass("Clase 1 (Pasada)", "Intro a contenedores", 10, DateTimeOffset.Now.AddMonths(-1), 90, pau);
                var classSoon = new OnlineClass("Clase 2 (Próxima)", "Charla sobre IA", 5, DateTimeOffset.Now.AddDays(2), 120, pau);
                var classFuture = new OnlineClass("Clase 3 (Futura)", "Fundamentos de computacion", 20, DateTimeOffset.Now.AddYears(1), 180, pau);
                
                classRepo.Add(classPast);
                classRepo.Add(classSoon);
                classRepo.Add(classFuture);
                Console.WriteLine("General classes created (past, soon, future).");

                // --- 3. Creación de Clases para el Reporte de HOY (Req. 4) ---
                
                // Obtenemos fechas de hoy (ej. 10:00 AM y 2:00 PM)
                DateTimeOffset todayMorning = new DateTimeOffset(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 10, 0, 0, DateTimeOffset.Now.Offset);
                DateTimeOffset todayAfternoon = todayMorning.AddHours(4); // 2:00 PM

                // Clase de hoy #1 (con inscripciones, sin imagen)
                var classToday1 = new OnlineClass("Taller de Docker (Hoy)", "Clase de hoy, sin portada", 10, todayMorning, 60, pau);
                classRepo.Add(classToday1);

                // Clase de hoy #2 (con inscripciones, con imagen)
                var classToday2 = new OnlineClass("Taller de Redes (Hoy)", "Clase de hoy, con portada", 15, todayAfternoon, 90, pau);
                classToday2.Image = "reporte_test_img.jpg"; // Asignamos un nombre de imagen
                classRepo.Add(classToday2);

                // Clase de hoy #3 (sin inscripciones, con imagen)
                var classToday3 = new OnlineClass("Charla de C# (Hoy)", "Otra clase de hoy", 20, todayAfternoon.AddHours(2), 45, pau);
                classToday3.Image = "reporte_test_img_2.jpg";
                classRepo.Add(classToday3);
                
                Console.WriteLine("Classes for today's report created.");

                // --- 4. Creación de Inscripciones (para clases de hoy y pasadas) ---
                inscriptionRepo.Add(new Inscription(teo, classToday1));
                inscriptionRepo.Add(new Inscription(romi, classToday1)); // 2 inscriptos
                
                inscriptionRepo.Add(new Inscription(teo, classToday2)); // 1 inscripto
                
                inscriptionRepo.Add(new Inscription(romi, classPast)); // 1 inscripto en una clase pasada
                Console.WriteLine("Inscriptions for classes created.");

                // --- 5. Creación de Archivos de Imagen Falsos (para el cálculo de tamaño) ---
                try
                {
                    string imagesPath = Path.Combine(AppContext.BaseDirectory, "ServerImages");
                    Directory.CreateDirectory(imagesPath); // Asegura que la carpeta exista

                    string filePath1 = Path.Combine(imagesPath, "reporte_test_img.jpg");
                    File.WriteAllText(filePath1, "Este es un archivo de prueba con un tamaño."); // Crea un archivo con contenido

                    string filePath2 = Path.Combine(imagesPath, "reporte_test_img_2.jpg");
                    File.WriteAllText(filePath2, "Este es un segundo archivo de prueba, un poco más grande que el primero.");
                    
                    Console.WriteLine("Dummy image files for report created in ServerImages.");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error creating dummy image files: {e.Message}");
                }

                Console.WriteLine("Data seeding finished successfully.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error during data seeding: {e.Message}");
            }
        }
    }
}