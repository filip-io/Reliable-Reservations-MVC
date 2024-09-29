using Microsoft.AspNetCore.Mvc;
using Reliable_Reservations_MVC.Models.Customer;
using Reliable_Reservations_MVC.Models.Table;
using System.ComponentModel.DataAnnotations;

namespace Reliable_Reservations_MVC.Models.Reservation
{
    public class ReservationEditViewModel
    {
        [HiddenInput(DisplayValue = false)]
        public int ReservationId { get; set; }

        
        [HiddenInput(DisplayValue = false)]
        [Required]
        public CustomerViewModel Customer { get; set; }


        [Required(ErrorMessage = "Number of guests is required")]
        [Range(1, 15, ErrorMessage = "Number of guests must be between 1 and 15.")]
        public int NumberOfGuests { get; set; }


        [Required(ErrorMessage = "Date is required")]
        public DateTime ReservationDate { get; set; }


        [Required(ErrorMessage = "At least one table number is required")]
        public List<TableViewModel> Tables { get; set; } = new List<TableViewModel>();


        public string? SpecialRequests { get; set; } = "None";
    }
}
