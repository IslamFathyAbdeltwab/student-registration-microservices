using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace NotificationService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _config;

        public Worker(ILogger<Worker> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _config["RabbitMQ:HostName"],
                UserName = _config["RabbitMQ:UserName"],
                Password = _config["RabbitMQ:Password"]
            };

            IConnection? connection = null;
            var maxRetries = 10;
            var delay = TimeSpan.FromSeconds(5);

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    connection = await factory.CreateConnectionAsync(stoppingToken);
                    _logger.LogInformation("Connected to RabbitMQ successfully.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("RabbitMQ not ready yet (attempt {Attempt}/{Max}): {Message}", attempt, maxRetries, ex.Message);
                    await Task.Delay(delay, stoppingToken);
                }
            }

            if (connection == null)
            {
                _logger.LogError("Could not connect to RabbitMQ after {Max} attempts. Worker exiting.", maxRetries);
                return;
            }

            using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await channel.QueueDeclareAsync(
                queue: "enrollment-created",
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);

                _logger.LogInformation("Received enrollment event: {Json}", json);
                _logger.LogInformation("Simulated email sent to student for their new enrollment.");

                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            };

            await channel.BasicConsumeAsync(
                queue: "enrollment-created",
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}