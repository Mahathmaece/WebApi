using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApi.Models.BusinessAreas
{
    public class BusinessAreaModel
    {
        [Key]
        public int BusinessAreaID { get; set; }
        public int CompanyID { get; set; }
        [StringLength(4)]
        public string BusinessAreaCode { get; set; }
        [StringLength(255)]
        public string BusinessAreaDescription { get; set; }
        [StringLength(300)]
        public string BACombinedDesc { get; set; }
    }
}