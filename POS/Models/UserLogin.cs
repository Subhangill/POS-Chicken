using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Models
{
	public class UserLogin : BaseEntity
	{
		public int Id { get; set; }
		public int RoleId { get; set; }
		public int EmpId { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }
		[NotMapped] public string? Empname { get; set; }
		[NotMapped] public string? Rolename { get; set; }
	}
}
