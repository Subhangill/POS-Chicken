using System.ComponentModel.DataAnnotations;

namespace POS.Models
{
    public class BaseEntity
    {
        public string? CodeId { get; set; }
        public int CreatedBy { get; set; }
        public int UpdatedBy { get; set; }
        public int IsDelete { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; } = new DateTime(1900 , 1,1);


    }
}
