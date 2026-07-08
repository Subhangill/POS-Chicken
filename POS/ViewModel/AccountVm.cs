using POS.Models;

namespace POS.ViewModel
{
	public class AccountVm
	{
		public List<AccountHead> AccountHeadlist { get; set; } = new List<AccountHead>();
		public List<LedgerResult> LedgerResultlist { get; set; } = new List<LedgerResult>();
		public List<Account> Accountlist { get; set; } = new List<Account>();
		public List<AccountOpening> AccountOpeninglist { get; set; } = new List<AccountOpening>();
		public List<Account_SubHead> Account_SubHeadlist { get; set; } = new List<Account_SubHead>();
		public Account Account { get; set; } = new Account();
		public Ledger Ledger { get; set; } 
	}
}
