using System;
using System.Windows;
using HashCalculator.Views.Windows;
using Wpfui = Wpf.Ui;
using WpfuiCtrls = Wpf.Ui.Controls;

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
                WpfuiCtrls.ControlAppearance.Danger,
                new WpfuiCtrls.SymbolIcon(WpfuiCtrls.SymbolRegular.ErrorCircle20),
                TimeSpan.FromSeconds(3)
                );
        }

        public static void SnackbarWarning(string message)
        {
            SnackbarService.Show(
                "警告",
                message,
                WpfuiCtrls.ControlAppearance.Caution,
                new WpfuiCtrls.SymbolIcon(WpfuiCtrls.SymbolRegular.Warning20),
                TimeSpan.FromSeconds(3)
                );
        }

        public static void SnackbarSuccess(string message)
        {
            SnackbarService.Show(
                "成功",
                message,
                WpfuiCtrls.ControlAppearance.Primary,
                new WpfuiCtrls.SymbolIcon(WpfuiCtrls.SymbolRegular.CheckmarkCircle20),
                TimeSpan.FromSeconds(3)
                );
        }

        public static void SnackbarInformation(string message)
        {
            SnackbarService.Show(
                "提示",
                message,
                WpfuiCtrls.ControlAppearance.Secondary,
                new WpfuiCtrls.SymbolIcon(WpfuiCtrls.SymbolRegular.Info20),
                TimeSpan.FromSeconds(3)
                );
        }

        public static WpfuiCtrls.MessageBoxResult ShowMessageBox(
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
            return new WpfuiCtrls.MessageBox()
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

        public static WpfuiCtrls.MessageBoxResult ShowMessageBox(
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
