namespace CoreTools.ApiCaller.Models.Config;

/// <summary>
/// 接口配置节
/// </summary>
public class ApiItem
{
    /// <summary>
    /// 方法名
    /// </summary>
    public required string Label { get; set; }

    /// <summary>
    /// 方法调用的接口url
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    /// 方法的Http谓词
    /// </summary>
    public required string HTTPMethod { get; set; }

    /// <summary>
    /// 方法参数类型
    /// </summary>
    public required string ParamType { get; set; }

    /// <summary>
    /// 方法描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 是否需要缓存
    /// </summary>
    public bool NeedCache { get; set; } = false;

    /// <summary>
    /// 缓存时长(分钟)
    /// </summary>
    public int CacheTime { get; set; } = 10;

    /// <summary>
    /// 作为参数时，参数的类型（默认为application/json, 支持application/x-www-form-urlencoded)
    /// </summary>
    public string ContentType { get; set; } = "application/json";

    /// <summary>
    /// 接口授权类型
    /// </summary>
    public string? AuthorizationType { get; set; }

    /// <summary>
    /// 超时时间
    /// </summary>
    public int Timeout { get; set; }
}
