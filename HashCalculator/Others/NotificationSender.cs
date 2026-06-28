using System;
using System.Windows;
using HashCalculator.Views.Windows;
using Wpfui = Wpf.Ui;
using Wpfuictrls = Wpf.Ui.Controls;

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
                Wpfuictrls.ControlAppearance.Danger,
                new Wpfuictrls.SymbolIcon(Wpfuictrls.SymbolRegular.ErrorCircle20),
                TimeSpan.FromSeconds(3)
                );
        }

        public static void SnackbarWarning(string message)
        {
            SnackbarService.Show(
                "警告",
                message,
                Wpfuictrls.ControlAppearance.Caution,
                new Wpfuictrls.SymbolIcon(Wpfuictrls.SymbolRegular.Warning20),
                TimeSpan.FromSeconds(3)
                );
        }

        public static void SnackbarSuccess(string message)
        {
            SnackbarService.Show(
                "成功",
                message,
                Wpfuictrls.ControlAppearance.Primary,
                new Wpfuictrls.SymbolIcon(Wpfuictrls.SymbolRegular.CheckmarkCircle20),
                TimeSpan.FromSeconds(3)
                );
        }

        public static void SnackbarInformation(string message)
        {
            SnackbarService.Show(
                "提示",
                message,
                Wpfuictrls.ControlAppearance.Secondary,
                new Wpfuictrls.SymbolIcon(Wpfuictrls.SymbolRegular.Info20),
                TimeSpan.FromSeconds(3)
                );
        }

        public static Wpfuictrls.MessageBoxResult ShowMessageBox(
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
            return new Wpfuictrls.MessageBox()
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

        public static Wpfuictrls.MessageBoxResult ShowMessageBox(
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
