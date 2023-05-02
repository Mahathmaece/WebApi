using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApi.Entities
{
    public class UserJoinUserRole
    {
        [Key]
        public int UserJoinUserRoleID { get; set; }
        public int UserID { get; set; }
        public int UserRoleID { get; set; }
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
        public bool IsLock { get; set; }

        // field that is updated by SAP pulling interface app only (for UserJoinUserRoles trigger to determine if need to insert into Audit Log)
        public bool? IsLastModifiedBySAPInterface { get; set; }
    }
}