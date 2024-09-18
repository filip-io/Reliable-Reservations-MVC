using Reliable_Reservations_MVC.Models.Table;
using System.ComponentModel.DataAnnotations;

namespace Reliable_Reservations_MVC.Models.TimeSlot
{
    public class TimeSlotViewModel
    {
        public int TimeSlotId { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public TableViewModel? TableViewModel { get; set; }

        public int TableId { get; set; }

        public int? ReservationId { get; set; }
    }
}