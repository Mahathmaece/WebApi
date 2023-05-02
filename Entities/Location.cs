using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApi.Entities
{
    public class Location
    {
        [Key]
        public int LocationID { get; set; }
        public int BusinessAreaID { get; set; }
        [StringLength(50)]
        public string LocationName { get; set; }
        [StringLength(100)]
        public string LocationDescription { get; set; }
    }
}