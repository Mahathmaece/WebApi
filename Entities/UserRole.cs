using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApi.Entities
{
    public class UserRole
    {
        [Key]
        public int UserRoleID { get; set; }
        [StringLength(255)]
        public string UserRoleDescription { get; set; }
    }
  
}