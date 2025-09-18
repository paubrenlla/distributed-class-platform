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
        static readonly OnlineClassRepository classRepo = new OnlineClassRepository();
        static readonly InscriptionRepository inscriptionRepo = new InscriptionRepository();
        
        static Program()
        {
            SeedData();
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Server starting with preloaded data...");
            Console.WriteLine("Starting Server Application..");

            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // TODO: Leer IP y Puerto desde un archivo de configuración
            IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000); 

            serverSocket.Bind(serverEndpoint);
            serverSocket.Listen(10);

            Console.WriteLine("Waiting for clients to connect...");

            // TODO: Implementar un mecanismo de cierre controlado
            while (true) 
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
    
            // Esta variable mantendrá el estado para esta conexión de cliente específica.
            User loggedInUser = null; 

            while (clientActive)
            {
                try
                {
                    Frame receivedFrame = networkDataHelper.Receive();
                    Console.WriteLine($"Client sent command: {receivedFrame.Command}");

                    // Pasamos 'loggedInUser' por referencia para que ProcessCommand pueda modificarlo.
                    Frame responseFrame = ProcessCommand(receivedFrame, ref loggedInUser); 
            
                    networkDataHelper.Send(responseFrame);
                }
                catch (SocketException)
                {
                    clientActive = false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error processing client request: " + ex.Message);
                    clientActive = false; 
                }
            }

            Console.WriteLine("Client disconnected: " + clientSocket.RemoteEndPoint);
            try
            {
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
            }
            catch {}
        }

        static Frame ProcessCommand(Frame frame, ref User loggedInUser) 
        {
            byte[] responseData;
            string responseMessage;
                    
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
                            stringBuilder.Append($"{onlineClass.Id}|{onlineClass.Name}|{onlineClass.StartDate:dd/MM/yyyy HH:mm}|{occupiedSlots}|{onlineClass.MaxCapacity}|{onlineClass.Image != null}\n");
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

                        responseMessage = $"OK|Clase '{name}' creada con éxito con el ID: {newClass.Id}";
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

                        if(inscriptionRepo.GetActiveByUserAndClass(loggedInUser.Id, classId) != null)
                            throw new Exception("Ya estás inscrito en esta clase.");

                        // Verificamos cupos
                        var activeInscriptions = inscriptionRepo.GetActiveClassByClassId(classId);
                        if(activeInscriptions.Count >= classToJoin.MaxCapacity)
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
                        .Where(c => DateTimeOffset.UtcNow < c.StartDate && (c.MaxCapacity - c.Inscribers) > 0)
                        .OrderByDescending(c => c.MaxCapacity - c.Inscribers)
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
                            sb.Append($"{c.Id}|{c.Name}|{c.StartDate:dd/MM/yyyy HH:mm}|{occupiedSlots}|{c.MaxCapacity}|{c.Image != null}\n");                        }
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
                    var clases = classRepo
                        .GetAll()
                        .Where(c => c.Name.Contains(filtroNombre, StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(c => c.MaxCapacity - c.Inscribers)
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
                            sb.Append($"{c.Id}|{c.Name}|{c.StartDate:dd/MM/yyyy HH:mm}|{occupiedSlots}|{c.MaxCapacity}|{c.Image != null}\n");
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
                    var clases = classRepo
                        .GetAll()
                        .Where(c => c.Description.Contains(filtroDesc, StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(c => c.MaxCapacity - c.Inscribers)
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
                            sb.Append($"{c.Id}|{c.Name}|{c.StartDate:dd/MM/yyyy HH:mm}|{occupiedSlots}|{c.MaxCapacity}|{c.Image != null}\n");
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
                        var clases = classRepo
                            .GetAll()
                            .Where(c => DateTimeOffset.UtcNow < c.StartDate && (c.MaxCapacity - c.Inscribers) > minCupos)
                            .OrderByDescending(c => c.MaxCapacity - c.Inscribers)
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
                                sb.Append($"{c.Id}|{c.Name}|{c.StartDate:dd/MM/yyyy HH:mm}|{occupiedSlots}|{c.MaxCapacity}|{c.Image != null}\n");
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
                        // Formato esperado: IDClase|NuevoNombre|NuevaDesc|NuevoCupo|NuevaDuracion|NuevaFecha
                        string payload = Encoding.UTF8.GetString(frame.Data);
                        var parts = payload.Split('|');
                        if (parts.Length < 6) throw new Exception("Datos incompletos para modificar la clase.");

                        int classId = int.Parse(parts[0]);
                        var classToModify = classRepo.GetById(classId);
                        if (classToModify == null) throw new Exception("La clase no existe.");
                        
                        // ¡AUTORIZACIÓN! Verificamos que el usuario logueado sea el creador.
                        if (classToModify.Creator.Id != loggedInUser.Id)
                            throw new Exception("No tienes permiso para modificar esta clase.");

                        // Parseamos los nuevos datos
                        string newName = parts[1];
                        string newDesc = parts[2];
                        int newCapacity = int.Parse(parts[3]);
                        int newDuration = int.Parse(parts[4]);
                        DateTimeOffset newDate = DateTimeOffset.Parse(parts[5]);
                        
                        // Usamos el método del dominio para modificar la clase
                        classToModify.Modificar(newName, newDesc, newCapacity, newDate, newDuration, null); // Pasamos null para la imagen por ahora
                        
                        responseMessage = $"OK|Clase '{classToModify.Name}' modificada con éxito.";
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
                        var classToDelete = classRepo.GetById(classId);
                        if (classToDelete == null) throw new Exception("La clase no existe.");

                        // ¡AUTORIZACIÓN!
                        if (classToDelete.Creator.Id != loggedInUser.Id)
                            throw new Exception("No tienes permiso para eliminar esta clase.");
                        
                        // Verificamos si hay inscriptos 
                        if (inscriptionRepo.GetActiveClassByClassId(classId).Any())
                            throw new Exception("No se puede eliminar una clase con usuarios inscriptos.");

                        // La clase de dominio ya verifica si ha comenzado o no
                        classToDelete.Eliminar(); 
                        
                        // Si todas las validaciones pasan, la eliminamos del repositorio
                        classRepo.Delete(classId);

                        responseMessage = $"OK|La clase '{classToDelete.Name}' ha sido eliminada.";
                    }
                    catch (Exception ex)
                    {
                        responseMessage = $"ERR|{ex.Message}";
                    }
                    break;
                default:
                    responseMessage = $"ERR|Comando desconocido o no implementado: {frame.Command}";
                    break;
            }
            
            responseData = Encoding.UTF8.GetBytes(responseMessage);
            return new Frame
            {
                Header = ProtocolConstants.Response,
                Command = frame.Command,
                Data = responseData
            };
        }
        private static void SeedData()
        {
            try
            {
                Console.WriteLine("Seeding initial data...");

                // Creación de Usuarios
                var pau = new User("pau", "pau");
                var teo = new User("teo", "teo");
                var romi = new User("romi", "romi");
                userRepo.Add(pau);
                userRepo.Add(teo);
                userRepo.Add(romi);
                Console.WriteLine("Users created: pau, teo, romi");

                // Creación de Clases
                // Creador para todas las clases será "pau"
                var classPast = new OnlineClass("Clase 1", "Intro a contenedores", 10, DateTimeOffset.Now.AddMonths(-1), 90, pau);
                var classSoon = new OnlineClass("Clase 2", "Charla sobre IA", 5, DateTimeOffset.Now.AddDays(2), 120, pau);
                var classFuture = new OnlineClass("Clase 3", "Fundamentos de computacion", 20, DateTimeOffset.Now.AddYears(1), 180, pau);
                classRepo.Add(classPast);
                classRepo.Add(classSoon);
                classRepo.Add(classFuture);
                Console.WriteLine("Classes created.");
                
                Console.WriteLine("No inscriptions where created");

                Console.WriteLine("Data seeding finished successfully.");
            }
            catch (Exception e)
            {
                // Este catch es por si intentas agregar un usuario que ya existe, para que el servidor no se caiga.
                Console.WriteLine($"Error during data seeding: {e.Message}");
            }
        }
    }
}