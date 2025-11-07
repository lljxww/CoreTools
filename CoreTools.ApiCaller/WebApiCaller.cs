using CoreTools.ApiCaller.Models;
using CoreTools.ApiCaller.Models.Config;
using Microsoft.Extensions.Options;

namespace CoreTools.ApiCaller;

public class WebApiCaller(IOptions<ApiCallerConfig> config, IHttpClientFactory factory)
{
    private readonly ApiCallerConfig _config = config.Value;
    private readonly IHttpClientFactory _factory = factory;

    public async Task<ApiResult> InvokeAsync(string apiNameAndMethodName,
        object? requestParam = null, RequestOption? requestOption = null)
    {
        // 创建请求对象
        var context = CallerContext.Build(_config, apiNameAndMethodName, requestParam, requestOption ?? new RequestOption());

        // 尝试从缓存读取结果
        if (context.NeedCache)
        {
            if (context.RequestOption.IsFromCache)
            {
                context.ApiResult = SafeGetCache(GetCacheEvent, context);

                if (context.ApiResult != null)
                {
                    context.ResultFrom = "C";
                }
            }
        }

        // 从新Http请求获取结果
        if (context.ApiResult == null)
        {
            try
            {
                // 执行请求
                using var _client = _factory.CreateClient("WebApiCaller");
                context = await context.RequestAsync(_client);

                if (context.ResultFrom == "T") // 检查是否超时
                {
                    RaiseEvent(OnRequestTimeout, context);
                    throw new CallerException(-1, $"[Caller]请求超时: {context.ServiceItem.Label}.{context.ApiItem.Label}");
                }
            }
            catch (Exception ex)
            {
                if (!CallerException.IsCallerException(ex, out _))
                {
                    // 触发异常事件
                    RaiseEvent(OnException, context, ex);
                }

                throw;
            }

            // 处理缓存
            if (context.NeedCache && !context.RequestOption.WhenDontSaveRequestCache.Invoke(context))
            {
                RaiseEvent(SetCacheEvent, context);
            }
        }

        // 记录日志事件
        if (!context.RequestOption.DontLog)
        {
            RaiseEvent(LogEvent, context);
        }

        // 执行后事件
        if (context.RequestOption.IsTriggerOnExecuted)
        {
            RaiseEvent(OnExecuted, context);
        }

        return context.ApiResult!;
    }

    /// <summary>
    /// 清除指定请求的缓存
    /// </summary>
    /// <param name="apiNameAndMethodName"></param>
    /// <param name="requestParam"></param>
    public void RemoveCache(string apiNameAndMethodName,
        object? requestParam = null,
        string? cacheKeyPart = null)
    {
        // 创建请求对象
        var context = CallerContext.Build(_config, apiNameAndMethodName, requestParam, new RequestOption
        {
            CacheKeyPart = cacheKeyPart
        });

        RemoveCacheEvent?.Invoke(context);
    }

    #region Events
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
    #endregion

    #region Utils
    private static void RaiseEvent(MulticastDelegate? eventDelegate, CallerContext context)
    {
        var delegates = eventDelegate?.GetInvocationList();
        if (delegates == null)
        {
            return;
        }

        foreach (var handler in delegates)
        {
            try
            {
                var action = handler.DynamicInvoke(context);
                if (action is Task task)
                {
                    _ = Task.Run(() => task); // fire-and-forget 异步执行
                }
            }
            catch
            {
            }
        }
    }

    public static void RaiseEvent(MulticastDelegate? eventDelegate, CallerContext context, Exception ex)
    {
        var delegates = eventDelegate?.GetInvocationList();
        if (delegates == null)
        {
            return;
        }

        foreach (var handler in delegates)
        {
            try
            {
                handler.DynamicInvoke(context, ex);
            }
            catch
            {
            }
        }
    }

    private static ApiResult? SafeGetCache(MulticastDelegate? eventDelegate, CallerContext context)
    {
        if (eventDelegate == null)
        {
            return null;
        }

        try
        {
            // 这里只有一个返回值，取最后一个非 null 的结果
            var delegates = eventDelegate.GetInvocationList();
            foreach (var d in delegates)
            {
                try
                {
                    if (d.DynamicInvoke(context) is ApiResult result)
                    {
                        return result;
                    }
                }
                catch
                {
                }
            }
        }
        catch
        {
        }

        return null;
    }
    #endregion
}
