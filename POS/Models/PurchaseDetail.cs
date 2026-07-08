using System.ComponentModel.DataAnnotations;

namespace POS.Models
{
	public class PurchaseDetail
	{
		[Key]
		public int Id { get; set; }
		public int PurchaseId { get; set; }
		public int Pid { get; set; }
		public decimal Qty { get; set; }
		public int Invoicetype { get; set; }
		public int Suppid { get; set; }
		public decimal Price { get; set; }
		public decimal Total { get; set; }
		public decimal Discount { get; set; }
		public decimal Tax { get; set; }
		public decimal Nettotal { get; set; }
	}
}
