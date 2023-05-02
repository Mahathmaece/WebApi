using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace WebApi.Models.Users
{
    public class ToggleColsModel
    {
        [StringLength(255)]
        public string WebPage { get; set; }
        [StringLength(255)]
        public string SelectedColumn { get; set; }
    }
}