using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace WebApi.Entities
{
    // Use by Reports module only
    public class UserSavedFilter
    {
        [Key]
        public int UserSavedFilterID { get; set; }
        public int UserID { get; set; }
        [StringLength(255)]
        public string WebPage { get; set; }

        // https://docs.microsoft.com/en-us/ef/core/modeling/backing-field
        //[StringLength(4000)]
        //private string AllFilters;

        [StringLength(4000)]
        public string AllFilters { get; set; }

        //[NotMapped]
        //public JObject AllFiltersJson
        //{
        //    get
        //    {
        //        return JsonConvert.DeserializeObject<JObject>(string.IsNullOrEmpty(AllFilters) ? "{}" : AllFilters);
        //    }
        //    set
        //    {
        //        AllFilters = value.ToString();
        //    }
        //}
    }
}