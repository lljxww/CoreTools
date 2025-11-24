using System.Text.Json.Serialization;

namespace CoreTools.ApiCaller.Models;

public class RequestOption
{
    /// <summary>
    /// 是否触发OnExecuted方法
    /// </summary>
    public bool IsTriggerOnExecuted { get; set; } = true;

    /// <summary>
    /// 是否从缓存读取结果(如果有)
    /// </summary>
    public bool IsFromCache { get; set; } = true;

    /// <summary>
    /// 当此predicate值为true时, 不将结果写入Cache
    /// </summary>
    [JsonIgnore]
    public Predicate<CallerContext> WhenDontSaveRequestCache { get; set; } = _ => false;

    /// <summary>
    /// 不记录日志
    /// </summary>
    public bool DontLog { get; set; } = false;

    /// <summary>
    /// 自定义URL配置
    /// </summary>
    [JsonIgnore]
    public Func<string, string>? CustomFinalUrlHandler { get; set; }

    /// <summary>
    /// 自定义请求体
    /// </summary>
    [JsonIgnore]
    public HttpContent? CustomHttpContent { get; set; }

    /// <summary>
    /// 自定义认证信息
    /// </summary>
    public string? CustomAuthorizeInfo { get; set; }

    /// <summary>
    /// 超时时长（ms），超过此时间的请求将取消
    /// </summary>
    public int Timeout
    {
        get;
        set => field = value <= 0 ? 40000 : value;
    } = 40000;

    /// <summary>
    /// 自定义对象, 可用于将请求时的一些细节传递到各类事件处理程序中使用
    /// </summary>
    [JsonIgnore]
    public object? CustomObject { get; set; }

    /// <summary>
    /// 用于参与计算缓存key的值
    /// </summary>
    public string? CacheKeyPart { get; set; }

    /// <summary>
    /// 是否将参数转换为小驼峰
    /// </summary>
    public bool IsLowerCamelCaseParam { get; set; } = false;

    /// <summary>
    /// 获取自定义对象
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T? GetCustomObject<T>()
    {
        if (CustomObject == null)
        {
            return default;
        }

        try
        {
            return (T)CustomObject;
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// 获取自定义对象, 如果获取失败, 则返回给定的默认值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public T GetCustomObject<T>(T defaultValue)
    {
        var result = GetCustomObject<T>();

        result ??= defaultValue;

        return result;
    }

    public static CancellationTokenSource CreateCancellationSource(int timeout)
    {
        var cts = new CancellationTokenSource();
        cts.CancelAfter(timeout);
        return cts;
    }
}
