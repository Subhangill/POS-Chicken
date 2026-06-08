namespace POS.Models
{
    public class VehicleInCreateView
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public int VehicleId { get; set; }
        public string Text { get; set; } = null!;
    }
}
