using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace POS.Models
{
	public class Sale : BaseEntity
	{
		[Key]
		public int Id { get; set; }
		public int Customerid { get; set; }
		public DateTime Date { get; set; }
		public decimal Gross { get; set; }
		public decimal Rem { get; set; }
		public decimal Received { get; set; }
		public decimal Discount { get; set; }
		public decimal Nettotal { get; set; }
		public string? Note { get; set; }
		public string? InvType { get; set; }

		[NotMapped] public string?Custname{ get; set; }
		[NotMapped] public string?ItemNames{ get; set; }
		[NotMapped] public string?Prices{ get; set; }
		[NotMapped] public string?Qty{ get; set; }
		[NotMapped] public string? ImagePath { get; set; }
		[NotMapped] public IFormFile? ImageFile { get; set; }
	}
}
