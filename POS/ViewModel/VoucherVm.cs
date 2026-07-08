using POS.Models;

namespace POS.ViewModel
{
	public class VoucherVm
	{
		public Voucher Voucher { get; set; }
		public List<Account> Banklist { get; set; }=new List<Account>();
		public List<Account> Accountlist { get; set; }=new List<Account>();
		public List<VoucherDetail> VoucherDetaillist { get; set; } = new List<VoucherDetail>();
	}
}
