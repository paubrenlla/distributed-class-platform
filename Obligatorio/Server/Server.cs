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
            // TODO: Leer IP y Puerto desde un archivo de configuración
            IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000); 

            serverSocket.Bind(serverEndpoint);
            serverSocket.Listen(10); // Escucha hasta 10 clientes en cola

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
                // Añadimos el nuevo comando para crear usuarios
                case ProtocolConstants.CommandCreateUser:
                    try
                    {
                        string payload = Encoding.UTF8.GetString(frame.Data); // "usuario|clave"
                        var parts = payload.Split('|');
                        if (parts.Length < 2) throw new Exception("Formato incorrecto. Se esperaba 'usuario|clave'.");

                        var user = new User(parts[0], parts[1]);
                        userRepo.Add(user); // Aquí no usamos lock, como acordamos.
                        
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
                        string credentials = Encoding.UTF8.GetString(frame.Data); // "usuario|clave"
                        var parts = credentials.Split('|');
                        if (parts.Length < 2) throw new Exception("Formato incorrecto. Se esperaba 'usuario|clave'.");

                        var user = userRepo.GetByUsername(parts[0]);
                        if (user != null && user.VerificarPassword(parts[1]))
                        {
                            loggedInUser = user; // ¡Éxito! Guardamos el usuario en el estado de la conexión.
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
                    // Lógica para listar clases... por ahora un placeholder.
                    responseMessage = $"OK|Hola {loggedInUser.Username}, aquí está la lista de clases: [...]";
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
    }
}