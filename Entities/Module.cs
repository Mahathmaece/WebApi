using System.ComponentModel.DataAnnotations;

namespace WebApi.Entities
{
    public class Module
    {
        [Key]
        public int ModuleID { get; set; }
        [StringLength(255)]
        public string ModuleCodeName { get; set; }
        [StringLength(255)]
        public string ModuleTitle { get; set; }
        [StringLength(255)]
        public string ModuleTranslate { get; set; }
        public int ModuleOrder { get; set; }
        [StringLength(255)]
        public string Icon { get; set; }
        public bool IsHide { get; set; }
    }
}