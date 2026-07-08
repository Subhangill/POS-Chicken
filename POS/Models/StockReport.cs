namespace POS.Models
{
	public class StockReport
	{
		public Filter Filters { get; set; }
		public class Filter
		{
			public DateTime From { get; set; }
			public DateTime To { get; set; }
			public bool IsSearch { get; set; }
		}
		public class RawStockReport
		{
			public int Id { get; set; }
			public string? Name { get; set; }
			public decimal Opn { get; set; }
			public decimal Production { get; set; }
			public decimal Bal { get; set; }
			public decimal Received { get; set; }
			public decimal Purchase { get; set; }
		}
		public class FinishStockReport
		{
			public int Id { get; set; }
			public string? Name { get; set; }
			public decimal Opn { get; set; }
			public decimal Production { get; set; }
			public decimal Bal { get; set; }
			public decimal Sale { get; set; }
			public decimal Purchase { get; set; }
		}
	}
}
