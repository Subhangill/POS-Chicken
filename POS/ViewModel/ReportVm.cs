using POS.Models;

namespace POS.ViewModel
{
	public class ReportVm
	{
		//Purchase Report
		public PurchaseReport.Filter Preportfilter { get; set; }
		public List<PurchaseReport.ItemDetail> PreportItemDetaillist { get; set; }
		public List<PurchaseReport.InvoiceSummary> PreportInvoiceSummarylist { get; set; }
		//

		//Sale Report
		public SaleReport.Filter Sreportfilter { get; set; }
		public List<SaleReport.ItemDetail> SreportItemDetaillist { get; set; }
		public List<SaleReport.InvoiceSummary> SreportInvoiceSummarylist { get; set; }
		//
		//Stock Report
		public StockReport.Filter StockReportfilter { get; set; }
		public List<StockReport.RawStockReport> RawStockReportlist { get; set; }
		public List<StockReport.FinishStockReport> FinishStockReportlist { get; set; }
		//
		//Waste Report
		public WasteReport.Filter WasteReportfilter { get; set; }
		public List<WasteReport.InvoiceSummary> WreportInvoicelist { get; set; }
		public List<WasteReport.ItemDetail> WreportItemdetaillist { get; set; }
		//
		public List<Supplier> Supplierlist { get; set; }
		public List<Customer> Customerlist { get; set; }
		public List<Product> Productlist { get; set; }



	}
}
