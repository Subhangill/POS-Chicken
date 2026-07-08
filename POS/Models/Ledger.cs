namespace POS.Models
{
	public class Ledger
	{
		public string Accountname { get; set; }
		public int HeadId { get; set; }
		public int AccountId { get; set; }
		public decimal Opening { get; set; }
		public DateTime From { get; set; }
		public DateTime To { get; set; }
		public bool IsSearch { get; set; }
	}
}
