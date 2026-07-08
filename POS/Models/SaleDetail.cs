using System.ComponentModel.DataAnnotations;

namespace POS.Models
{
	public class SaleDetail
	{
		[Key]
		public int Id { get; set; }
		public int InvId { get; set; }
		public int Pid { get; set; }
		public decimal Price { get; set; }
		public decimal Qty{ get; set; }
		public decimal Total{ get; set; }
		public decimal Nettotal{ get; set; }
		public decimal Disc{ get; set; }
		public decimal Tax{ get; set; }
		public decimal Rent{ get; set; }
		public decimal Cutof { get; set; }
	}
}
