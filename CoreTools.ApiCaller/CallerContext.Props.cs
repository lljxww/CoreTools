using CoreTools.ApiCaller.Models;
using CoreTools.ApiCaller.Models.Config;

namespace CoreTools.ApiCaller;

public partial class CallerContext
{
    /// <summary>
    /// 服务名.方法名
    /// </summary>
    public required string ApiName { get; set; }

    public required HttpMethod HttpMethod { get; set; }

    /// <summary>
    /// 超时时间(计算后)
    /// </summary>
    public int Timeout { get; set; } = 40000;

    /// <summary>
    /// 服务配置节
    /// </summary>
    public required ServiceItem ServiceItem { get; set; }

    /// <summary>
    /// 基础地址
    /// </summary>
    public required string BaseUrl { get; set; }

    /// <summary>
    /// 请求时的特定设置
    /// </summary>
    public required RequestOption RequestOption { get; set; }

    /// <summary>
    /// 认证信息
    /// </summary>
    public Authorization? Authorization { get; private set; }

    /// <summary>
    /// Api配置节
    /// </summary>
    public required ApiItem ApiItem { get; set; }

    /// <summary>
    /// 是否需要缓存(计算后)
    /// </summary>
    public required bool NeedCache { get; set; } = false;

    /// <summary>
    /// 最终的请求地址(计算后)
    /// </summary>
    public required string FinalUrl { get; set; }

    /// <summary>
    /// 请求参数
    /// </summary>
    public object? OriginParam { get; private set; }

    /// <summary>
    /// 请求参数(转换为字典类型后)
    /// </summary>
    public Dictionary<string, string>? ParamDic { get; private set; }

    /// <summary>
    /// 响应结果
    /// </summary>
    public string? ResponseContent { get; set; }

    /// <summary>
    /// 请求执行时间
    /// </summary>
    public int Runtime { get; set; } = 0;

    /// <summary>
    /// 请求结果来源
    /// </summary>
    public required string ResultFrom { get; set; } = "R";

    /// <summary>
    /// 缓存Key
    /// </summary>
    public required string CacheKey { get; set; }

    /// <summary>
    /// 请求结果对象
    /// </summary>
    public ApiResult? ApiResult { get; set; }

    /// <summary>
    /// 请求体
    /// </summary>
    public required HttpRequestMessage RequestMessage { get; set; }

    /// <summary>
    /// 缓存时间(分, 计算后)
    /// </summary>
    public int CacheMinuties { get; private set; } = 1;
}
