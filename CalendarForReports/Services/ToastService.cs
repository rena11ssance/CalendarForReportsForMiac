using CalendarForReports.Views;

namespace CalendarForReports.Services
{
    public static class ToastService
    {
        public static void Show(string title, string message)
        {
            var toast = new ToastWindow(title, message);
            toast.Show();
        }
    }
}