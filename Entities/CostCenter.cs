using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApi.Entities
{
    public class CostCenter
    {
        [Key]
        public int CostCenterID { get; set; }
        [StringLength(50)]
        public string CostCenterCode { get; set; }
        [StringLength(50)]
        public string CostCenterDescription { get; set; }
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{yyyy-MM-dd H:mm:ss}",
        ApplyFormatInEditMode = true)]
        public DateTime? ValidToDate { get; set; }
    }
}