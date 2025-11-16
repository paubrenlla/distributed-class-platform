using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events; 
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System;
using Common.DTOs;
using Newtonsoft.Json;

namespace LogsConsumer
{
    public class RabbitMQConsumerService : BackgroundService
    {
        private IConnection? _connection;
        private IChannel? _channel; 
        private readonly string _queueName = "task_queue"; 
        private readonly LogStorageService _logStorage;
        private readonly ILogger<RabbitMQConsumerService> _logger;
        private readonly string _rabbitMqHost;
        private readonly string _rabbitMqUser;
        private readonly string _rabbitMqPass;

        public RabbitMQConsumerService(LogStorageService logStorage, ILogger<RabbitMQConsumerService> logger)
        {
            _logStorage = logStorage;
            _logger = logger;
            
            _rabbitMqHost = Environment.GetEnvironmentVariable("RABBIT_HOST") ?? "localhost";
            _rabbitMqUser = Environment.GetEnvironmentVariable("RABBIT_USER") ?? "guest";
            _rabbitMqPass = Environment.GetEnvironmentVariable("RABBIT_PASS") ?? "guest";
        }
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = _rabbitMqHost,
                    UserName = _rabbitMqUser,
                    Password = _rabbitMqPass,
                };

                _connection = await factory.CreateConnectionAsync(cancellationToken);

                _channel = await _connection.CreateChannelAsync();

                await _channel.QueueDeclareAsync(
                    queue: _queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                _logger.LogInformation("RabbitMQ Consumer conectado y cola declarada.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al conectar con RabbitMQ. El servicio no se iniciará.");
                throw;
            }

            await base.StartAsync(cancellationToken);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_channel == null)
            {
                _logger.LogError("El canal de RabbitMQ es nulo. El servicio no puede ejecutarse.");
                return;
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var jsonMessage = Encoding.UTF8.GetString(body);
                    var logMessage = JsonConvert.DeserializeObject<LogMessageDTO>(jsonMessage);

                    if (logMessage != null)
                    {
                        _logStorage.AddLog(logMessage); 
                        _logger.LogInformation($"[Log Recibido] {logMessage.Username}: {logMessage.Action}");
                    }

                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    await Task.Yield(); // Cede el hilo
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al procesar mensaje de RabbitMQ.");
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
            };

            await _channel.BasicConsumeAsync(queue: _queueName, autoAck: false, consumer: consumer, consumerTag: "log-consumer", cancellationToken: stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Cerrando conexión de RabbitMQ...");
            if (_channel != null) await _channel.CloseAsync();
            if (_connection != null) await _connection.CloseAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}