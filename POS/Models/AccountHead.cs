using System.ComponentModel.DataAnnotations;

namespace POS.Models
{
	public class AccountHead
	{
		[Key]
		public int Id { get; set; }
		public string? Name { get; set; }
	}
}
