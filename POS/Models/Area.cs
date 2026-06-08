using System.ComponentModel.DataAnnotations;

namespace POS.Models
{
    public class Area :BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public string? Name { get; set; }
        public string? UrduName { get; set; }
    }
}
