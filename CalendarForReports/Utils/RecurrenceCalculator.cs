using System;
using CalendarForReports.Models;

namespace CalendarForReports.Utils
{
    public static class RecurrenceCalculator
    {
        public static DateTime? GetNextRun(ReportTask task, DateTime fromUtc)
        {
            if (!task.IsEnabled) return null;

            if (task.Recurrence == RecurrenceType.None)
            {
                var once = (task.StartDate.Date + task.RunTime).ToUniversalTime();
                if (once < fromUtc) return null;
                if (task.Exceptions.Contains(task.StartDate.Date)) return null;
                return once;
            }

            var candidate = task.StartDate.Date + task.RunTime;

            while (candidate.ToUniversalTime() <= fromUtc)
            {
                candidate = AddInterval(candidate, task.Recurrence, task.Interval);
                if (candidate == DateTime.MaxValue) return null;
            }

            while (task.Exceptions.Contains(candidate.Date))
            {
                candidate = AddInterval(candidate, task.Recurrence, task.Interval);
                if (candidate == DateTime.MaxValue) return null;
            }

            return candidate.ToUniversalTime();
        }

        private static DateTime AddInterval(DateTime dt, RecurrenceType type, int n)
        {
            switch (type)
            {
                case RecurrenceType.Daily: return dt.AddDays(n);
                case RecurrenceType.Weekly: return dt.AddDays(7 * n);
                case RecurrenceType.Monthly: return SafeAddMonths(dt, n);
                case RecurrenceType.Quarterly: return SafeAddMonths(dt, 3 * n);
                case RecurrenceType.Yearly: return SafeAddYears(dt, n);
                default: return DateTime.MaxValue;
            }
        }

        private static DateTime SafeAddMonths(DateTime dt, int months)
        {
            var targetMonth = dt.AddMonths(months);
            if (dt.Day == DateTime.DaysInMonth(dt.Year, dt.Month))
                return new DateTime(targetMonth.Year, targetMonth.Month,
                                    DateTime.DaysInMonth(targetMonth.Year, targetMonth.Month))
                       + dt.TimeOfDay;
            return targetMonth;
        }

        private static DateTime SafeAddYears(DateTime dt, int years)
        {
            var targetYear = dt.AddYears(years);
            if (dt.Month == 2 && dt.Day == 29 && (targetYear.Month != 2 || targetYear.Day != 29))
                return new DateTime(targetYear.Year, 2, 28) + dt.TimeOfDay;
            return targetYear;
        }

        public static bool OccursOn(ReportTask task, DateTime day)
        {
            if (!task.IsEnabled) return false;

            var start = task.StartDate.Date;
            var target = day.Date;

            if (target < start) return false;
            if (task.Exceptions.Contains(target)) return false;

            switch (task.Recurrence)
            {
                case RecurrenceType.None:
                    return target == start;

                case RecurrenceType.Daily:
                    return (target - start).Days % task.Interval == 0;

                case RecurrenceType.Weekly:
                    if (target.DayOfWeek != start.DayOfWeek) return false;
                    return ((target - start).Days / 7) % task.Interval == 0;

                case RecurrenceType.Monthly:
                    {
                        int months = (target.Year - start.Year) * 12 + (target.Month - start.Month);
                        if (months < 0 || months % task.Interval != 0) return false;
                        return IsSameDayOfMonth(start, target);
                    }

                case RecurrenceType.Quarterly:
                    {
                        int months = (target.Year - start.Year) * 12 + (target.Month - start.Month);
                        if (months < 0 || months % (3 * task.Interval) != 0) return false;
                        return IsSameDayOfMonth(start, target);
                    }

                case RecurrenceType.Yearly:
                    {
                        if (start.Month != target.Month) return false;
                        int years = target.Year - start.Year;
                        if (years < 0 || years % task.Interval != 0) return false;
                        return IsSameDayOfMonth(start, target);
                    }
            }
            return false;
        }

        private static bool IsSameDayOfMonth(DateTime start, DateTime target)
        {
            int startLast = DateTime.DaysInMonth(start.Year, start.Month);
            int targetLast = DateTime.DaysInMonth(target.Year, target.Month);

            if (start.Day == startLast)
                return target.Day == targetLast;
            return target.Day == start.Day;
        }
    }
}