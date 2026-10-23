using System.Windows;
using CalendarForReports.Models;

namespace CalendarForReports.Views
{
    public partial class TaskDetailsWindow : Window
    {
        // Конструктор, принимающий задачу (тот самый, который ожидает MainWindow)
        public TaskDetailsWindow(ReportTask task)
        {
            InitializeComponent();

            TitleText.Text = task.Title;
            KeyText.Text = $"Ключ: {task.ReportKey}";
            RecurrenceText.Text = $"Периодичность: {task.RecurrenceName} (интервал: {task.Interval})";
            TimeText.Text = $"Время запуска: {task.RunTime:hh\\:mm}";

            if (task.NextRunUtc.HasValue)
            {
                NextRunText.Text = $"Следующий запуск: {task.NextRunUtc.Value.ToLocalTime():dd.MM.yyyy HH:mm}";
            }
            else
            {
                NextRunText.Text = "Следующий запуск: не запланирован";
            }

            DescriptionText.Text = string.IsNullOrWhiteSpace(task.Description)
                ? "Описание отсутствует."
                : task.Description;
        }

        // Метод, который ищет XAML при нажатии на кнопку "Закрыть"
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}