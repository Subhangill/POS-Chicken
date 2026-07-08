using System.ComponentModel.DataAnnotations;

namespace POS.Models
{
	public class WasteReceiveSubDetail
	{
		[Key]
		public int Id { get; set; }
		public int Pid { get; set; }
		public int Invid { get; set; }
		public int WasteReceiveDetailId { get; set; }
		public int SubSuppid { get; set; }        
		public decimal Weight { get; set; }
	}
}
