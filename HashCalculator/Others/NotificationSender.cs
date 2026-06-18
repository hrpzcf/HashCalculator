using System;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace HashCalculator
{
    internal static class NotificationSender
    {
        static NotificationSender()
        {
            SnackbarServiceInst ??= new SnackbarService();
        }

        public static void Error(string message)
        {
            SnackbarServiceInst.Show(
                "错误",
                message,
                ControlAppearance.Danger,
                new SymbolIcon(SymbolRegular.ErrorCircle20),
                TimeSpan.FromSeconds(3)
                );
        }

        public static void Warning(string message)
        {
            SnackbarServiceInst.Show(
                "警告",
                message,
                ControlAppearance.Caution,
                new SymbolIcon(SymbolRegular.Warning20),
                TimeSpan.FromSeconds(3)
                );
        }

        public static void Success(string message)
        {
            SnackbarServiceInst.Show(
                "成功",
                message,
                ControlAppearance.Primary,
                new SymbolIcon(SymbolRegular.CheckmarkCircle20),
                TimeSpan.FromSeconds(3)
                );
        }

        public static SnackbarService SnackbarServiceInst { get; }
    }
}
