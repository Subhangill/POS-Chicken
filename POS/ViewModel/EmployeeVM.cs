using POS.Models;

namespace POS.ViewModel
{
    public class EmployeeVM
    {
        public List<Area> arealist { get; set; }
        public Employee employee { get; set; }
        public EmployeeArea? employeeArea { get; set; }
        public List<int> SelectedAreaIds { get; set; }
    }
}
