using POS.Models;

namespace POS.ViewModel
{
	public class PaymentVm
	{
		public PaymentMaster PaymentMaster { get; set; }
		public List<PaymentDetail> PaymentDetaillist { get; set; } = new List<PaymentDetail>();
		public List<Supplier> Supplierlist { get; set; }
		public List<Product> Productlist { get; set; }
	}
}
