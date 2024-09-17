using System.ComponentModel.DataAnnotations;

namespace Reliable_Reservations_MVC.Models.Reservation
{
    public class ReservationCreateViewModel
    {
        public int CustomerId { get; set; }

        public DateOnly ReservationDate { get; set; }

        [Range(1, 15, ErrorMessage = "Number of guests must be between 1 and 15.")]
        public int NumberOfGuests { get; set; }

        public List<int> TableNumbers { get; set; }

        public string? SpecialRequests { get; set; } = "None";
    }
}