using AuthGrpc.Services;
using Domain;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Repository;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(o =>
{
    o.ListenAnyIP(5191, lo => lo.Protocols = HttpProtocols.Http2);
});

builder.Services.AddGrpc();

builder.Services.AddSingleton<UserRepository>();
builder.Services.AddSingleton<OnlineClassRepository>();

var app = builder.Build();

Seed(app.Services);

app.MapGrpcService<AuthService>();
app.MapGet("/", () => "Auth gRPC (HTTP/2) en :5191");

app.Run();

static void Seed(IServiceProvider sp)
{
    var users = sp.GetRequiredService<UserRepository>();
    var classes = sp.GetRequiredService<OnlineClassRepository>();
	var u = new User("pau", "pau");
    users.Add(u);

    var c = new OnlineClass("Clase demo", "gRPC y WebSockets", 10, DateTimeOffset.Now.AddHours(2), 60, u);
    classes.Add(c);
	Console.WriteLine($"[AuthGrpc] Demo class link: {c.Link}");
}
