using System.ComponentModel.DataAnnotations;

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

    }
}
