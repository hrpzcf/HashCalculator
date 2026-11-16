namespace HashCalculator.ViewModels;

/// <summary>
/// 第三方组件信息，供关于页展示组件名称、用途、许可名称、仓库地址与许可全文资源名。
/// </summary>
public sealed class ThirdPartyComponent
{
    public ThirdPartyComponent(
        string name, string usage, string license, string repositoryUrl, string licenseResource)
    {
        this.Name = name;
        this.Usage = usage;
        this.License = license;
        this.RepositoryUrl = repositoryUrl;
        this.LicenseResource = licenseResource;
    }

    /// <summary>
    /// 组件名称。
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 组件在本程序中的用途。
    /// </summary>
    public string Usage { get; }

    /// <summary>
    /// 组件所遵循的开源许可名称。
    /// </summary>
    public string License { get; }

    /// <summary>
    /// 组件仓库地址。
    /// </summary>
    public string RepositoryUrl { get; }

    /// <summary>
    /// 许可全文在程序集中的资源名后缀，读取时由 <see cref="LicenseTexts"/> 拼接统一前缀。
    /// </summary>
    public string LicenseResource { get; }
}
