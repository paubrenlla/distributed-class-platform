using System.Text;
using System.Threading;
using RabbitMQ.Client;

public static class LogPublisher
{
    private static ConnectionFactory _factory;
    private static IConnection _conn;
    private static IChannel _ch;
    private static readonly SemaphoreSlim _pubLock = new(1, 1);

    private const string QueueName = "task_queue";

    public static async Task InitAsync()
    {
        var host = Environment.GetEnvironmentVariable("RABBIT_HOST") ?? "rabbitmq";
        var user = Environment.GetEnvironmentVariable("RABBIT_USER") ?? "guest";
        var pass = Environment.GetEnvironmentVariable("RABBIT_PASS") ?? "guest";

        _factory = new ConnectionFactory { HostName = host, UserName = user, Password = pass };
        _conn = await _factory.CreateConnectionAsync();
        _ch = await _conn.CreateChannelAsync();

        await _ch.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );
    }

    public static async Task Publish(string message)
    {
        if (_ch is null) return;

        var body = Encoding.UTF8.GetBytes(message);
        var props = new BasicProperties
        {
            DeliveryMode = DeliveryModes.Persistent // en vez de 2
        };
        
        await _pubLock.WaitAsync();
        try
        {
            await _ch.BasicPublishAsync(
                exchange: "",
                routingKey: QueueName,
                mandatory: false,
                basicProperties: props,
                body: body
            );
        }
        finally
        {
            _pubLock.Release();
        }
    }

    public static async ValueTask DisposeAsync()
    {
        if (_ch != null) await _ch.CloseAsync();
        if (_conn != null) await _conn.CloseAsync();
    }
}