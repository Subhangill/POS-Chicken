using System.ComponentModel.DataAnnotations;

namespace POS.Models
{
	public class PaymentDetail
	{
		[Key]
		public int Id { get; set; }
		public int InvId { get; set; }
		public int Pid { get; set; }
		public decimal Rate { get; set; }
		public decimal Weight { get; set; }
		public decimal Total { get; set; }
		public DateTime Startdate { get; set; } = AppDate.Today;
		public DateTime Enddate { get; set; }=AppDate.Today;



	}
}
