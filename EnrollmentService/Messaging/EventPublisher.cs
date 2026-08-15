using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace EnrollmentService.Messaging
{
    public class EventPublisher
    {
        private readonly IConfiguration _config;

        public EventPublisher(IConfiguration config)
        {
            _config = config;
        }

        public async Task PublishEnrollmentCreated(object enrollmentData)
        {
            var factory = new ConnectionFactory
            {
                HostName = _config["RabbitMQ:HostName"],
                UserName = _config["RabbitMQ:UserName"],
                Password = _config["RabbitMQ:Password"]
            };

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: "enrollment-created",
                durable: true,
                exclusive: false,
                autoDelete: false);

            var json = JsonSerializer.Serialize(enrollmentData);
            var body = Encoding.UTF8.GetBytes(json);

            var props = new BasicProperties
            {
                Persistent = true
            };

            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: "enrollment-created",
                mandatory: false,
                basicProperties: props,
                body: body);
        }
    }
}