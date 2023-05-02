using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApi.Entities
{
    public class UserRight
    {
        [Key]
        public int UserRightID { get; set; }
        [StringLength(255)]
        public string UserRightDescription { get; set; }
        [StringLength(255)]
        public string PageModule { get; set; }
    }
}