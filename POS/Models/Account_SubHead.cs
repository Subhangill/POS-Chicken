using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Models
{
	public class Account_SubHead:BaseEntity
	{
		[Key]
		public int Id { get; set; }
		public int HeadId { get; set; }
		public string? Name { get; set; }
		[NotMapped]
		public string? Headname { get; set; }
	}
}
