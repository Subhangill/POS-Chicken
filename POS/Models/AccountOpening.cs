using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Models
{
	public class AccountOpening
	{
		[Key]
		public int Id { get; set; }
		public int AccountId { get; set; }
		public decimal Dr { get; set; }
		public decimal Cr { get; set; }
		public DateTime Date { get; set; }
		public string? Detail { get; set; }
		[NotMapped]public string? Accountname { get; set; }
	}
}
