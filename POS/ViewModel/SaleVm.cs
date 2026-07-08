using POS.Models;

namespace POS.ViewModel
{
	public class SaleVm
	{
		public Order Order { get; set; }
		public Sale Sale { get; set; }
		public List<OrderDetail> OrderDetaillist { get; set; } = new List<OrderDetail>();
		public List<SaleDetail> SaleDetaillist { get; set; } = new List<SaleDetail>();
		public List<Supplier> Supplierlist { get; set; }=new List<Supplier>();
		public List<Customer> Customerlist{ get; set; }=new List<Customer>();
		public List<Product> Productlist { get; set; } = new List<Product>();
	}
}
