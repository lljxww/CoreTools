namespace CoreTools.ApiCaller.Models.Config;

/// <summary>
/// 接口配置文件
/// </summary>
public class ApiCallerConfig
{
    /// <summary>
    /// 授权方式
    /// </summary>
    public IList<Authorization>? Authorizations { get; set; }

    /// <summary>
    /// 接口配置节
    /// </summary>
    public required IList<ServiceItem> ServiceItems { get; set; }
}
