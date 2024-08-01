namespace NewsAggregation.Services.ServiceJobs
{
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using NewsAggregation.Data;
    using NewsAggregation.DTO;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using System.Net.Mail;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    public class RabbitMQService : BackgroundService
    {
        private readonly ILogger<RabbitMQService> _logger;
        private readonly RabbitMQSettings _settings;
        private readonly SmtpSettings _smtpSettings;
        private IConnection _connection;
        private IModel _channel;

        public RabbitMQService(IOptions<RabbitMQSettings> settings, ILogger<RabbitMQService> logger, IOptions<SmtpSettings> smtpSettings)
        {
            _logger = logger;
            _settings = settings.Value;
            _smtpSettings = smtpSettings.Value;

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

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var emailMessage = JsonSerializer.Deserialize<EmailMessage>(message);

                _logger.LogInformation("Received message: {Message}", message);

                await SendEmail(emailMessage);

                _channel.BasicAck(ea.DeliveryTag, false);
            };

            _channel.BasicConsume(queue: _settings.QueueName, autoAck: false, consumer: consumer);
            return Task.CompletedTask;
        }

        private async Task SendEmail(EmailMessage emailMessage)
        {
            // Your email sending logic
            var client = new SmtpClient(_smtpSettings.SmtpServer)
            {
                Port = _smtpSettings.SmtpPort,
                Credentials = new System.Net.NetworkCredential(_smtpSettings.SmtpUsername, _smtpSettings.SmtpPassword),
                EnableSsl = false
            };

            var mailMessage = new MailMessage(emailMessage.From, emailMessage.To, emailMessage.Subject, emailMessage.Body);

            await client.SendMailAsync(mailMessage);
        }

        public override void Dispose()
        {
            _channel.Close();
            _connection.Close();
            base.Dispose();
        }
    }

}
