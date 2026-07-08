using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Models
{
	public class Account : BaseEntity
	{
		[Key]
		public int Id { get; set; }
		public int Cid { get; set; }
		public string? Name { get; set; }
		public int HeadId { get; set; }
		public int SubHead { get; set; }
		public int AccountNo { get; set; }
		[NotMapped]
		public string? Headname { get; set; }
		[NotMapped]
		public string? Subheadname { get; set; }
	}
}
