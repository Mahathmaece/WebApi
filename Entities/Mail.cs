using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading;
using System.ComponentModel;
using System.Text.RegularExpressions;
using WebApi.Helpers;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace WebApi.Entities
{
    public class Mail
    {
        [Key]
        public Guid Id { get; set; }

        // Mail from(in Messaging app, "From" , is who we are sending the email to)
        public int SendingUserID { get; set; }
        public User SendingUser { get; set; }

        public int ReceivingUserID { get; set; }
        public User ReceivingUser { get; set; }

        [Column(TypeName = "text")]
        public string Subject { get; set; }
        [Column(TypeName = "text")]
        public string Message { get; set; }

        [DisplayFormat(DataFormatString = "{yyyy-MM-dd H:mm:ss}",
        ApplyFormatInEditMode = true)]
        public DateTime SentTime { get; set; }

        public bool SentSuccessToSMTPServer { get; set; }   // AMS send status to SMTP server and NOT recipient receive status!

        [StringLength(3000)]
        public string ErrorMessage { get; set; }

        public bool Read { get; set; }
        public bool Starred { get; set; }
        public bool Important { get; set; }
        public bool HasAttachments { get; set; }
        public int Label { get; set; }
        public int Folder { get; set; }

        public Guid? OriginMailID { get; set; }

        public AppSettings _appSettings;
        public List<Attachment> attachments;
        public ILogger _log;

        public Mail()
        {
            attachments = new List<Attachment>();
        }

        public Mail(AppSettings appSettings)
        {
            attachments = new List<Attachment>();
            _appSettings = appSettings;
        }

        public Mail(AppSettings appSettings, ILogger log)
        {
            attachments = new List<Attachment>();
            _appSettings = appSettings;
            _log = log;
        }

        // mailType - used to identify the type you want to know when handling callback
        // For e.g. "1" - asset verification (if its type 1, in callback we can do other actions)
        // "2" - servicing
        public int send()
        {
            if (!isEmailValid(this.ReceivingUser.StaffEmail))
            {
                return -1;
            }

            int mailStatus = -1;

            try
            {
                _log.LogInformation("Preparing to send email to " + this.ReceivingUser.StaffEmail);
                SmtpClient client = new SmtpClient(_appSettings.EmailServerHost);
                client.UseDefaultCredentials = false;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.EnableSsl = bool.Parse(_appSettings.EmailViaSSL);
                client.Port = int.Parse(_appSettings.EmailPort);

                if (_appSettings.EmailAccountUserName.Length == 0)
                {
                    // no credentials specified in config
                    client.Credentials = null;
                }
                else
                {
                    var basicCredential = new NetworkCredential(_appSettings.EmailAccountUserName, _appSettings.EmailAccountPW);
                    client.Credentials = basicCredential;
                }

                MailAddress from;
                if (_appSettings.EmailFromAddress.Length == 0)
                {
                    from = new MailAddress(this.SendingUser.StaffEmail, this.SendingUser.StaffName);
                }
                else
                {
                    from = new MailAddress(_appSettings.EmailFromAddress, _appSettings.EmailFromName);
                }

                MailAddress to = new MailAddress(this.ReceivingUser.StaffEmail);

                MailMessage message = new MailMessage(from, to);

                string pattern = @"<[^>]*>|&emsp;|&ensp;";
                var htmlDecodedMessage = Regex.Replace(this.Message, pattern, string.Empty);
                // var htmlDecodedMessage = Regex.Replace(this.Message, @"[<p>]+[</p>]+|&emsp;|&ensp;>", string.Empty);
                message.Body = htmlDecodedMessage;
                message.Subject = this.Subject;

                if (HasAttachments)
                {
                    foreach (Attachment a in attachments)
                    {
                        message.Attachments.Add(a);
                    }
                }

                try
                {
                    _log.LogInformation("Attempting to send email to " + this.ReceivingUser.StaffEmail);
                    client.Send(message);
                    mailStatus = 1;
                    _log.LogInformation("Email sent to " + this.ReceivingUser.StaffEmail + " with no errors encountered.");
                }
                catch (SmtpFailedRecipientsException ex)
                {
                    _log.LogInformation("Email Delivery failed - SmtpFailedRecipientsException: " + ex.Message);
                    int attempt = 1;
                    for (int i = 0; i < ex.InnerExceptions.Length; i++)
                    {
                        SmtpStatusCode status = ex.InnerExceptions[i].StatusCode;
                        if (status == SmtpStatusCode.MailboxBusy ||
                            status == SmtpStatusCode.MailboxUnavailable)
                        {
                            _log.LogInformation("Email Delivery failed - retrying in 5 seconds.");
                            Console.WriteLine("Email Delivery failed - retrying in 5 seconds.");
                            System.Threading.Thread.Sleep(5000);
                            attempt++;
                            _log.LogInformation("Email " + attempt + "th attempt to send email to " + this.ReceivingUser.StaffEmail);
                            client.Send(message);
                            mailStatus = 1;
                        }
                        else
                        {
                            _log.LogInformation("Email Failed to deliver message to {0}", ex.InnerExceptions[i].FailedRecipient);
                            Console.WriteLine("Email Failed to deliver message to {0}",
                                ex.InnerExceptions[i].FailedRecipient);

                            mailStatus = 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.LogInformation("Email Exception caught in RetryIfBusy(): {0}", ex.ToString());
                    Console.WriteLine("Email Exception caught in RetryIfBusy(): {0}",
                            ex.ToString());

                    mailStatus = -1;
                }
            }
            catch (Exception ex1)
            {
                _log.LogInformation("Email Exception: ", ex1.Message);
                mailStatus = -1;
            }

            return mailStatus;
        }

        private bool isEmailValid(string email)
        {
            return email != null && new EmailAddressAttribute().IsValid(email);
        }
    }


}