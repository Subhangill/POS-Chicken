using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace POS.Models
{
	public class Voucher:BaseEntity
	{
		[Key]
		public int Id { get; set; }
		public string Type { get; set; }
		public int Vno { get; set; }
		public string? Detail { get; set; } = "";
		public DateTime Date { get; set; }
		public decimal Dr { get; set; }
		public decimal Cr{ get; set; }
		public int InvId { get; set; }
		public int AccountId { get; set; }
		public int TranId { get; set; }
		[NotMapped] public string? ImagePath { get; set; }
		[NotMapped] public IFormFile? ImageFile { get; set; }
	}
}
