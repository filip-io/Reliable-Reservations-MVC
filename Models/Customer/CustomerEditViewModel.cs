using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Reliable_Reservations_MVC.Models.Customer
{
    public class CustomerEditViewModel
    {
        [HiddenInput(DisplayValue = false)]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [DisplayName("First name")]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [DisplayName("Last name")]
        public string? LastName { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Not a valid phone number")]
        [DisplayName("Phone number")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "E-mail is required")]
        [EmailAddress(ErrorMessage = "Not a valid email address")]
        public string? Email { get; set; }
    }
}