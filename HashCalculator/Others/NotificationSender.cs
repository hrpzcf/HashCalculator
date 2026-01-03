using HandyControl.Controls;
using HandyControl.Data;

namespace HashCalculator
{
    internal static class NotificationSender
    {
        public static void GrowlError(string message)
        {
            if (Settings.Current.SendNonGlobalGrowlNotifications)
            {
                Growl.Error(message);
            }
            else
            {
                NotificationHandle?.Close();
                NotificationHandle = Notification.Show(
                    new DesktopNotification(NotificationType.Error, message),
                    ShowAnimation.HorizontalMove,
                    false);
            }
        }

        public static void GrowlWarning(string message)
        {
            if (Settings.Current.SendNonGlobalGrowlNotifications)
            {
                Growl.Warning(message);
            }
            else
            {
                NotificationHandle?.Close();
                NotificationHandle = Notification.Show(
                    new DesktopNotification(NotificationType.Warning, message),
                    ShowAnimation.HorizontalMove,
                    false);
            }
        }

        public static void GrowlSuccess(string message)
        {
            if (Settings.Current.SendNonGlobalGrowlNotifications)
            {
                Growl.Success(message);
            }
            else
            {
                NotificationHandle?.Close();
                NotificationHandle = Notification.Show(
                    new DesktopNotification(NotificationType.Success, message),
                    ShowAnimation.HorizontalMove,
                    false);
            }
        }

        public static Notification NotificationHandle { get; private set; }
    }
}
