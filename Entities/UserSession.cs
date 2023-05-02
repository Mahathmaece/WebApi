using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApi.Entities
{
    public class UserSession
    {
        [Key]
        public Guid SessionID { get; set; }
        public bool IsLDAPLogin { get; set; }
        [StringLength(255)]
        public string LoginSource { get; set; } // Web, Mobile
        [StringLength(255)]
        public string DeviceIDOrIPAddress { get; set; }
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{yyyy-MM-dd H:mm:ss}",
        ApplyFormatInEditMode = true)]
        public DateTime ExpiredOn { get; set; }
        public bool IsLogout { get; set; }
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{yyyy-MM-dd H:mm:ss}",
        ApplyFormatInEditMode = true)]
        public DateTime CreatedOn { get; set; }
        public int CreatedBy { get; set; }
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{yyyy-MM-dd H:mm:ss}",
        ApplyFormatInEditMode = true)]
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedBy { get; set; }

    }
}