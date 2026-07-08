using System.ComponentModel.DataAnnotations;

namespace POS.Models
{
	public class TransactionDetail
	{
		[Key]
		public int Id { get; set; }
		public int InvNo { get; set; }
		public int VoucherNo { get; set; }
		public string? InvType { get; set; }
		public string? VType { get; set; }
		public DateTime Date { get; set; }
		public DateTime Datetime { get; set; }
		public decimal Dr { get; set; }
		public string? Detail { get; set; }
		public decimal Cr { get; set; }
		public int TransId { get; set; }
		public int Accountno { get; set; }
	}
}
