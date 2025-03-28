﻿using System;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace HashCalculator
{
    public partial class App : Application
    {
        private bool _isSessionEndingHandled = false;

        internal static readonly Assembly Executing = Assembly.GetExecutingAssembly();

        private void StartupHandler(object sender, StartupEventArgs e)
        {
            Settings.LoadSettings();
            Initializer.ParseArgsForShell(e.Args);
            Initializer.PushArgs(e.Args);
        }

        private void ApplicationFinalization()
        {
            Settings.Current.RunInMultiInstMode = Initializer.RunMultiMode;
            Settings.SaveSettings();
        }

        private void ExitHandler(object sender, ExitEventArgs e)
        {
            if (!this._isSessionEndingHandled)
            {
                this.ApplicationFinalization();
            }
        }

        private void ApplicationSessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            this.ApplicationFinalization();
            this._isSessionEndingHandled = true;
        }

        private void ExceptionHandler(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                $"遇到异常即将退出：\n{e.Exception.Message}\n\n" +
                $"问题反馈：软件设置 - 关于软件 - 问题反馈。", "错误");
            Environment.Exit(3);
        }
    }
}
