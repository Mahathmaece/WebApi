using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApi.Models.Locations
{
    public class LocationModel
    {
        public long LocationID { get; set; }
        public string LocationFullName { get; set; }

        public string ParentName { get; set; }

        public bool IsLock { get; set; }
    }
}