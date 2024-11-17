using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using CommandLine;

namespace HashCalculator
{
    public class Loading : Application
    {
        private static readonly string[] assemblyNames = new string[]
        {
            "System.Buffers",
            "System.Memory",
            "System.Numerics.Vectors",
            "System.Runtime.CompilerServices.Unsafe",
            "CommandLine",
            "HandyControl",
            "Microsoft.Bcl.HashCode",
            "Microsoft.WindowsAPICodePack",
            "Microsoft.WindowsAPICodePack.Shell",
            "Newtonsoft.Json",
            "XamlAnimatedGif",
        };
        internal static readonly Assembly Executing = Assembly.GetExecutingAssembly();

        [STAThread()]
        public static void Main(string[] args)
        {
            Settings.StartupArgs = args;
            Settings.SetProcessEnvVar();
            Loading application = new Loading();
            application.Exit += ApplicationExit;
            application.Startup += ApplicationStartup;
            application.DispatcherUnhandledException += application.ExceptionHandler;
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
            application.StartupUri = new Uri("Views/MainWindow.xaml", UriKind.Relative);
            application.Run();
        }

        private static void ApplicationExit(object sender, ExitEventArgs e)
        {
            Settings.Current.RunInMultiInstMode = Initializer.RunMultiMode;
            Settings.SaveSettings();
        }

        private static void ApplicationStartup(object sender, StartupEventArgs e)
        {
            Settings.LoadSettings();
            Parser.Default.ParseArguments<VerifyHash, ComputeHash, ShellInstallation>(
                Settings.StartupArgs).WithParsed<ShellInstallation>(option =>
                {
                    if (option.Install)
                    {
                        Exception exception = ShellExtHelper.InstallShellExtension();
                        if (exception != null)
                        {
                            if (!option.InstallSilently)
                            {
                                MessageBox.Show(exception.Message, "错误",
                                    MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK,
                                    MessageBoxOptions.ServiceNotification);
                            }
                        }
                        else
                        {
                            if (!File.Exists(Settings.ConfigInfo.MenuConfigFile))
                            {
                                string message = new ShellMenuEditorModel(null).SaveMenuListToJsonFile();
                                if (!string.IsNullOrEmpty(message) && !option.InstallSilently)
                                {
                                    MessageBox.Show($"扩展模块配置文件创建失败，快捷菜单将不显示，原因：{message}",
                                        "错误", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK,
                                        MessageBoxOptions.ServiceNotification);
                                }
                            }
                        }
                        Environment.Exit(0);
                    }
                    else if (option.Uninstall)
                    {
                        Exception exception = ShellExtHelper.UninstallShellExtension();
                        if (exception != null && !option.InstallSilently)
                        {
                            MessageBox.Show(exception.Message, "错误",
                                MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK,
                                MessageBoxOptions.ServiceNotification);
                        }
                        Environment.Exit(0);
                    }
                });
            Settings.ExtractEmbeddedAlgoDll(false);
            Initializer.PushArgs(Settings.StartupArgs);
        }

        private static Assembly AssemblyResolve(object sender, ResolveEventArgs arg)
        {
            try
            {
                string assemblyName = new AssemblyName(arg.Name).Name;
                if (assemblyNames.Contains(assemblyName))
                {
                    using (Stream stream = Executing.GetManifestResourceStream(
                        string.Format("HashCalculator.Assembly.{0}.dll", assemblyName)))
                    {
                        if (stream != null)
                        {
                            byte[] assemblyBytes = new byte[stream.Length];
                            if (stream.Read(assemblyBytes, 0, assemblyBytes.Length) == assemblyBytes.Length)
                            {
                                return Assembly.Load(assemblyBytes);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            return null;
        }

        private void ExceptionHandler(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"遇到异常即将退出：\n{e.Exception.Message}\n\n问题反馈：软件设置 - 关于软件 - 问题反馈。", "错误");
            Environment.Exit(3);
        }
    }
}
