using LogsConsumer; // Asegúrate de tener esta clase en tu proyecto
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// 1. Añade servicios de API (esto configura el Logging automáticamente)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. Añade tu servicio de almacenamiento como Singleton
// (Necesitarás crear la clase LogStorageService.cs que te di antes)
builder.Services.AddSingleton<LogStorageService>(LogStorageService.Instance);

// 3. Añade tu consumidor de RabbitMQ como un servicio en segundo plano
// (Necesitarás usar la clase RabbitMQConsumerService.cs 100% asincrónica que te di)
builder.Services.AddHostedService<RabbitMQConsumerService>();

var app = builder.Build();

// Configura el pipeline de la API
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // Esto te da una UI para probar tu API REST
}

app.UseAuthorization();
app.MapControllers(); // Esto activa tu LogsController

app.Run();