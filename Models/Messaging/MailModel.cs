using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;

namespace WebApi.Models.Messaging
{
    [NotMapped]
    public class MailModel
    {
        [Key]
        public Guid Id { get; set; }

        public string SendingStaffName { get; set; }
        public string SendingStaffEmail { get; set; }
        public string ReceivingStaffName { get; set; }
        public string ReceivingStaffEmail { get; set; }

        [Column(TypeName = "text")]
        public string Subject { get; set; }
        [Column(TypeName = "text")]
        public string Message { get; set; }

        [DisplayFormat(DataFormatString = "{yyyy-MM-dd H:mm:ss}",
        ApplyFormatInEditMode = true)]
        public DateTime SentTime { get; set; }

        public bool SentSuccessToSMTPServer { get; set; }   // AMS send status to SMTP server and NOT recipient receive status!

        public bool Read { get; set; }
        public bool Starred { get; set; }
        public bool Important { get; set; }
        public bool HasAttachments { get; set; }
        public int Label { get; set; }
        public int Folder { get; set; }
    }

    //MH
    public class MailsPageObj
    {
        public IEnumerable<MailModel> results { get; set; }
        public int totalRows { get; set; }
        public int pageNumber { get; set; }
        public int rowsOfPage { get; set; }
    }

}