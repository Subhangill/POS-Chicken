using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace POS.Models
{
	public class FinishPurchaseMaster:BaseEntity
	{
		[Key]
		public int Id { get; set; }
		public int SuppId { get; set; }
		public string? InvType { get; set; }
		public decimal Tax { get; set; }
		public string? Detail { get; set; }
		public string? Note { get; set; }
		public decimal Nettotal { get; set; }
		public decimal Discount { get; set; }
		public decimal Paid { get; set; }
		public decimal Rem { get; set; }
		public decimal Grosstotal { get; set; }
		public int InvNo { get; set; }
		public int Source { get; set; }
		public int Invoicetype { get; set; }
		public DateTime Date { get; set; }
		[NotMapped] public string? Suppname { get; set; }
		[NotMapped] public string? Itemdetails { get; set; }
		[NotMapped] public string? Prices { get; set; }
		[NotMapped] public string? Qtys { get; set; }
		[NotMapped] public string? ImagePath { get; set; }
		[NotMapped] public IFormFile? ImageFile { get; set; }
	}
}
