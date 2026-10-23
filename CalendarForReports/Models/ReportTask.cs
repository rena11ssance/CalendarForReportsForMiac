using System;
using System.Collections.Generic;

namespace CalendarForReports.Models
{
    public enum RecurrenceType { None, Daily, Weekly, Monthly, Quarterly, Yearly }

    public class ReportTask
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = "";
        public string ReportKey { get; set; } = "";
        public string Description { get; set; } = "";

        public DateTime StartDate { get; set; } = DateTime.Today;
        public RecurrenceType Recurrence { get; set; } = RecurrenceType.None;
        public int Interval { get; set; } = 1;

        public TimeSpan RunTime { get; set; } = new TimeSpan(9, 0, 0);

        public bool IsEnabled { get; set; } = true;
        public DateTime? NextRunUtc { get; set; }
        public DateTime? LastNotifiedUtc { get; set; }

        public List<DateTime> Exceptions { get; set; } = new List<DateTime>();

        public string RecurrenceName => Recurrence switch
        {
            RecurrenceType.None => "Разово",
            RecurrenceType.Daily => "Ежедневно",
            RecurrenceType.Weekly => "Еженедельно",
            RecurrenceType.Monthly => "Ежемесячно",
            RecurrenceType.Quarterly => "Ежеквартально",
            RecurrenceType.Yearly => "Ежегодно",
            _ => "Неизвестно"
        };

        public string Display => $"{Title} ({RecurrenceName}, {RunTime:hh\\:mm})";
    }
}