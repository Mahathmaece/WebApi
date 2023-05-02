using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApi.Entities
{
    public class Company
    {
        [Key]
        public int CompanyID { get; set; }
        [StringLength(4)]
        public string CompanyCode { get; set; }
        [StringLength(255)]
        public string CompanyDescription { get; set; }
    }
}