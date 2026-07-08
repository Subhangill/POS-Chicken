using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Models
{
	public class WasteReceiveDetail
	{
		public int Id { get; set; }
		public int Invid { get; set; }
		public decimal Weight { get; set; }
		public int Suppid { get; set; }
		public int Pid { get; set; }
		[NotMapped]
		public List<WasteReceiveSubDetail> SubDetails { get; set; } = new List<WasteReceiveSubDetail>();
	}
}
