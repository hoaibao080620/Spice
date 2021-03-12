using System;
using System.IO;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using MimeKit.Text;
using Spice.Utilities;

namespace Spice.Services {
    public class EmailSender : IEmailSender {
        public async Task SendEmailAsync(string destination, string customerName, string htmlMessage) {
            var mineMessage = new MimeMessage();
            mineMessage.From.Add(new MailboxAddress(Email.Name,Email.EmailAddress));
            mineMessage.To.Add(new MailboxAddress(customerName,destination));
            mineMessage.Subject = Email.EmailConfirmation;
            mineMessage.Body = new TextPart(TextFormat.Html) { Text = htmlMessage };

            // send email
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(Email.EmailAddress, Email.Password);
            await smtp.SendAsync(mineMessage);
            await smtp.DisconnectAsync(true);
        }
    }
}