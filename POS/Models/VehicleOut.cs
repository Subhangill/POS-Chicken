using System.ComponentModel.DataAnnotations;

namespace POS.Models
{
    public class VehicleOut :BaseEntity
    {
        [Key]
        public int Id { get; set; }
        public DateTime DateAndTime { get; set; }
        public int employeeId { get; set; }
        public int vehicleId { get; set; }
        public bool status { get;set; }

    }
}
