using Microsoft.Extensions.Options;
using NewsAggregation.Data;
using NewsAggregation.DTO;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace NewsAggregation.Services.ServiceJobs.Email
{
    public class EmailQueueService
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly RabbitMQSettings _settings;

        public EmailQueueService(IOptions<RabbitMQSettings> settings)
        {
            _settings = settings.Value;

            var factory = new ConnectionFactory()
            {
                HostName = _settings.HostName,
                UserName = _settings.UserName,
                Password = _settings.Password
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(queue: _settings.QueueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
        }

        public void QueueEmail(EmailMessage emailMessage)
        {
            var message = JsonSerializer.Serialize(emailMessage);
            var body = Encoding.UTF8.GetBytes(message);

            _channel.BasicPublish(exchange: "", routingKey: _settings.QueueName, basicProperties: null, body: body);
        }

        public void Dispose()
        {
            _channel.Close();
            _connection.Close();
        }
    }

}
