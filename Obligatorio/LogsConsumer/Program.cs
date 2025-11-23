using LogsConsumer; // Asegúrate de tener esta clase en tu proyecto
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<LogStorageService>(LogStorageService.Instance);

builder.Services.AddHostedService<RabbitMQConsumerService>();

var app = builder.Build();

// Configura el pipeline de la API

app.UseSwagger();
app.UseSwaggerUI(); // Esto da una UI para probar tu API REST

app.UseAuthorization();
app.MapControllers(); // Esto activa LogsController

app.Run();