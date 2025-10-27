namespace CoreTools.ApiCaller.Models.Config;

public class ServiceItem
{
    /// <summary>
    /// 接口名
    /// </summary>
    public required string Label { get; set; }

    /// <summary>
    /// 接口授权类型
    /// </summary>
    public string? AuthorizationType { get; set; }

    /// <summary>
    /// 接口地址
    /// </summary>
    public required string BaseUrl { set; get; }

    /// <summary>
    /// 超时时间
    /// </summary>
    public int Timeout { get; set; }

    /// <summary>
    /// 接口配置节
    /// </summary>
    public required IList<ApiItem> ApiItems { get; set; }

    /// <summary>
    /// 是否使用小驼峰命名法
    /// </summary>
    public bool UseCamelCase { get; set; } = false;
}
