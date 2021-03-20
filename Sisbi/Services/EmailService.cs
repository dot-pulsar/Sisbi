using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Models;
using Sisbi.Services.Contracts;
using Sisbi.Settings;

namespace Sisbi.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(EmailRequest mailRequest)
        {
            var email = new MimeMessage
            {
                Sender = new MailboxAddress(_emailSettings.DisplayName, _emailSettings.Email),
                To = {MailboxAddress.Parse(mailRequest.ToEmail)},
                Subject = mailRequest.Subject,
                Body = new BodyBuilder
                {
                    HtmlBody = mailRequest.Body
                }.ToMessageBody()
            };

            using var smtp = new SmtpClient();
            
            await smtp.ConnectAsync(_emailSettings.Host, _emailSettings.Port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_emailSettings.Email, _emailSettings.Password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}