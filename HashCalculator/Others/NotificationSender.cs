using System;
using System.Windows;
using HashCalculator.Views.Windows;
using Wpfctrls = Wpf.Ui.Controls;
using Wpfui = Wpf.Ui;

namespace HashCalculator
{
    internal static class NotificationSender
    {
        static NotificationSender()
        {
            SnackbarService ??= new Wpfui.SnackbarService();
        }

        public static void SnackbarError(string message)
        {
            SnackbarService.Show(
                "错误",
                message,
                Wpfctrls.ControlAppearance.Danger,
                new Wpfctrls.SymbolIcon(Wpfctrls.SymbolRegular.ErrorCircle20),
                TimeSpan.FromSeconds(Settings.Current.SnackbarNotificationTimeSpanSeconds)
                );
        }

        public static void SnackbarWarning(string message)
        {
            SnackbarService.Show(
                "警告",
                message,
                Wpfctrls.ControlAppearance.Caution,
                new Wpfctrls.SymbolIcon(Wpfctrls.SymbolRegular.Warning20),
                TimeSpan.FromSeconds(Settings.Current.SnackbarNotificationTimeSpanSeconds)
                );
        }

        public static void SnackbarSuccess(string message)
        {
            SnackbarService.Show(
                "成功",
                message,
                Wpfctrls.ControlAppearance.Primary,
                new Wpfctrls.SymbolIcon(Wpfctrls.SymbolRegular.CheckmarkCircle20),
                TimeSpan.FromSeconds(Settings.Current.SnackbarNotificationTimeSpanSeconds)
                );
        }

        public static void SnackbarInformation(string message)
        {
            SnackbarService.Show(
                "提示",
                message,
                Wpfctrls.ControlAppearance.Secondary,
                new Wpfctrls.SymbolIcon(Wpfctrls.SymbolRegular.Info20),
                TimeSpan.FromSeconds(Settings.Current.SnackbarNotificationTimeSpanSeconds)
                );
        }

        public static Wpfctrls.MessageBoxResult ShowMessageBox(
            Window owner,
            string title,
            string content,
            string closeButtonText = null,
            string primaryButtonText = null,
            string secondaryButtonText = null)
        {
            owner ??= MainWindow.Current;
            bool isCloseButtonEnabled = !string.IsNullOrEmpty(closeButtonText);
            bool isPrimaryButtonEnabled = !string.IsNullOrEmpty(primaryButtonText);
            bool isSecondaryButtonEnabled = !string.IsNullOrEmpty(secondaryButtonText);
            if (!isPrimaryButtonEnabled && !isSecondaryButtonEnabled)
            {
                isCloseButtonEnabled = true;
                if (string.IsNullOrEmpty(closeButtonText))
                {
                    closeButtonText = "确定";
                }
            }
            return new Wpfctrls.MessageBox()
            {
                Owner = owner,
                Title = title,
                Content = content,
                IsCloseButtonEnabled = isCloseButtonEnabled,
                CloseButtonText = closeButtonText,
                IsPrimaryButtonEnabled = isPrimaryButtonEnabled,
                PrimaryButtonText = primaryButtonText,
                IsSecondaryButtonEnabled = isSecondaryButtonEnabled,
                SecondaryButtonText = secondaryButtonText,
            }.ShowDialogAsync().GetAwaiter().GetResult();
        }

        public static Wpfctrls.MessageBoxResult ShowMessageBox(
            string title,
            string content,
            string closeButtonText = null,
            string primaryButtonText = null,
            string secondaryButtonText = null)
        {
            return ShowMessageBox(null, title, content, closeButtonText, primaryButtonText, secondaryButtonText);
        }

        public static Wpfui.SnackbarService SnackbarService { get; }
    }
}
