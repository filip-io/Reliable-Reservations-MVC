using Reliable_Reservations_MVC.Models.Customer;
using Reliable_Reservations_MVC.Models.Table;

namespace Reliable_Reservations_MVC.Models.Reservation
{
    public enum ReservationStatus
    {
        Pending,
        Confirmed,
        Canceled,
        Completed,
        NoShow
    }


    public class ReservationDetailsViewModel
    {
        public int ReservationId { get; set; }

        public CustomerViewModel? Customer { get; set; }

        public DateTime ReservationDate { get; set; }

        public int NumberOfGuests { get; set; }

        public string? SpecialRequests { get; set; }

        public ReservationStatus Status { get; set; }

        public List<TableViewModel> Tables { get; set; } = new List<TableViewModel>();
    }
}