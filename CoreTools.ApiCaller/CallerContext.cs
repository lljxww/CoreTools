using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CoreTools.ApiCaller.Models;
using CoreTools.ApiCaller.Models.Config;
using CoreTools.ApiCaller.Utilities;

namespace CoreTools.ApiCaller;

public class CallerContext
{
    private CallerContext() { }

    internal async Task<CallerContext> RequestAsync(HttpClient client)
    {
        ResultFrom = "R";

        Stopwatch sw = new();
        try
        {
            using var cts = RequestOption.CreateCancellationSource(Timeout);

            sw.Start();
            var response = await client.SendAsync(RequestMessage, cts.Token);
            ResponseContent = await response.Content.ReadAsStringAsync();
        }
        catch (OperationCanceledException)
        {
            // 标识发生超时
            ApiResult = ApiResult.Build(new
            {
                Success = false,
                Code = 6001,
                Message = $"请求超时{Timeout}ms, 服务: {ServiceItem.Label}.{ApiItem.Label}"
            });
            ResultFrom = "T"; // “Timeout”
            return this;
        }
        finally
        {
            sw.Stop();
            Runtime = Convert.ToInt32(sw.ElapsedMilliseconds);
        }

        ApiResult = new ApiResult(ResponseContent);

        return this;
    }

    /// <summary>
    /// 创建Caller上下文实例
    /// </summary>
    /// <param name="apiNameAndMethodName">服务名.方法名</param>
    /// <param name="config">配置对象</param>
    /// <param name="param">参数对象</param>
    /// <returns></returns>
    internal static CallerContext Build(ApiCallerConfig config,
        string apiNameAndMethodName,
        object? param,
        RequestOption requestOption)
    {
        (var serviceItem, var apiItem) = GetServiceConfigItem(apiNameAndMethodName, config);
        apiItem.ParamType = apiItem.ParamType.ToLower().Trim();

        var paramDic = param?.AsDictionary();
        var httpMethod = new HttpMethod(apiItem.HTTPMethod);
        var finalUrl = GetFinalUrl(serviceItem.BaseUrl, apiItem.Url, apiItem.ParamType, paramDic, requestOption);

        var timeout = requestOption.Timeout > 0
                 ? requestOption.Timeout
                 : apiItem.Timeout > 0
                     ? apiItem.Timeout
                     : serviceItem.Timeout;
        timeout = timeout > 0 ? timeout : 4000;

        var context = new CallerContext
        {
            ApiName = apiNameAndMethodName,
            OriginParam = param,
            ParamDic = paramDic,
            ServiceItem = serviceItem,
            ApiItem = apiItem,
            BaseUrl = serviceItem.BaseUrl,
            HttpMethod = httpMethod,
            NeedCache = apiItem.NeedCache,
            CacheMinuties = apiItem.CacheTime,
            RequestOption = requestOption,
            ResultFrom = "R",
            FinalUrl = finalUrl,
            RequestMessage = GetRequestMessage(
                apiItem.ParamType,
                apiItem.ContentType,
                param,
                requestOption,
                serviceItem.UseCamelCase,
                paramDic,
                httpMethod,
                finalUrl),
            CacheKey = apiItem.NeedCache ?
                GetCacheKey(param, apiNameAndMethodName, requestOption.CacheKeyPart)
                : string.Empty,
            Timeout = timeout
        };

        context = MakeAuthorization(context, config);

        return context;
    }

    /// <summary>
    /// 从配置中, 找到指定的服务和方法
    /// </summary>
    /// <param name="apiNameAndMethodName">服务名和方法名</param>
    /// <param name="config">Caller请求的配置文件</param>
    /// <returns></returns>
    private static (ServiceItem, ApiItem) GetServiceConfigItem(string apiNameAndMethodName, ApiCallerConfig config)
    {
        var serviceName = apiNameAndMethodName.Split('.')[0];
        var methodName = apiNameAndMethodName.Split('.')[1];

        var serviceItem = config.ServiceItems.Single(a => a.Label.ToLower().Trim() == serviceName.ToLower().Trim());
        var apiItem = serviceItem.ApiItems.Single(c => c.Label.ToLower().Trim() == methodName.ToLower().Trim());

        return (serviceItem, apiItem);
    }

    /// <summary>
    /// 如果请求需要缓存, 在这里生成缓存的键
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    private static string GetCacheKey(object? originParam,
        string apiName,
        string? cacheKeyPart)
    {
        // 使用 System.Text.Json 序列化参数
        var originParamJson = originParam == null
            ? ""
            : JsonSerializer.Serialize(originParam);

        var keySource = $"{apiName.ToLower()}+{originParamJson}{cacheKeyPart}";

        // SHA1 哈希
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(keySource));

        // 转成小写十六进制字符串
        return "wc:" + Convert.ToHexStringLower(hash);
    }

    private static string GetFinalUrl(string baseUrl,
        string apiUrl,
        string paramType,
        Dictionary<string, string>? paramDic,
        RequestOption requestOption)
    {
        var finalUrl = $"{baseUrl.TrimEnd('/')}/{apiUrl.TrimStart('/')}";
        finalUrl = finalUrl.TrimStart('/');

        switch (paramType.ToLower())
        {
            case "query":
                {
                    if (paramDic != null && paramDic.Count > 0)
                    {
                        // 使用 UriBuilder + HttpUtility.ParseQueryString 统一处理 Query，避免双重编码
                        var builder = new UriBuilder(finalUrl);
                        var query = System.Web.HttpUtility.ParseQueryString(builder.Query);

                        foreach (var pair in paramDic)
                        {
                            query[pair.Key] = pair.Value; // 不要手动 UrlEncode
                        }

                        builder.Query = query.ToString(); // 自动编码
                        finalUrl = builder.ToString();
                    }

                    break;
                }
            case "path":
                {
                    if (paramDic != null && paramDic.Count > 0)
                    {
                        foreach (var pair in paramDic)
                        {
                            // 对 Path 参数做一次安全编码，避免非法字符
                            finalUrl = finalUrl.Replace(
                                 $"{{{pair.Key}}}",
                                 Uri.EscapeDataString(pair.Value)
                             );
                        }
                    }

                    break;
                }
            default: break;
        }

        // 用户自定义的url
        if (requestOption.CustomFinalUrlHandler != null)
        {
            finalUrl = requestOption.CustomFinalUrlHandler.Invoke(finalUrl);
        }

        return finalUrl;
    }

    /// <summary>
    /// 构建CallerContext中的请求体
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    private static HttpRequestMessage GetRequestMessage(
        string paramType,
        string? contentType,
        object? originParam,
        RequestOption requestOption,
        bool useCamelCase,
        Dictionary<string, string>? paramDic,
        HttpMethod httpMethod,
        string finalUrl)
    {
        // 默认空内容，避免 null
        HttpContent httpContent = new ByteArrayContent([]);

        // 仅当为 body 参数类型时构建内容
        if (paramType.Equals("body", StringComparison.OrdinalIgnoreCase))
        {
            // 1. 自定义内容优先
            if (requestOption.CustomHttpContent is not null)
            {
                httpContent = requestOption.CustomHttpContent;
            }
            // 2. JSON body
            else if (originParam is not null && !string.IsNullOrWhiteSpace(contentType))
            {
                var options = useCamelCase
                    ? JsonSetting.CAMEL_CASE_POLICY_OPTION
                    : JsonSetting.DEFAULT_SERIALIZER_OPTION;

                var json = JsonSerializer.Serialize(originParam, options);
                httpContent = new StringContent(json, Encoding.UTF8);
                httpContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            }
            // 3. Form body
            else if (paramDic is not null)
            {
                // FormUrlEncodedContent 会自动编码
                httpContent = new FormUrlEncodedContent(paramDic);
            }
        }

        return new HttpRequestMessage(httpMethod, new Uri(finalUrl))
        {
            Content = httpContent
        };
    }

    private static CallerContext MakeAuthorization(CallerContext context, ApiCallerConfig config)
    {
        var hasAuthorizations = config.Authorizations != null
            && config.Authorizations.Count > 0;

        // 处理接口授权
        if (!string.IsNullOrWhiteSpace(context.ServiceItem.AuthorizationType))
        {
            var authName = context.ServiceItem.AuthorizationType.Trim();

            if (!CallerOption.AuthorizeFuncs.ContainsKey(authName))
            {
                throw new Exception($"找不到授权配置: {context.ServiceItem.AuthorizationType}");
            }

            context.Authorization = (hasAuthorizations && config.Authorizations!.Any(a => a.Name == authName))
                ? config.Authorizations!.Single(a => a.Name == authName)
                : new Authorization
                {
                    Name = authName
                };
        }

        if (!string.IsNullOrWhiteSpace(context.ApiItem.AuthorizationType))
        {
            var authName = context.ApiItem.AuthorizationType.Trim();

            if (!CallerOption.AuthorizeFuncs.ContainsKey(authName))
            {
                throw new Exception($"找不到授权配置: {context.ApiItem.AuthorizationType}");
            }

            context.Authorization = (hasAuthorizations && config.Authorizations!.Any(a => a.Name == authName))
                 ? config.Authorizations!.Single(a => a.Name == authName)
                 : new Authorization
                 {
                     Name = authName
                 };
        }

        context.Authorization ??= new Authorization
        {
            Name = string.Empty
        };

        // 添加自定义AuthorizeInfo
        if (!string.IsNullOrWhiteSpace(context.RequestOption.CustomAuthorizeInfo))
        {
            context.Authorization.AuthorizationInfo = context.RequestOption.CustomAuthorizeInfo;
        }

        if (!string.IsNullOrWhiteSpace(context.Authorization?.Name))
        {
            context = CallerOption.AuthorizeFuncs[context.Authorization.Name].Invoke(context);
        }

        return context;
    }

    public override string ToString()
    {
        string paramJson;
        try
        {
            var options = ServiceItem.UseCamelCase
                                ? JsonSetting.CAMEL_CASE_POLICY_OPTION
                                : JsonSetting.DEFAULT_SERIALIZER_OPTION;
            paramJson = JsonSerializer.Serialize(OriginParam, options);
        }
        catch
        {
            paramJson = "[Unserializable Parameter]";
        }

        string result;
        try
        {
            result = ApiResult?.RawStr ?? "[No Result]";
        }
        catch
        {
            result = "[Unserializable Result]";
        }

        return $@"
-----------------------------------------------------------
|> TIME: {DateTime.Now:yyyy/MM/dd HH:mm:ss}
|> METHOD: {ServiceItem.Label}.{ApiItem.Label}
|> USE CAMEL CASE: {ServiceItem.UseCamelCase}
|> URL: {HttpMethod} {FinalUrl}
|> PARAM: {paramJson}
|> PARAM TYPE: {ApiItem?.ParamType}
|> RESULT: {result}
-----------------------------------------------------------";
    }

    #region Props
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
    #endregion
}
