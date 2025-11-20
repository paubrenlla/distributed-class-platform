using System.Net.WebSockets;
using System.Text;

Console.Write("Usuario: ");
var u = Console.ReadLine() ?? "";
Console.Write("Password: ");
var p = Console.ReadLine() ?? "";
Console.Write("Link de clase: ");
var link = Console.ReadLine() ?? "";

var url = Environment.GetEnvironmentVariable("CHAT_WS_URL") ?? "ws://localhost:8080/ws";
var uri = new Uri($"{url}?u={Uri.EscapeDataString(u)}&p={Uri.EscapeDataString(p)}&link={Uri.EscapeDataString(link)}");

using var ws = new ClientWebSocket();
await ws.ConnectAsync(uri, default);
Console.WriteLine("Conectado. Escribí y Enter para enviar. Vacío para salir.");

var cts = new CancellationTokenSource();
_ = Task.Run(async () =>
{
    var buffer = new byte[4096];
    while (!cts.IsCancellationRequested && ws.State == WebSocketState.Open)
    {
        var res = await ws.ReceiveAsync(buffer, cts.Token);
        if (res.CloseStatus.HasValue) break;
        Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, res.Count));
    }
});

while (true)
{
    var line = Console.ReadLine() ?? "";
    if (string.IsNullOrEmpty(line)) break;
    var bytes = Encoding.UTF8.GetBytes(line);
    await ws.SendAsync(bytes, WebSocketMessageType.Text, true, default);
}

cts.Cancel();
try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", default); } catch { }