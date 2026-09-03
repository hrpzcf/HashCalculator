using HashCalculator.IPC.Handlers;

namespace HashCalculator.IPC;

/// <summary>
/// 跨进程命令的单例宿主。
/// Executor 与 Server 生命周期都应等于进程存活期，且各实例只应有一个。
/// 窗口可能被关闭到托盘再重开，因此用 Server 判空保证只启动一次，不能放在
/// 窗口的关闭事件里随窗口销毁。
/// </summary>
internal static class IPCHost
{
    private static CommandServer server;

    /// <summary>
    /// 启动本实例的管道监听，只应执行一次
    /// </summary>
    public static void Start()
    {
        if (server is null)
        {
            server = new CommandServer(new CommandExecutor()
                .Register(new ActivateAppHandler())
                .Register(new NavigateHandler())
                .Register(new GetMultiModeHandler())
                .Register(new SetMultiModeHandler())
                .Register(new ParseArgumentsHandler()));
            server.Start();
        }
    }
}
