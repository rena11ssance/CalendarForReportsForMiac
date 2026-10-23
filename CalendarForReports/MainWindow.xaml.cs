using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CalendarForReports.Data;
using CalendarForReports.Models;
using CalendarForReports.Services;
using CalendarForReports.Utils;
using CalendarForReports.Views;

namespace CalendarForReports
{
    public partial class MainWindow : Window
    {
        private List<ReportTask> _tasks = new();
        private readonly DispatcherTimer _schedulerTimer;

        private DateTime _currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        private DateTime _selectedDate = DateTime.Today;

        public ObservableCollection<DayViewModel> Days { get; } = new();

        private ReportTask _selectedTask;

        public MainWindow()
        {
            InitializeComponent();

            _tasks = JsonStorage.Load();
            DaysItemsControl.ItemsSource = Days;

            _schedulerTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _schedulerTimer.Tick += SchedulerTick;
            _schedulerTimer.Start();

            RebuildDays();
            RefreshInactiveList();
            SchedulerTick(null, null);
        }

        private void RebuildDays()
        {
            Days.Clear();

            var firstOfMonth = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
            int offset = ((int)firstOfMonth.DayOfWeek + 6) % 7;
            var start = firstOfMonth.AddDays(-offset);

            for (int i = 0; i < 42; i++)
            {
                var date = start.AddDays(i);
                var dayTasks = _tasks
                    .Where(t => t.IsEnabled && RecurrenceCalculator.OccursOn(t, date))
                    .Select(t => new TaskChipViewModel
                    {
                        Task = t,
                        Title = t.Title,
                        ChipColor = GetChipColor(t)
                    })
                    .ToList();

                Days.Add(new DayViewModel
                {
                    Date = date,
                    DayNumber = date.Day,
                    IsToday = date.Date == DateTime.Today,
                    IsCurrentMonth = date.Month == _currentMonth.Month,
                    IsSelected = date.Date == _selectedDate.Date,
                    Tasks = dayTasks
                });
            }

            MonthTitle.Text = _currentMonth.ToString("MMMM yyyy",
                System.Globalization.CultureInfo.GetCultureInfo("ru-RU"));
        }

        private static Brush GetChipColor(ReportTask t)
        {
            return t.Recurrence switch
            {
                RecurrenceType.None => new SolidColorBrush(Color.FromRgb(0x7F, 0x8C, 0x8D)),
                RecurrenceType.Daily => new SolidColorBrush(Color.FromRgb(0xE7, 0x4C, 0x3C)),
                RecurrenceType.Weekly => new SolidColorBrush(Color.FromRgb(0xF3, 0x9C, 0x12)),
                RecurrenceType.Monthly => new SolidColorBrush(Color.FromRgb(0x34, 0x98, 0xDB)),
                RecurrenceType.Quarterly => new SolidColorBrush(Color.FromRgb(0x9B, 0x59, 0xB6)),
                RecurrenceType.Yearly => new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60)),
                _ => Brushes.Gray
            };
        }

        private void PrevMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(-1);
            RebuildDays();
        }

        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(1);
            RebuildDays();
        }

        private void Today_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            _selectedDate = DateTime.Today;
            _selectedTask = null;
            UpdateButtonsState();
            RebuildDays();
        }

        private void DayCard_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var origin = e.OriginalSource as DependencyObject;
            if (IsInsideTaskChip(origin))
                return;

            if (sender is FrameworkElement border && border.Tag is DayViewModel day)
            {
                _selectedDate = day.Date;
                _selectedTask = null;
                UpdateButtonsState();
                RebuildDays();
            }
        }

        private static bool IsInsideTaskChip(DependencyObject origin)
        {
            while (origin != null)
            {
                if (origin is FrameworkElement fe && fe.Tag is TaskChipViewModel)
                    return true;
                origin = VisualTreeHelper.GetParent(origin);
            }
            return false;
        }

        private void TaskChip_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is TaskChipViewModel chip)
            {
                _selectedTask = chip.Task;
                _selectedDate = FindDayByTask(chip.Task) ?? _selectedDate;
                UpdateButtonsState();
                SelectedTaskInfo.Text = $"Выбрана: {chip.Task.Title}";

                // Если это двойной клик — открываем окно деталей
                if (e.ClickCount == 2)
                {
                    var wnd = new TaskDetailsWindow(chip.Task) { Owner = this };
                    wnd.ShowDialog();
                }

                e.Handled = true;
            }
        }

        private DateTime? FindDayByTask(ReportTask task)
        {
            foreach (var day in Days)
            {
                if (day.Tasks.Any(t => t.Task == task))
                    return day.Date;
            }
            return null;
        }

        private void UpdateButtonsState()
        {
            bool hasTask = _selectedTask != null;
            EditButton.IsEnabled = hasTask;
            DeactivateButton.IsEnabled = hasTask;
            DeleteOccurrenceButton.IsEnabled = hasTask;
            DeleteSeriesButton.IsEnabled = hasTask;

            if (!hasTask)
                SelectedTaskInfo.Text = "Задача не выбрана. Кликни по плашке задачи в календаре.";
        }

        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            var wnd = new TaskEditWindow(defaultDate: _selectedDate) { Owner = this };
            if (wnd.ShowDialog() == true)
            {
                _tasks.Add(wnd.Task);
                Persist();
                RebuildDays();
                RefreshInactiveList();
            }
        }

        private void EditSelected_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTask == null) return;

            var wnd = new TaskEditWindow(_selectedTask) { Owner = this };
            if (wnd.ShowDialog() == true)
            {
                Persist();
                RebuildDays();
                RefreshInactiveList();
            }
        }

        private void DeactivateTask_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTask == null) return;

            if (MessageBox.Show($"Сделать задачу «{_selectedTask.Title}» неактивной?",
                "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _selectedTask.IsEnabled = false;
                _selectedTask = null;
                Persist();
                UpdateButtonsState();
                RebuildDays();
                RefreshInactiveList();
            }
        }

        private void DeleteOccurrence_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTask == null) return;

            if (_selectedTask.Recurrence == RecurrenceType.None)
            {
                if (MessageBox.Show("Это разовая задача. Удалить её полностью?",
                    "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _tasks.Remove(_selectedTask);
                    _selectedTask = null;
                    Persist();
                    UpdateButtonsState();
                    RebuildDays();
                    RefreshInactiveList();
                }
                return;
            }

            if (MessageBox.Show(
                $"Удалить только вхождение «{_selectedTask.Title}» на {_selectedDate:dd.MM.yyyy}?",
                "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _selectedTask.Exceptions.Add(_selectedDate.Date);
                _selectedTask.NextRunUtc = RecurrenceCalculator.GetNextRun(_selectedTask, DateTime.UtcNow);
                _selectedTask = null;
                Persist();
                UpdateButtonsState();
                RebuildDays();
            }
        }

        private void DeleteSeries_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTask == null) return;

            if (MessageBox.Show($"Удалить задачу «{_selectedTask.Title}» полностью?",
                "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _tasks.Remove(_selectedTask);
                _selectedTask = null;
                Persist();
                UpdateButtonsState();
                RebuildDays();
                RefreshInactiveList();
            }
        }

        private void RefreshInactiveList()
        {
            var inactive = _tasks.Where(t => !t.IsEnabled).ToList();
            InactiveTasksListBox.ItemsSource = null;
            InactiveTasksListBox.ItemsSource = inactive;
            InactiveExpander.Header = $"Неактивные задачи ({inactive.Count})";
        }

        private void ActivateSelected_Click(object sender, RoutedEventArgs e)
        {
            if (InactiveTasksListBox.SelectedItem is not ReportTask selected) return;

            selected.IsEnabled = true;
            selected.NextRunUtc = RecurrenceCalculator.GetNextRun(selected, DateTime.UtcNow);
            Persist();
            RebuildDays();
            RefreshInactiveList();
        }

        private void DeleteSelectedPermanently_Click(object sender, RoutedEventArgs e)
        {
            if (InactiveTasksListBox.SelectedItem is not ReportTask selected) return;

            if (MessageBox.Show($"Удалить задачу «{selected.Title}» навсегда?",
                "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _tasks.Remove(selected);
                Persist();
                RefreshInactiveList();
            }
        }

        private void Persist() => JsonStorage.Save(_tasks);

        private void SchedulerTick(object sender, EventArgs e)
        {
            var nowUtc = DateTime.UtcNow;
            bool changed = false;

            var due = _tasks.Where(t => t.IsEnabled
                && t.NextRunUtc.HasValue
                && t.NextRunUtc.Value <= nowUtc).ToList();

            foreach (var task in due)
            {
                RunReport(task);
                task.NextRunUtc = RecurrenceCalculator.GetNextRun(task, nowUtc);
                changed = true;
            }

            foreach (var task in _tasks.Where(t => t.IsEnabled && t.NextRunUtc.HasValue))
            {
                var notifyThreshold = task.NextRunUtc.Value.AddMinutes(-15);
                if (nowUtc >= notifyThreshold && nowUtc < task.NextRunUtc.Value)
                {
                    if (task.LastNotifiedUtc == null || task.LastNotifiedUtc < notifyThreshold)
                    {
                        NotifyAboutUpcoming(task);
                        task.LastNotifiedUtc = nowUtc;
                        changed = true;
                    }
                }
            }

            if (changed) Persist();
        }

        private void RunReport(ReportTask task)
        {
            ToastService.Show("Запуск отчёта",
                $"{task.Title}\n(ключ: {task.ReportKey})");
        }

        private void NotifyAboutUpcoming(ReportTask task)
        {
            var remaining = task.NextRunUtc.Value - DateTime.UtcNow;
            ToastService.Show("Скоро запуск",
                $"{task.Title}\nчерез {remaining.Hours} ч {remaining.Minutes} мин");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _schedulerTimer.Stop();
            Persist();
        }
    }

    public class DayViewModel
    {
        public DateTime Date { get; set; }
        public int DayNumber { get; set; }
        public bool IsToday { get; set; }
        public bool IsCurrentMonth { get; set; }
        public bool IsSelected { get; set; }
        public List<TaskChipViewModel> Tasks { get; set; } = new();
    }

    public class TaskChipViewModel
    {
        public ReportTask Task { get; set; }
        public string Title { get; set; }
        public Brush ChipColor { get; set; }
    }
}