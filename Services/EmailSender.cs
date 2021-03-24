using System;
using System.IO;
using System.Net;
using System.Net.Mail;
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
            //var mineMessage = new MimeMessage();
            //mineMessage.From.Add(new MailboxAddress(Email.Name,Email.EmailAddress));
            //mineMessage.To.Add(new MailboxAddress(customerName,destination));
            //mineMessage.Subject = Email.EmailConfirmation;
            //mineMessage.Body = new TextPart(TextFormat.Html) { Text = htmlMessage };

            //// send email
            //using var smtp = new SmtpClient();
            
            //await smtp.ConnectAsync("smtp.gmail.com", 465, SecureSocketOptions.StartTls);
            //await smtp.AuthenticateAsync(Email.EmailAddress, Email.Password);
            //await smtp.SendAsync(mineMessage);
            //await smtp.DisconnectAsync(true);

            using (MailMessage mail = new MailMessage()) {
                mail.From = new MailAddress("hoaibao08062000@gmail.com");
                mail.To.Add(destination);
                mail.Subject = "Spice Shop";
                mail.Body = htmlMessage;
                mail.IsBodyHtml = true;

                using System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient("smtp.gmail.com", 587);
                smtp.Credentials = new NetworkCredential("hoaibao08062000@gmail.com", "hoaibao0806^^");
                smtp.EnableSsl = true;
                await smtp.SendMailAsync(mail);
            }
        }
    }
}