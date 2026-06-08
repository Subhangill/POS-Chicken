using System.ComponentModel.DataAnnotations;

namespace POS.Models
{
    public class EmployeeArea :BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        public int AreaId { get; set; }
    }
}
