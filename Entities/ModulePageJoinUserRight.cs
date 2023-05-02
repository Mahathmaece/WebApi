using System.ComponentModel.DataAnnotations;

namespace WebApi.Entities
{
    public class ModulePageJoinUserRight
    {
        [Key]
        public int ModulePageJoinUserRightID { get; set; }
        public int ModulePageID { get; set; }
        public int UserRightID { get; set; }
    }
}