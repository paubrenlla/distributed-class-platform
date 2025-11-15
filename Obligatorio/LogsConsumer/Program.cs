using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

internal class Program
{
    static async Task Main()
    {
        var host = Environment.GetEnvironmentVariable("RABBIT_HOST") ?? "rabbitmq";
        var user = Environment.GetEnvironmentVariable("RABBIT_USER") ?? "guest";
        var pass = Environment.GetEnvironmentVariable("RABBIT_PASS") ?? "guest";

        var factory = new ConnectionFactory { HostName = host, UserName = user, Password = pass };
        using var conn = await factory.CreateConnectionAsync();
        using var ch = await conn.CreateChannelAsync();

        const string QueueName = "task_queue";

        await ch.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        await ch.BasicQosAsync(0, prefetchCount: 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(ch);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var msg = Encoding.UTF8.GetString(ea.Body.ToArray());
            Console.WriteLine($"[x] {msg}");
            await ch.BasicAckAsync(ea.DeliveryTag, multiple: false);
        };

        await ch.BasicConsumeAsync(queue: QueueName, autoAck: false, consumer: consumer);

        Console.WriteLine("[*] Esperando logs. Presione Enter para salir…");
        Console.ReadLine();
    }
}