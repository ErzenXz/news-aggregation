namespace NewsAggregation.Services
{
    using Microsoft.AspNetCore.Http.HttpResults;
    using Microsoft.AspNetCore.Mvc;
    using NewsAggregation.Services.ServiceJobs.Email.Deprecated;
    using System;
    using System.Net.Mail;

    public class SecureMail(IBackgroundTaskQueue taskQueue)
    {
        private readonly IBackgroundTaskQueue _taskQueue = taskQueue;

        // SMTP server details
        private const string SmtpServer = "sandbox.smtp.mailtrap.io";
        private const int SmtpPort = 587;
        private const string SmtpUsername = "5648ffb3f4ea38";
        private const string SmtpPassword = "6f4335407f828e";

        public async Task<IActionResult> SendEmail(string fromAddress, string toAddress, string subject, string body)
        {
            MailMessage mail = new MailMessage(fromAddress, toAddress, subject, body);

            SmtpClient client = new SmtpClient(SmtpServer)
            {
                Port = SmtpPort,
                Credentials = new System.Net.NetworkCredential(SmtpUsername, SmtpPassword),
                EnableSsl = false
            };

            try
            {
                await _taskQueue.QueueBackgroundWorkItemAsync(async token =>
                {
                    await client.SendMailAsync(mail);
                });

                return new AcceptedResult();
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new { Message = ex.Message });
            }
        }
    }

}
