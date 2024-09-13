using System.ComponentModel.DataAnnotations;

namespace Reliable_Reservations_MVC.Models.Customer
{
    public class CustomerViewModel
    {
        public int CustomerId { get; set; }

        public required string FirstName { get; set; }

        public required string LastName { get; set; }

        public required string PhoneNumber { get; set; }

        public required string Email { get; set; }
    }
}