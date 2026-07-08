using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Models
{
	public class PaymentMaster : BaseEntity
	{
		[Key]
		public int Id { get; set; }
		public int Suppid { get; set; }
		public DateTime Date { get; set; }
		public decimal Grossamount { get; set; }
		public decimal Previous { get; set; }
		public decimal Totalamount { get; set; }
		public string? Note { get; set; }
		[NotMapped] public string? Suppname { get; set; }
		[NotMapped] public string? Itemdetails { get; set; }
		[NotMapped] public string? Prices { get; set; }
		[NotMapped] public string? Qtys { get; set; }
	}
}
