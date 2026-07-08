namespace POS.Models
{
	public class PurchaseReport
	{
		public Filter PReportfilter { get; set; }
		public class Filter
		{
			public DateTime From { get; set; }
			public DateTime To { get; set; }
			public int Sid { get; set; }
			public int Pid { get; set; }
			public int Ptype { get; set; }
			public int Rtype { get; set; }
			public bool IsSearch { get; set; }
		}
		public class InvoiceSummary
		{
			public int Id { get; set; }
			public string? Suppname { get; set; }
			public decimal Grosstotal { get; set; }
			public decimal Discount { get; set; }
			public decimal Nettotal { get; set; }
			public DateTime Date { get; set; }
		}
		public class ItemDetail
		{
			public int Pid { get; set; }
			public string? Pname { get; set; }
			public decimal Price { get; set; }
			public decimal Qty { get; set; }
			public decimal Total { get; set; }
		}
	}
}
