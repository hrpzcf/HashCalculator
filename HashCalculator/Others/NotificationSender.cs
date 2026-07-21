using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace HashCalculator
{
    public static class NotificationSender
    {
        private static readonly ISnackbarService _snackbarService;

        static NotificationSender()
        {
            _snackbarService ??= App.GetRequiredService<ISnackbarService>();
        }

        public static void SnackbarError(string message)
        {
            _snackbarService.Show(
                "错误",
                message,
                ControlAppearance.Danger,
                new SymbolIcon(SymbolRegular.ErrorCircle20),
                TimeSpan.FromSeconds(Settings.Current.SnackbarNotificationTimeSpanSeconds)
                );
        }

        public static void SnackbarWarning(string message)
        {
            _snackbarService.Show(
                "提醒",
                message,
                ControlAppearance.Caution,
                new SymbolIcon(SymbolRegular.Warning20),
                TimeSpan.FromSeconds(Settings.Current.SnackbarNotificationTimeSpanSeconds)
                );
        }

        public static void SnackbarSuccess(string message)
        {
            _snackbarService.Show(
                "成功",
                message,
                ControlAppearance.Success,
                new SymbolIcon(SymbolRegular.CheckmarkCircle20),
                TimeSpan.FromSeconds(Settings.Current.SnackbarNotificationTimeSpanSeconds)
                );
        }

        public static void SnackbarInformation(string message)
        {
            _snackbarService.Show(
                "信息",
                message,
                ControlAppearance.Info,
                new SymbolIcon(SymbolRegular.Info20),
                TimeSpan.FromSeconds(Settings.Current.SnackbarNotificationTimeSpanSeconds)
                );
        }

        public static void SnackbarSecondary(string message)
        {
            _snackbarService.Show(
                "提示",
                message,
                ControlAppearance.Secondary,
                new SymbolIcon(SymbolRegular.Info20),
                TimeSpan.FromSeconds(Settings.Current.SnackbarNotificationTimeSpanSeconds)
                );
        }

        private static ContentDialogResult ShowDialogSync(ContentDialog dialog)
        {
            DispatcherFrame frame = new DispatcherFrame();
            Task<ContentDialogResult> showContentDialogTask = dialog.ShowAsync();
            showContentDialogTask.ContinueWith(
                task => frame.Continue = false,
                TaskScheduler.FromCurrentSynchronizationContext()
            );
            Dispatcher.PushFrame(frame);
            return showContentDialogTask.Result;
        }

        public static ContentDialogResult ShowMessageBox(
            Window owner,
            string title,
            string content,
            string primaryButtonText = "",
            string secondaryButtonText = "",
            string closeButtonText = "",
            ContentDialogButton defaultButton = ContentDialogButton.Close)
        {
            if (string.IsNullOrEmpty(closeButtonText))
            {
                closeButtonText = "确定";
            }
            // 选窗口：优先 owner；否则当前激活窗口；再否则应用主窗口。
            Window window = owner
                ?? Application.Current.Windows.OfType<Window>().First(w => w.IsActive)
                ?? Application.Current.MainWindow;
            ContentDialogHost contentDialogHost = ContentDialogHost.GetForWindow(window)
                ?? ContentDialogHost.GetForWindow(Application.Current.MainWindow);
            ContentDialog toBeShownContentDialogInstance = new ContentDialog(contentDialogHost)
            {
                Title = title,
                Content = content,
                CloseButtonText = closeButtonText,
                PrimaryButtonText = primaryButtonText,
                SecondaryButtonText = secondaryButtonText,
                DefaultButton = defaultButton,
            };

            // 不能用 .GetAwaiter().GetResult()：ContentDialog.ShowAsync
            // 内部用了 RunContinuationsAsynchronously，同步阻塞会卡死。
            // 改用 DispatcherFrame 跑嵌套消息循环：调用栈同步阻塞、UI 仍能响应、
            // 同步返回结果。这是 WPF Window.ShowDialog() 的实现原理。
            return ShowDialogSync(toBeShownContentDialogInstance);
        }

        public static ContentDialogResult ShowMessageBox(string title, string content)
        {
            return ShowMessageBox(null, title, content);
        }
    }
}
