using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace HashCalculator;

/// <summary>
/// 管理主题覆盖字典的替换，在浅色/深色主题切换时同步替换对应的覆盖画刷。
/// 监听 <see cref="ApplicationThemeManager.Changed"/> 事件，无需手动调用。
/// </summary>
internal static class ThemeOverridesManager
{
    private const string DarkOverridesUri =
        "pack://application:,,,/HashCalculator;component/Themes/DarkOverrides.xaml";
    private const string LightOverridesUri =
        "pack://application:,,,/HashCalculator;component/Themes/LightOverrides.xaml";

    private static bool _initialized = false;

    /// <summary>
    /// 注册主题变更事件。需在 App 启动早期调用一次。
    /// </summary>
    public static void Initialize()
    {
        if (!_initialized)
        {
            _initialized = true;
            ApplicationThemeManager.Changed += OnThemeChanged;
            // 启动时立即加载当前主题对应的覆盖字典，避免依赖后续的 Changed 事件
            // （该事件只在主题切换时触发，启动时可能不触发）。
            // 注意：Initialize() 在 Settings.LoadSettings() 之后调用，后者反序列化时会触发
            // SelectedApplicationThemeIndex setter -> ApplicationThemeManager.Apply，因此
            // GetAppTheme() 此时必然返回已应用的主题（Dark/Light），不会是 Unknown。
            OnThemeChanged(ApplicationThemeManager.GetAppTheme(), Colors.Transparent);
        }
    }

    /// <summary>
    /// 根据主题替换 Application.Resources.MergedDictionaries 中的覆盖字典。
    /// </summary>
    private static void OnThemeChanged(ApplicationTheme theme, Color accent)
    {
        string resourceUri = theme switch
        {
            ApplicationTheme.Dark => DarkOverridesUri,
            ApplicationTheme.Light => LightOverridesUri,
            _ => null // HighContrast 不应用覆盖
        };
        Collection<ResourceDictionary> dictionaries =
            Application.Current.Resources.MergedDictionaries;
        // 移除现有的覆盖字典
        for (int i = dictionaries.Count - 1; i >= 0; i--)
        {
            if (dictionaries[i]?.Source?.ToString() is string source &&
                (source.Contains("LightOverrides") || source.Contains("DarkOverrides")))
            {
                dictionaries.RemoveAt(i);
            }
        }
        // 添加对应主题的覆盖字典（Light/Dark 时才加）
        if (resourceUri != null)
        {
            dictionaries.Add(new ResourceDictionary
            {
                Source = new Uri(resourceUri, UriKind.Absolute)
            });
        }
    }
}
