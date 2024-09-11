using System.ComponentModel.DataAnnotations;

namespace Reliable_Reservations_MVC.Models
{
    public class CustomerCreateViewModel
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        [Phone(ErrorMessage = "Not a valid phone number.")]
        public string PhoneNumber { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "Not a valid email address.")]
        public string Email { get; set; }
    }
}