namespace NewsAggregation.Services.ServiceJobs
{
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using NewsAggregation.Data;
    using NewsAggregation.DTO;
    using Polly;
    using Polly.CircuitBreaker;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using System;
    using System.Net.Mail;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;

    public class RabbitMQService : BackgroundService
    {
        private readonly ILogger<RabbitMQService> _logger;
        private readonly RabbitMQSettings _settings;
        private readonly SmtpSettings _smtpSettings;
        private IConnection _connection;
        private IModel _channel;
        private AsyncCircuitBreakerPolicy _circuitBreakerPolicy;

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

            _circuitBreakerPolicy = Policy
                            .Handle<SmtpException>()
                            .Or<Exception>(ex => ex.Message.Contains("rate limit"))
                            .AdvancedCircuitBreakerAsync(
                                failureThreshold: 0.5,
                                samplingDuration: TimeSpan.FromSeconds(10),
                                minimumThroughput: 7,
                                durationOfBreak: TimeSpan.FromMinutes(1), 
                                onBreak: (ex, breakDelay) =>
                                {
                                    _logger.LogWarning("Circuit breaker triggered due to SMTP failure. Breaking for {BreakDelay}. Exception: {Exception}", breakDelay, ex);
                                },
                                onReset: () => _logger.LogInformation("Circuit breaker reset."),
                                onHalfOpen: () => _logger.LogInformation("Circuit breaker half-open. Testing if SMTP server is responsive.")
                            );
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

                try
                {
                    await _circuitBreakerPolicy.ExecuteAsync(async () =>
                    {
                        await RetryPolicy.ExecuteAsync(async () =>
                        {
                            await SendEmail(emailMessage);
                        });
                    });

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email: {Message}", message);
                    // Optionally, requeue the message or move to a dead-letter queue
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume(queue: _settings.QueueName, autoAck: false, consumer: consumer);
            return Task.CompletedTask;
        }

        private AsyncPolicy RetryPolicy =>
            Policy
                .Handle<SmtpException>()
                .Or<Exception>(ex => ex.Message.Contains("rate limit"))
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning("Retry {RetryCount} of {PolicyKey} at {OperationKey}, due to: {Exception}.", retryCount, context.PolicyKey, context.OperationKey, exception);
                    });

        private async Task SendEmail(EmailMessage emailMessage)
        {
            using var client = new SmtpClient(_smtpSettings.SmtpServer)
            {
                Port = _smtpSettings.SmtpPort,
                Credentials = new System.Net.NetworkCredential(_smtpSettings.SmtpUsername, _smtpSettings.SmtpPassword),
                EnableSsl = false
            };

            //var sanitizedBody = HttpUtility.HtmlEncode(emailMessage.Body);


            var mailMessage = new MailMessage
            {
                From = new MailAddress(emailMessage.From),
                Subject = emailMessage.Subject,
                Body = emailMessage.Body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(emailMessage.To);

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
