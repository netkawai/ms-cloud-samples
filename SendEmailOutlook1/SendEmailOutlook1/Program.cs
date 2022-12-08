using System.Diagnostics;
using System.Net.Mail;
using System.Net;

namespace SendEmailOutlook1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string recipients = "recipient full mail address";
            string subject = "test mail";

            using (SmtpClient client = new SmtpClient()
            {
                Host = "smtp-mail.outlook.com",
                Port = 587,
                UseDefaultCredentials = false, // This require to be before setting Credentials property
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new NetworkCredential("sender full domain mail", "app password"), // you must give a full email address for authentication 
                TargetName = "STARTTLS/smtp-mail.outlook.com", // Set to avoid MustIssueStartTlsFirst exception
                EnableSsl = true // Set to avoid secure connection exception
            })
            {

                MailMessage message = new MailMessage()
                {
                    From = new MailAddress("sender full domain mail"), // sender must be a full email address
                    Subject = subject,
                    IsBodyHtml = true,
                    Body = "<h1>Hello World</h1>",
                    BodyEncoding = System.Text.Encoding.UTF8,
                    SubjectEncoding = System.Text.Encoding.UTF8,

                };
                var toAddresses = recipients.Split(',');
                foreach (var to in toAddresses)
                {
                    message.To.Add(to.Trim());
                }

                try
                {
                    client.Send(message);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
        }
    }
}