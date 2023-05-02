using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace WebApi.Models.Users
{
    public class FiltersModel
    {
        [StringLength(255)]
        public string WebPage { get; set; }
        [StringLength(4000)]
        public string AllFilters { get; set; }
    }
}