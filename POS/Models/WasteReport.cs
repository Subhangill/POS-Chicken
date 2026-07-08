namespace POS.Models
{
	public class WasteReport
	{
		public Filter Filters { get; set; }
		public class Filter
		{
			public DateTime From { get; set; }
			public DateTime To { get; set; }
			public int Source { get; set; }
			public int Pid { get; set; }
			public int Sid { get; set; }
			public int Type { get; set; }
			public bool IsSearch { get; set; }
		}
		public class ItemDetail
		{
			public int Id { get; set; }
			public string? Name { get; set; }
			public decimal Weight { get; set; }
		}
		public class InvoiceSummary
		{
			public DateTime Date { get; set; }
			public int Id { get; set; }
			public string? Suppname { get; set; }
			public string? Areaname { get; set; }
			public string? Empname { get; set; }
			public int Source { get; set; }
			public decimal Grossweight { get; set; }
			public decimal Wasteweight { get; set; }
			public decimal Netweight { get; set; }
		}
	}
}
