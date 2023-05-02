using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApi.Entities
{
    public class MailAttachment
    {
        [Key]
        public Guid AttachmentID { get; set; }
        public Guid MailID { get; set; }
        [Column(TypeName = "text")]
        public string SavedPath { get; set; }
        [Column(TypeName = "text")]
        public string Filename { get; set; }
    }
}