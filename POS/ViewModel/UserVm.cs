using POS.Models;

namespace POS.ViewModel
{
	public class UserVm
	{
		public RoleAssign RoleAssign { get; set; }
		public UserLogin UserLogin { get; set; }
		public List<Employee> Employeelist { get; set; }
		public List<FormAction> FormActionlist { get; set; }
		public List<Form> Formlist { get; set; }
		public List<FormHead> FormHeadlist { get; set; }
		public List<Role> Rolelist { get; set; }
		public List<RoleAssign> RoleAssignList { get; set; }
	}
}
