using System;
using System.Windows;
using System.Windows.Threading;

namespace CalendarForReports.Views
{
    public partial class ToastWindow : Window
    {
        private readonly DispatcherTimer _timer;
        private static int _offsetY = 0;

        public ToastWindow(string title, string message, int autoCloseSeconds = 5)
        {
            InitializeComponent();

            TitleText.Text = title;
            MessageText.Text = message;

            var wa = SystemParameters.WorkArea;
            Left = wa.Right - Width - 16;
            Top = wa.Bottom - Height - 16 - _offsetY;
            _offsetY += (int)Height + 8;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(autoCloseSeconds) };
            _timer.Tick += (s, e) =>
            {
                _timer.Stop();
                Close();
            };
            _timer.Start();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _offsetY -= (int)Height + 8;
            if (_offsetY < 0) _offsetY = 0;
        }
    }
}