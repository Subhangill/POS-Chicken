using System.ComponentModel.DataAnnotations;

namespace POS.Models
{
    public class Production :BaseEntity
    {
        [Key]
        public int Id { get; set; }
         public DateTime Date { get; set; }
        public string Note { get; set; } 
        public decimal RawTotal { get; set; }
        public decimal FinalTotal { get; set; }

    }
}
