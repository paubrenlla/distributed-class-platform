using System.Net.WebSockets;
using System.Text;
using AuthGrpc;
using Grpc.Net.Client;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // WS del ChatServer y gRPC del AuthGrpc
        string wsBase = Environment.GetEnvironmentVariable("CHAT_WS_URL") ?? "ws://localhost:8080/ws";
        string authUrl = Environment.GetEnvironmentVariable("AUTH_GRPC_URL") ?? "http://localhost:5191";

        Console.Write("Usuario: ");
        var username = Console.ReadLine()?.Trim() ?? "";
        Console.Write("Password: ");
        var password = Console.ReadLine()?.Trim() ?? "";
        Console.Write("Link de clase: ");
        var classLink = Console.ReadLine()?.Trim() ?? "";

        try
        {
            using var channel = GrpcChannel.ForAddress(authUrl);
            var client = new Auth.AuthClient(channel);

            var uresp = await client.ValidateUserAsync(new ValidateUserRequest
            {
                Username = username,
                Password = password
            });
            if (!uresp.Ok)
            {
                Console.WriteLine($"Login inválido: {uresp.Reason}");
                return 1;
            }

            var lresp = await client.ValidateClassLinkAsync(new ValidateClassLinkRequest { Link = classLink });
            if (!lresp.Ok)
            {
                Console.WriteLine($"Link inválido: {lresp.Reason}");
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error contactando AuthGrpc ({authUrl}): {ex.Message}");
            return 1;
        }

        var url = $"{wsBase}?u={Uri.EscapeDataString(username)}&p={Uri.EscapeDataString(password)}&link={Uri.EscapeDataString(classLink)}";

        using var ws = new ClientWebSocket();

        try
        {
            await ws.ConnectAsync(new Uri(url), CancellationToken.None);
            Console.WriteLine("Conectado al chat.");

            _ = Task.Run(async () =>
            {
                var buffer = new byte[4096];
                while (ws.State == WebSocketState.Open)
                {
                    var res = await ws.ReceiveAsync(buffer, CancellationToken.None);
                    if (res.MessageType == WebSocketMessageType.Close) break;
                    Console.WriteLine($"< {Encoding.UTF8.GetString(buffer, 0, res.Count)}");
                }
            });

            while (ws.State == WebSocketState.Open)
            {
                var line = Console.ReadLine();
                if (string.IsNullOrEmpty(line)) break;
                var bytes = Encoding.UTF8.GetBytes(line);
                await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
            }

            if (ws.State == WebSocketState.Open)
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);

            return 0;
        }
        catch (WebSocketException wex)
        {
            Console.WriteLine($"No se pudo abrir el WebSocket ({url}): {wex.Message}");
            Console.WriteLine("usuario/contraseña incorrectos y/o link de clase inválido.");
            return 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al abrir el WebSocket: {ex.Message}");
            return 1;
        }
    }
}
