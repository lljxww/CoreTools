using CoreTools.ApiCaller.Models;

namespace CoreTools.ApiCaller;

public static partial class WebApiCaller
{
    /// <summary>
    /// 设置缓存
    /// </summary>
    public delegate void SetCacheHandler(CallerContext context);
    public static event SetCacheHandler? SetCacheEvent;

    /// <summary>
    /// 读取缓存
    /// </summary>
    public delegate ApiResult GetCacheHandler(CallerContext context);
    public static event GetCacheHandler? GetCacheEvent;

    /// <summary>
    /// 清除缓存
    /// </summary>
    public delegate void RemoveCacheHandler(CallerContext context);
    public static event RemoveCacheHandler? RemoveCacheEvent;

    /// <summary>
    /// 记录日志
    /// </summary>
    public delegate void LogHandler(CallerContext context);
    public static event LogHandler? LogEvent;

    /// <summary>
    /// 请求方法执行结束后的操作
    /// </summary>
    public delegate void OnExecutedHandler(CallerContext context);
    public static event OnExecutedHandler? OnExecuted;

    /// <summary>
    /// 执行发生异常时触发
    /// </summary>
    public delegate void OnExceptionHandler(CallerContext context, Exception ex);
    public static event OnExceptionHandler? OnException;

    /// <summary>
    /// 请求超时时触发
    /// </summary>
    public delegate void OnRequestTimeoutHandler(CallerContext context);
    public static event OnRequestTimeoutHandler? OnRequestTimeout;
}
