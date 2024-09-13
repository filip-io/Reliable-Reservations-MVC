using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Reliable_Reservations_MVC.Models.Customer
{
    public class CustomerCreateViewModel
    {
        [Required(ErrorMessage = "First name is required")]
        [DisplayName("First name")]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [DisplayName("Last name")]
        public string? LastName { get; set; }

        [Required]
        [Phone(ErrorMessage = "Not a valid phone number")]
        [DisplayName("Phone number")]
        public string? PhoneNumber { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "Not a valid email address")]
        [DisplayName("E-mail")]
        public string? Email { get; set; }
    }
}