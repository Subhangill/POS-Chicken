using System.ComponentModel.DataAnnotations;

namespace POS.Models
{
    public class VehicleIn : BaseEntity
    {
        [Key]
        public int Id { get; set; }
        
        public DateTime date { get; set; }
        
        public int employeeId { get; set; }
        public int vehicleId { get; set; }
        public decimal totalWeight { get; set; }
    }
}
