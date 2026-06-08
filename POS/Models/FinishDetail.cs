using System.ComponentModel.DataAnnotations;

namespace POS.Models
{
    public class FinishDetail
    {
        [Key]
        public int Id { get; set; }
        public int masterId { get; set; }
        public int productId { get; set; }
        public decimal weight { get; set; }

    }
}
