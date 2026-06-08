using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Models
{
    public class VehicleInDetail : BaseEntity
    {
        [Key]

        public int Id { get; set; }

        public int productId { get; set; }
        public decimal Kg { get; set; }
        public int SupplierId { get; set; }

    }
}
