using System.Windows;
using System.Windows.Controls;

namespace HashCalculator
{
    public partial class DesktopNotification : Border
    {
        public DesktopNotification(NotificationType type, string message)
        {
            this.DataContext = new DesktopNotificationModel()
            {
                Type = type,
                Message = message,
            };
            this.InitializeComponent();
        }

        private void ButtonClose_OnClick(object sender, RoutedEventArgs e)
        {
            NotificationSender.NotificationHandle?.Close();
        }
    }
}
