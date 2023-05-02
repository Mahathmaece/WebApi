using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApi.Entities
{
    public class DefaultToggleColumn
    {
        [Key]
        public int DefaultToggleColumnID { get; set; }
        [StringLength(255)]
        public string WebPage { get; set; }
        [StringLength(255)]
        public string SelectedColumn { get; set; }
    }
}