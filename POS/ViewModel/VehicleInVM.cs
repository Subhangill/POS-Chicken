using POS.Models;

namespace POS.ViewModel
{
    public class VehicleInVM
    {
        public VehicleIn vehicleIn { get; set; } 
        public List<VehicleInDetail> vehicleInDetailList { get; set; }
        public VehicleInDetail vehicleInDetail { get; set; }
        public List<Product> productList { get; set; }
        public List<Supplier> SupplierList { get; set; }
        public List<Vehicle> vehicleList { get; set; }
        public List<Employee> EmployeeList { get; set; }
        public Product product { get; set; }    
        public List<Product> RawProduct { get; set; }
        public List<Product> FinalProduct { get; set; }
        public List<VehicleInCreateView> EmployeeVehicle { get; set; }





        // Navigation helpers
        public Employee employee { get; set; }
        public Supplier supplier { get; set; }
        public Vehicle vehicle { get; set; }
        public Setting setting { get; set; }

        // Dropdown selection structures
        public int? SelectedVehicleOutId { get; set; }

        // Navigation and Print mapping helpers
        public VehicleOut vehicleOut { get; set; }
        public List<VehicleOutItemVM> Items { get; set; }
    }

  
}
