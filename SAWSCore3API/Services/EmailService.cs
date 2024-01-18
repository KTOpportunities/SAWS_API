using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SAWSCore3API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MimeKit;
using MimeKit.Text;
using MimeKit.Utils;
using MailKit.Net.Smtp;
using MailKit.Security;


namespace SAWSCore3API.Services
{
    public interface IEmailService
    {
        void Send(string to, string orgname, string fname, string lname);
    }

    public class EmailService : IEmailService
    {

        private readonly SmptSetting _appSettings;
        private readonly IConfiguration _configuration;


        public EmailService(IConfiguration configuration)
        {
            var settings = new SmptSetting();

            if (settings != null)
            {

                settings.from = configuration["SmptSettings:from"];
                settings.userName = configuration["SmptSettings:userName"];
                settings.Password = configuration["SmptSettings:Password"];
                settings.regUrl = configuration["AppURL"] + "#/register";
                settings.enableSsl = true;
                settings.Port = 465;

                settings.host = configuration["SmptSettings:host"];
                settings.applicationUrl = configuration["SmptSettings:applicationUrl"];
            }

            _appSettings = settings;
            _configuration = configuration;

        }

        public EmailService(IOptions<SmptSetting> appSettings, IConfiguration configuration)
        {
            var settings = appSettings.Value;

            if (settings.userName == null)
            {

                settings.from = "notifications@j-cred.co.za";
                settings.userName = "notifications@j-cred.co.za";
                settings.Password = "M@nagem3nt";
                settings.regUrl = "https://app.j-cred.co.za/#/register";
                settings.enableSsl = true;
                settings.Port = 465;

                settings.host = "mail.j-cred.co.za";
            }

            _appSettings = settings;
            _configuration = configuration;

        }

        public void Send(string to, string orgname, string fname, string lname)
        { 
        }

        public void SendPasswordResetEmail(string to, string htmlBody)
        {
            if (_appSettings == null)
            {
                throw new Exception("Error reading email settings");
            }


            try
            {


                var mailMessage = new MimeMessage();
                mailMessage.From.Add(new MailboxAddress("South African Weather Service", _appSettings.from));
                mailMessage.To.Add(new MailboxAddress(to, to));
                mailMessage.Subject = "South African Weather Service forgot/reset password request";

                mailMessage.Body = new TextPart("html")
                {
                    Text = htmlBody
                };

                using (var smtpClient = new SmtpClient())
                {
                    smtpClient.Connect(_appSettings.host, _appSettings.Port, _appSettings.enableSsl);
                    smtpClient.Authenticate(_appSettings.userName, _appSettings.Password);
                    smtpClient.Send(mailMessage);
                    smtpClient.Disconnect(true);
                }

            }
            catch (Exception err)
            {

            }

        }




    }
}
