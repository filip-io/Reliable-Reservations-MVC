using System.ComponentModel.DataAnnotations;

namespace Reliable_Reservations_MVC.Models.OpeningHours
{
    public class OpeningHoursViewModel
    {
        public int OpeningHoursId { get; set; }

        public DayOfWeek DayOfWeek { get; set; }

        public TimeOnly OpenTime { get; set; }

        public TimeOnly CloseTime { get; set; }

        public bool IsClosed { get; set; }

        public virtual ICollection<SpecialOpeningHoursViewModel> SpecialOpeningHours { get; set; } = new List<SpecialOpeningHoursViewModel>();
    }

    public class SpecialOpeningHoursViewModel
    {
        public int SpecialOpeningHoursId { get; set; }

        public DateOnly Date { get; set; }

        public TimeOnly? OpenTime { get; set; }

        public TimeOnly? CloseTime { get; set; }

        public bool IsClosed { get; set; }

        public int OpeningHoursId { get; set; }
    }
}
