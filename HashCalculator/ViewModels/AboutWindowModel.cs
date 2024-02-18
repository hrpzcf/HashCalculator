using System.Windows.Input;

namespace HashCalculator
{
    internal class AboutWindowModel
    {
        private RelayCommand openWebSiteCmd;

        public string SrcGitee => "https://gitee.com/hrpzcf/HashCalculator";

        public string SrcGitHub => "https://github.com/hrpzcf/HashCalculator";

        public string IssueGitee => "https://gitee.com/hrpzcf/HashCalculator/issues";

        public string IssueGitHub => "https://github.com/hrpzcf/HashCalculator/issues";

        public string WikiGitee => "https://gitee.com/hrpzcf/HashCalculator/wikis/Home";

        public string WikiGitHub => "https://github.com/hrpzcf/HashCalculator/wiki";

        public string Title => Info.Title;

        public string Author => Info.Author;

        public string Ver => Info.Ver;

        public string Published => Info.Published;

        private void OpenWebSiteAction(object param)
        {
            if (param is string url)
            {
                SHELL32.ShellExecuteW(MainWindow.WndHandle, "open", url, null, null, ShowCmd.SW_NORMAL);
            }
        }

        public ICommand OpenWebSiteCmd
        {
            get
            {
                if (this.openWebSiteCmd == null)
                {
                    this.openWebSiteCmd = new RelayCommand(this.OpenWebSiteAction);
                }
                return this.openWebSiteCmd;
            }
        }

        public GenericItemModel[] OpenSourceProjects { get; } = new GenericItemModel[]
        {
            new GenericItemModel(
                "BLAKE2",
                "https://github.com/BLAKE2/BLAKE2",
                "提供 BLAKE2 系列哈希算法的实现"),
            new GenericItemModel(
                "BLAKE3",
                "https://github.com/BLAKE3-team/BLAKE3",
                "提供 BLAKE3 系列哈希算法的实现"),
            new GenericItemModel(
                "CRC32",
                "https://github.com/stbrumme/crc32",
                "提供 CRC32 哈希算法的实现"),
            new GenericItemModel(
                "GmSSL",
                "https://github.com/guanzhi/GmSSL",
                "提供 SM3 哈希算法的实现"),
            new GenericItemModel(
                "OpenHashTab",
                "https://github.com/namazso/OpenHashTab",
                "提供 CRC64 哈希算法的实现"),
            new GenericItemModel(
                "QuickXorHash",
                "https://github.com/namazso/QuickXorHash",
                "提供 QuickXor 哈希算法的实现"),
            new GenericItemModel(
                "RHash",
                "https://github.com/rhash/RHash",
                "提供 eD2k/Has160/MD4/RipeMD160/SHA224/Whirlpool 算法的实现"),
            new GenericItemModel(
                "Streebog",
                "https://github.com/adegtyarev/streebog",
                "提供 Streebog 系列哈希算法的实现"),
            new GenericItemModel(
                "XKCP",
                "https://github.com/XKCP/XKCP",
                "提供 SHA3 系列哈希算法的实现"),
            new GenericItemModel(
                "xxHash",
                "https://github.com/Cyan4973/xxHash",
                "提供 XXH 系列哈希算法的实现"),
            new GenericItemModel(
                "CommandLine",
                "https://github.com/commandlineparser/commandline",
                "用于解析命令行参数"),
            new GenericItemModel(
                "Newtonsoft.Json",
                "https://www.newtonsoft.com/json",
                "用于读取和保存本软件的相关配置文件"),
            new GenericItemModel(
                "tiny-json",
                "https://github.com/rafagafe/tiny-json",
                "用于读取和保存外壳扩展的相关配置文件"),
            new GenericItemModel(
                "WindowsAPICodePack",
                "https://github.com/aybe/Windows-API-Code-Pack-1.1",
                "用于调用系统接口打开文件/文件夹选择对话框"),
            new GenericItemModel(
                "XamlAnimatedGif",
                "https://github.com/XamlAnimatedGif/XamlAnimatedGif",
                "用于在图形用户界面上显示动态图片"),
        };
    }
}
