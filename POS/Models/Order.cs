using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Models
{
	public class Order:BaseEntity
	{
		[Key]
		public int Id { get; set; }
		public DateTime Date { get; set; }
		public int Customerid { get; set; }
		public int Supplierid { get; set; }
		public decimal Grossweight { get; set; }
		public decimal Netweight { get; set; }
		public string? Note { get; set; }

		[NotMapped]
		public string? Customername { get; set; }
		[NotMapped]
		public string? Suppname { get; set; }
	}
}
