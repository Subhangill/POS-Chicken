using POS.Models;

namespace POS.ViewModel
{
	public class PurchaseVm
	{
		public List<Area> Arealist { get; set; }
		public List<Vehicle> Vehiclelist { get; set; }
		public List<Employee> Employeelist { get; set; }
		public List<Supplier> Supplierlist { get; set; }
		public List<Product> Productlist { get; set; }
		public List<FinishPurchaseDetail> FinishPurchaseDetaillist { get; set; } = new List<FinishPurchaseDetail>();
		public List<PurchaseDetail> PurchaseDetaillist { get; set; } = new List<PurchaseDetail>();
		public List<RawWastageDetail> RawWastageDetaillist { get; set; } = new List<RawWastageDetail>();
		public List<WasteReceiveDetail> WasteReceiveDetaillist { get; set; } = new List<WasteReceiveDetail>();
		public WasteReceive WasteReceived { get; set; }
		public FinishPurchaseMaster FinishPurchaseMaster { get; set; }
		public Purchase Purchase { get; set; }


	}
}
