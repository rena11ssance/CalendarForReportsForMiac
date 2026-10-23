using System;
using System.Collections.Generic;
using System.Windows;
using CalendarForReports.Models;

namespace CalendarForReports
{
    public partial class TaskEditWindow : Window
    {
        public ReportTask Task { get; private set; }

        public class RecurrenceItem
        {
            public RecurrenceType Value { get; set; }
            public string Name { get; set; }
        }

        public TaskEditWindow(ReportTask existing = null, DateTime? defaultDate = null)
        {
            InitializeComponent();

            var recurrences = new List<RecurrenceItem>
            {
                new() { Value = RecurrenceType.None,      Name = "Разово" },
                new() { Value = RecurrenceType.Daily,     Name = "Ежедневно" },
                new() { Value = RecurrenceType.Weekly,    Name = "Еженедельно" },
                new() { Value = RecurrenceType.Monthly,   Name = "Ежемесячно" },
                new() { Value = RecurrenceType.Quarterly, Name = "Ежеквартально" },
                new() { Value = RecurrenceType.Yearly,    Name = "Ежегодно" }
            };

            RecurrenceBox.ItemsSource = recurrences;
            RecurrenceBox.DisplayMemberPath = "Name";
            RecurrenceBox.SelectedValuePath = "Value";
            RecurrenceBox.SelectedValue = RecurrenceType.None;

            if (existing != null)
            {
                Task = existing;
                TitleBox.Text = existing.Title;
                KeyBox.Text = existing.ReportKey;
                DescriptionBox.Text = existing.Description;
                StartDatePicker.SelectedDate = existing.StartDate;
                TimeBox.Text = existing.RunTime.ToString(@"hh\:mm");
                RecurrenceBox.SelectedValue = existing.Recurrence;
                IntervalBox.Text = existing.Interval.ToString();
                EnabledCheck.IsChecked = existing.IsEnabled;
            }
            else
            {
                Task = new ReportTask();
                StartDatePicker.SelectedDate = defaultDate?.Date ?? DateTime.Today;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleBox.Text))
            {
                MessageBox.Show("Введите название отчёта.");
                return;
            }

            if (!TimeSpan.TryParse(TimeBox.Text, out var time))
            {
                MessageBox.Show("Неверный формат времени. Используйте ЧЧ:ММ (например, 09:30).");
                return;
            }

            if (!int.TryParse(IntervalBox.Text, out var interval) || interval < 1)
            {
                MessageBox.Show("Интервал должен быть целым числом ≥ 1.");
                return;
            }

            Task.Title = TitleBox.Text.Trim();
            Task.ReportKey = KeyBox.Text.Trim();
            Task.Description = DescriptionBox.Text.Trim();
            Task.StartDate = (StartDatePicker.SelectedDate ?? DateTime.Today).Date;
            Task.RunTime = time;
            Task.Recurrence = (RecurrenceType)RecurrenceBox.SelectedValue;
            Task.Interval = interval;
            Task.IsEnabled = EnabledCheck.IsChecked == true;

            Task.NextRunUtc = Utils.RecurrenceCalculator.GetNextRun(Task, DateTime.UtcNow);

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}