using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApi.Entities
{
    public class UserRoleJoinUserRight
    {
        [Key]
        public int UserRoleJoinUserRightID { get; set; }
        public int UserRoleID { get; set; }
        public int UserRightID { get; set; }
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