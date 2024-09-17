namespace Reliable_Reservations_MVC.Models.Table
{
    public class TableViewModel
    {
        public int TableId { get; set; }
        public required int TableNumber { get; set; }
        public required int SeatingCapacity { get; set; }
        public string Location { get; set; } = string.Empty;
    }
}