using System.ComponentModel.DataAnnotations;
using Reliable_Reservations_MVC.Models.OpeningHours;
using Reliable_Reservations_MVC.Models.Table;
using Reliable_Reservations_MVC.Models.TimeSlot;

namespace Reliable_Reservations_MVC.Models.Reservation
{
    public class ReservationCreateViewModel
    {
        public int CustomerId { get; set; }

        public DateTime ReservationDate { get; set; }

        public int NumberOfGuests { get; set; }

        public List<int> TableNumbers { get; set; } = new List<int>();

        public string SpecialRequests { get; set; } = "None";
    }
}