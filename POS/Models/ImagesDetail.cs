using System.ComponentModel.DataAnnotations;

namespace POS.Models
{
	public class ImagesDetail
	{
		[Key]
		public int Id { get; set; }
		public int Recordid { get; set; }
		public string ImagePath { get; set; }
		public string Invtype{ get; set; }
	}
}
