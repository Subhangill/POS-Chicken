using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace POS.Models
{
	public class WasteReceive : BaseEntity
	{
		[Key]
		public int Id { get; set; }
		public int Empid { get; set; }
		public string? CodeId { get; set; } = "";
		public int Suppid { get; set; }
		public int Vehicle { get; set; }
		public int Area { get; set; }
		public int Source { get; set; }
		public decimal GrossWeight { get; set; }
		public decimal WasteWeight { get; set; }
		public string? Note { get; set; } = "";
		public decimal NetWeight { get; set; }
		public DateTime Date { get; set; }
		[NotMapped] public string? Areaname { get; set; }
		[NotMapped] public string? Suppname { get; set; }
		[NotMapped] public string? ImagePath { get; set; }
		[NotMapped] public IFormFile? ImageFile { get; set; }
	}
}
