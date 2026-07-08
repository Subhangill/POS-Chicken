using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Models
{
	public class VoucherDetail
	{
		[Key]
		public int Id { get; set; }
		public int InvId { get; set; }
		public int AccountId { get; set; }
		public int VoucherId { get; set; }
		public decimal Dr { get; set; }
		public DateTime? ChequeDate { get; set; } = new DateTime(year: 1990,01,01);
		public string? ChequeStatus { get; set; }=string.Empty;
		public string? ChequeNo { get; set; }=string.Empty;
		public string? Vtype { get; set; } = string.Empty;
		public string? Detail { get; set; } = string.Empty;
		public decimal Cr { get; set; }
		[NotMapped]
		public string? Accname { get; set; }
		[NotMapped]
		public bool IsBank { get; set; }
	}
}
