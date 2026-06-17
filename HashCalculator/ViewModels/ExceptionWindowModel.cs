using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace HashCalculator
{
    internal class ExceptionWindowModel : NotifiableModel
    {
        private RelayCommand feedbackCmd;
        private int _handledExceptionCount = 0;
        private readonly BlockingCollection<string> _messageAndStackTraceQueue
            = [];
        private readonly StringBuilder _message = new StringBuilder();
        private readonly ExceptionWindow exceptionWindow;

        public string StackTraceMessage => this._message.ToString();

        internal ExceptionWindowModel(ExceptionWindow window)
        {
            this.exceptionWindow = window;
            this.UpdateExceptionMessageAndStackTrace();
        }

        internal void AddMessage(string message, string stackTrace)
        {
            if (++this._handledExceptionCount > 10)
            {
                if (!this._messageAndStackTraceQueue.IsAddingCompleted)
                {
                    this._messageAndStackTraceQueue.CompleteAdding();
                }
                return;
            }
            this._messageAndStackTraceQueue.Add(message);
            this._messageAndStackTraceQueue.Add(stackTrace);
        }

        private void UpdateExceptionMessageAndStackTrace()
        {
            Task.Run(async () =>
            {
                while (!this._messageAndStackTraceQueue.IsCompleted)
                {
                    string message = this._messageAndStackTraceQueue.Take();
                    this._message.AppendLine(message);
                    string stackTrace = this._messageAndStackTraceQueue.Take();
                    this._message.AppendLine(stackTrace);
                    MainWndViewModel.Synchronization.Invoke(() =>
                    {
                        this.NotifyPropertyChanged(nameof(this.StackTraceMessage));
                    });
                    Thread.Sleep(100);
                }
            });
        }

        private void FeedbackAction(object param)
        {
            Action<string> navigate = url =>
            {
                SHELL32.ShellExecuteW(MainWindow.WndHandle, "open", url, null, null, ShowCmd.SW_NORMAL);
            };
            if (param is string message)
            {
                CommonUtils.ClipboardSetText(this.StackTraceMessage);
                if (message.Contains("Gitee", StringComparison.OrdinalIgnoreCase))
                {
                    navigate(Settings.Current.IssueGitee);
                }
                else if (message.Contains("GitHub", StringComparison.OrdinalIgnoreCase))
                {
                    navigate(Settings.Current.IssueGitHub);
                }
            }
            this.exceptionWindow.Close();
        }

        public ICommand FeedbackCommand
        {
            get
            {
                this.feedbackCmd ??= new RelayCommand(this.FeedbackAction);
                return this.feedbackCmd;
            }
        }
    }
}
