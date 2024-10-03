using System.ComponentModel.DataAnnotations;

namespace Reliable_Reservations_MVC.Models.Reservation
{
    public class ReservationUpdateViewModel
    {
        [Required]
        public int ReservationId { get; set; }


        [Required]
        public int CustomerId { get; set; }


        [Required]
        public DateTime ReservationDate { get; set; }

        [Required]
        [Range(1, 15, ErrorMessage = "Number of guests must be between 1 and 15.")]
        public int NumberOfGuests { get; set; }

        [Required]
        public List<int> TableNumbers { get; set; } = new List<int>();

        public string? SpecialRequests { get; set; }
    }
}
