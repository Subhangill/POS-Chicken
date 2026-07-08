namespace POS.Models
{
	public class LedgerResult
	{
		public int InvNo { get; set; }
		public int TransId { get; set; }
		public decimal Dr { get; set; }
		public string? Detail { get; set; }
		public string? InvType { get; set; }
		public string? VType { get; set; }
		public decimal Cr{ get; set; }
		public DateTime Date { get; set; }
	}
}
