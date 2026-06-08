using System.ComponentModel.DataAnnotations;

namespace POS.Models
{
    public class Product :BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public string? Name { get; set; }
        public string? UrduName { get; set; }
        public int BrandId { get; set; }
        public int CategoryId { get; set; }
        public decimal Price { get; set; }

    }
}
