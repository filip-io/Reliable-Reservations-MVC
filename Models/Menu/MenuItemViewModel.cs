using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Reliable_Reservations_MVC.Models.Menu
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Category
    {
        Appetizer,
        Starter,
        MainCourse,
        Dessert,
        Beverage,
        SideDish,
        Soup,
        Salad,
        Special,
        Kids,
        Vegetarian
    }

    public class MenuItemViewModel
    { 
        public int MenuItemId { get; set; }

        public required string Name { get; set; }


        public required string Description { get; set; }


        public required decimal Price { get; set; }


        public required Category Category { get; set; }


        public required bool AvailabilityStatus { get; set; }


        public required bool IsPopular { get; set; }


        public required DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}