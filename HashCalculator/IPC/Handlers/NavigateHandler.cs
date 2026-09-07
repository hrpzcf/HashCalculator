using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HashCalculator.Views.Pages;
using HashCalculator.Views.Windows;

namespace HashCalculator.IPC.Handlers;

/// <summary>
/// 导航命令：把主窗口切换到指定页面。
/// Payload 为页面标识字符串（ANSI，见 <see cref="PageNames"/>）。
/// </summary>
internal sealed class NavigateHandler : IHandler
{
    public HandlerIdentity Identity => HandlerIdentity.NavigateTo;

    /// <summary>可供跨进程导航的页面标识</summary>
    public static class PageNames
    {
        public const string Home = "home";
        public const string Algorithms = "algos";
        public const string Settings = "settings";
    }

    private static readonly Dictionary<string, Type> pageMap = new()
    {
        [PageNames.Home] = typeof(HomePage),
        [PageNames.Algorithms] = typeof(AlgosPanelPage),
        [PageNames.Settings] = typeof(SettingsPanelPage),
    };

    public Task<HandlerResult> HandleAsync(ReadOnlyMemory<byte> payload, CancellationToken token)
    {
        string name = PayloadCodecs.Decode(payload);
        if (pageMap.TryGetValue(name, out Type pageType))
        {
            MainWindow.Current?.NavigateTo(pageType);
            return Task.FromResult(HandlerResult.OfStatus(HandlerStatus.OK));
        }
        return Task.FromResult(new HandlerResult { Status = HandlerStatus.BadPayload });
    }
}
