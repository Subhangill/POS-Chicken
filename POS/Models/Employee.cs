using System.ComponentModel.DataAnnotations;

namespace POS.Models
{
    public class Employee : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public string? Name { get; set; }
        public string? UrduName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime JoiningDate { get; set; }
        public decimal Salary { get; set; }
        public bool status { get; set; }
    }
}
