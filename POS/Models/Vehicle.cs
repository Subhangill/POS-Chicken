using System.ComponentModel.DataAnnotations;

namespace POS.Models
{
    public class Vehicle :BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public string? VehicleNo { get; set; }
        public bool status { get; set; }
    }
}
