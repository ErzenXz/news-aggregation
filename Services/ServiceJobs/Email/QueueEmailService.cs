
namespace NewsAggregation.Services.ServiceJobs.Email
{
    public class QueueEmailService : BackgroundService
    {

        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly ILogger<QueueEmailService> _logger;

        public QueueEmailService(IBackgroundTaskQueue taskQueue, ILogger<QueueEmailService> logger)
        {
            _taskQueue = taskQueue;
            _logger = logger;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var workItem = await _taskQueue.DequeueAsync(stoppingToken);
                _logger.LogInformation("[x] Dequeued {WorkItem}", nameof(workItem));
                try
                {
                    await workItem(stoppingToken);
                }
                catch (Exception)
                {
                    _logger.LogError("Error occurred executing {WorkItem}", nameof(workItem));

                }
            }
        }
    }
}
