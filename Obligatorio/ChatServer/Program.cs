using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using AuthGrpc;                 // generado desde tu auth.proto
using Grpc.Net.Client;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// WebSockets sobre HTTP/1.1 en :8080 (igual al estilo de los profes)
builder.WebHost.ConfigureKestrel(o =>
{
    o.ListenAnyIP(8080, lo => lo.Protocols = HttpProtocols.Http1);
});

// gRPC Auth client (apunta al AuthGrpc)
var authUrl = Environment.GetEnvironmentVariable("AUTH_GRPC_URL") ?? "http://localhost:5191";
builder.Services.AddSingleton(_ => GrpcChannel.ForAddress(authUrl));
builder.Services.AddSingleton(sp => new Auth.AuthClient(sp.GetRequiredService<GrpcChannel>()));

var app = builder.Build();
app.UseWebSockets();

var rooms = new ConcurrentDictionary<string, ConcurrentDictionary<WebSocket, byte>>();
//El diccionario es para hacer una sala por calse
app.Map("/ws", async ctx =>
{
    if (!ctx.WebSockets.IsWebSocketRequest)
    {
        ctx.Response.StatusCode = 400;
        return;
    }

    var u    = ctx.Request.Query["u"].ToString();
    var p    = ctx.Request.Query["p"].ToString();
    var link = ctx.Request.Query["link"].ToString();

    var auth = ctx.RequestServices.GetRequiredService<Auth.AuthClient>();

	var userOk = (await auth.ValidateUserAsync(new ValidateUserRequest { Username = u, Password = p })).Ok;
	if (!userOk) { ctx.Response.StatusCode = 401; await ctx.Response.WriteAsync("auth"); return; }

	var linkOk = (await auth.ValidateClassLinkAsync(new ValidateClassLinkRequest { Link = link })).Ok;
	if (!linkOk) { ctx.Response.StatusCode = 403; await ctx.Response.WriteAsync("link"); return; }

	var enrOk = (await auth.ValidateEnrollmentAsync(new ValidateEnrollmentRequest { Username = u, Link = link })).Ok;
	if (!enrOk) { ctx.Response.StatusCode = 403; await ctx.Response.WriteAsync("enrollment"); return; }

    var ws = await ctx.WebSockets.AcceptWebSocketAsync();

    var room = rooms.GetOrAdd(link, _ => new());
    room.TryAdd(ws, 0);

    await Broadcast(room, $"[system] {u} se unió");

    var buffer = new byte[4096];
    try
    {
        while (ws.State == WebSocketState.Open)
        {
            var result = await ws.ReceiveAsync(buffer, ctx.RequestAborted);
            if (result.CloseStatus.HasValue) break;

            var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
            await Broadcast(room, $"{u}: {text}");
        }
    }
    finally
    {
        room.TryRemove(ws, out _);
        await Broadcast(room, $"[system] {u} salió");
        try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", ctx.RequestAborted); } catch { }
    }
});

app.MapGet("/", () => "ChatServer WS en :8080 (usar /ws?u=...&p=...&link=...)");
app.Run();

static async Task Broadcast(ConcurrentDictionary<WebSocket, byte> room, string msg)
{
    var bytes = Encoding.UTF8.GetBytes(msg);
    var seg = new ArraySegment<byte>(bytes);
    foreach (var kv in room.Keys.ToArray())
    {
        if (kv.State != WebSocketState.Open) continue;
        try { await kv.SendAsync(seg, WebSocketMessageType.Text, true, default); }
        catch {}
    }
}
