using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using POS.Models;

namespace POS.Data
{
	public class ApplicationDbContext : IdentityDbContext
	{
		public DbSet<Form> Form { get; set; }
		public DbSet<FormHead> FormHead { get; set; }
		public DbSet<FormAction> FormAction { get; set; }
		public DbSet<RoleAssign> RoleAssign { get; set; }
		public DbSet<UserLogin> UserLogin { get; set; }
		public DbSet<Role> Role { get; set; }
		public DbSet<PaymentMaster> PaymentMaster { get; set; }
		public DbSet<ImagesDetail> ImagesDetail { get; set; }
		public DbSet<PaymentDetail> PaymentDetail { get; set; }
		public DbSet<FinishPurchaseMaster> FinishPurchaseMaster { get; set; }
		public DbSet<FinishPurchaseDetail> FinishPurchaseDetail { get; set; }
		public DbSet<AccountOpening> AccountOpening { get; set; }
		public DbSet<Voucher> Voucher { get; set; }
		public DbSet<VoucherDetail> VoucherDetail { get; set; }
		public DbSet<OrderDetail> OrderDetail { get; set; }
		public DbSet<Order> Order { get; set; }
		public DbSet<Sale> Sale { get; set; }
		public DbSet<SaleDetail> SaleDetail { get; set; }
		public DbSet<WasteReceiveSubDetail> WasteReceiveSubDetail { get; set; }
		public DbSet<WasteReceiveDetail> WasteReceiveDetail { get; set; }
		public DbSet<WasteReceive> WasteReceivedMaster { get; set; }
		public DbSet<RawWastageDetail> RawWastageDetail { get; set; }
		public DbSet<Purchase> Purchase { get; set; }
		public DbSet<PurchaseDetail> PurchaseDetail { get; set; }
		public DbSet<TransactionDetail> TransactionDetail { get; set; }
		public DbSet<Account> Account { get; set; }
		public DbSet<Account_SubHead> Account_SubHead { get; set; }
		public DbSet<Area> Area { get; set; }
		public DbSet<AccountHead> AccountHead { get; set; }
		public DbSet<Category> Category { get; set; }
		public DbSet<Customer> Customer { get; set; }
		public DbSet<Employee> Employee { get; set; }
		public DbSet<EmployeeArea> EmployeeArea { get; set; }
		public DbSet<Supplier> Supplier { get; set; }
		public DbSet<Product> Product { get; set; }
		public DbSet<VehicleIn> VehicleIn { get; set; }
		public DbSet<VehicleInDetail> VehicleInDetail { get; set; }
		public DbSet<Setting> Setting { get; set; }
		public DbSet<VehicleOut> VehicleOut { get; set; }
		public DbSet<Vehicle> Vehicle { get; set; }
		public DbSet<Production> Production { get; set; }
		public DbSet<RawDetail> RawDetails { get; set; }
		public DbSet<FinishDetail> FinishDetails { get; set; }
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
			: base(options)
		{
		}
	}
}
