using Newtonsoft.Json.Converters;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Reliable_Reservations_MVC.Models.Menu
{
    public class MenuItemCreateViewModel
    {
        [Required(ErrorMessage = "Menu item name is required")]
        [DisplayName("Name")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "Menu item description is required")]
        [DisplayName("Description")]
        public required string Description { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [DisplayName("Price")]
        public required decimal Price { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [DisplayName("Category")]
        public required Category Category { get; set; }

        [Required(ErrorMessage = "Availability status is required")]
        [DisplayName("Availability")]
        public required bool AvailabilityStatus { get; set; } = true;

        [Required(ErrorMessage = "Popular status is required")]
        [DisplayName("Popular?")]
        public required bool IsPopular { get; set; }
    }
}