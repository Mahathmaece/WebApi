using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace WebApi.Models.Users
{
    // Use by Reports module only
    public class UpdateUserToggleColsModel
    {
        public int UserID { get; set; }
        [StringLength(255)]
        public string WebPage { get; set; }
        public List<string> SelectedToggleColumns {get; set;}
    }
}