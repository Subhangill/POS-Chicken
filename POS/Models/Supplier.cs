using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Models
{
    public class Supplier : BaseEntity
    {
        [Key]
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? UrduName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? BusinessName { get; set; }
        public int AreaId { get; set; }
        [NotMapped] public IFormFile? ImageFile { get; set; }
        [NotMapped] public string? ImagePath { get; set; }

    }
}
