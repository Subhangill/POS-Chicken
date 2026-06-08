using System.Collections.Generic;
using POS.Models;

namespace POS.ViewModel
{
    public class VehicleOutVM
    {
        public VehicleOut vehicleOut { get; set; }
        public List<Employee> EmployeeList { get; set; }
        public List<Vehicle> VehicleList { get; set; }

        public Vehicle vehicle { get; set; }
        public Employee employee { get; set; }
        public Setting setting { get; set; }
        public List<VehicleOutItemVM> Items { get; set; }
    }

    public class VehicleOutItemVM
    {
        public string SupplierName { get; set; }
        public string ProductName { get; set; }
        public decimal Kg { get; set; }
    }
}
