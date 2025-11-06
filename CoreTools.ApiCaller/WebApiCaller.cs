using System.Reflection;
using CoreTools.ApiCaller.Models;
using CoreTools.ApiCaller.Models.Config;
using CoreTools.ApiCaller.Utilities;

namespace CoreTools.ApiCaller;

public static partial class WebApiCaller
{
    private static readonly Lock _lock = new();

    public static void InitCaller(string envName,
        ApiCallerConfig apiCallerConfig,
        IHttpClientFactory httpClientFactory)
    {
        if (Inited)
        {
            return;
        }

        lock (_lock)
        {
            if (Inited)
            {
                return;
            }

            CallerOptions.Init(apiCallerConfig);
            HttpClientInstance.Initialize(httpClientFactory);

            Inited = true;
        }
    }

    public static async Task<ApiResult> InvokeAsync(string apiNameAndMethodName,
        object? requestParam = null, RequestOption? requestOption = null)
    {
        if (!Inited)
        {
            throw new Exception("请先运行WebApiCaller.Init()完成Caller的初始化工作!");
        }

        // 创建请求对象
        var context = CallerContext.Build(apiNameAndMethodName, requestParam, requestOption ?? new RequestOption());

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
                context = await context.RequestAsync();

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
    public static void RemoveCache(string apiNameAndMethodName,
        object? requestParam = null,
        string? cacheKeyPart = null)
    {
        // 创建请求对象
        var context = CallerContext.Build(apiNameAndMethodName, requestParam, new RequestOption
        {
            CacheKeyPart = cacheKeyPart
        });

        RemoveCacheEvent?.Invoke(context);
    }

    /// <summary>
    /// 自动扫描并注册所有 ICallerAuthorize 实现类
    /// </summary>
    public static void RegisterAuthorizers(string? namespaceStr)
    {
        var authorizeTypes = GetAuthorizeTypes(namespaceStr);

        foreach (var type in authorizeTypes)
        {
            try
            {
                if (Activator.CreateInstance(type) is not ICallerAuthorize instance)
                {
                    continue;
                }

                var name = instance.GetAuthorizeName();

                if (string.IsNullOrWhiteSpace(name))
                {
                    Console.Error.WriteLine($"[Authorize] 类型 {type.FullName} 返回了空的授权名称，已跳过。");
                    continue;
                }

                if (AuthorizeFuncs.ContainsKey(name))
                {
                    Console.Error.WriteLine($"[Authorize] 重复的授权名称: {name} 来自 {type.FullName}，已忽略。");
                    continue;
                }

                AuthorizeFuncs[name] = instance.Func;
                Console.WriteLine($"[Authorize] 已注册: {name} -> {type.FullName}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Authorize] 注册 {type.FullName} 时出错: {ex.Message}");
            }
        }
    }

    public static void RegisterAuthorizers(Type callerType)
        => RegisterAuthorizers(callerType?.Namespace);

    /// <summary>
    /// 扫描指定命名空间下所有实现 ICallerAuthorize 的类型。
    /// </summary>
    private static List<Type> GetAuthorizeTypes(string? targetNamespace)
    {
        var baseType = typeof(ICallerAuthorize);

        // 建议缓存，防止频繁扫描
        var assemblies = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a =>
                !a.IsDynamic &&
                a.FullName is not null &&
                !a.FullName.StartsWith("System", StringComparison.Ordinal) &&
                !a.FullName.StartsWith("Microsoft", StringComparison.Ordinal) &&
                !a.FullName.StartsWith("netstandard", StringComparison.Ordinal))
            .ToList();

        var result = new List<Type>(capacity: 64);

        foreach (var asm in assemblies)
        {
            foreach (var type in SafeGetTypes(asm))
            {
                if (type is null ||
                    type.IsInterface ||
                    type.IsAbstract ||
                    !baseType.IsAssignableFrom(type))
                {
                    continue;
                }

                if (string.IsNullOrEmpty(targetNamespace) ||
                    (type.Namespace?.StartsWith(targetNamespace, StringComparison.Ordinal) ?? false))
                {
                    result.Add(type);
                }
            }
        }

        return result;
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // 打印缺失依赖信息，方便调试
            var missing = ex.LoaderExceptions
                .OfType<FileNotFoundException>()
                .Select(e => e.FileName)
                .Distinct();

            Console.Error.WriteLine($"[Authorize] {assembly.GetName().Name} 类型加载不完整: {string.Join(", ", missing)}");

            return ex.Types.Where(t => t != null)!;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Authorize] 无法加载程序集 {assembly.GetName().Name}: {ex.Message}");
            return [];
        }
    }
}
