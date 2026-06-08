using System.ComponentModel.DataAnnotations;

namespace POS.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }
        
        public string? Name { get; set; }
        public string? UrduName { get; set; }
    }
}
