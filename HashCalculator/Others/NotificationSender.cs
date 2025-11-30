using HandyControl.Controls;

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
                Growl.ErrorGlobal(message);
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
                Growl.WarningGlobal(message);
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
                Growl.SuccessGlobal(message);
            }
        }
    }
}
