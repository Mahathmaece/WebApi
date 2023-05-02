using System.ComponentModel.DataAnnotations;

namespace WebApi.Models.Locations
{
    public class CreateLocationModel
    {
        [Required]
        public string LocationGroup { get; set; }

        [Required]
        public string PrimaryLocation { get; set; }

        [Required]
        public string StorageArea { get; set; }
    }
}