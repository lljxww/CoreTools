namespace CoreTools.ApiCaller.Utilities;

internal static class HttpClientInstance
{
    private static IHttpClientFactory? _factory;

    // 单例共享 HttpClient
    private static HttpClient? client;

    // 供启动时调用，传入 DI 容器的 IHttpClientFactory
    public static void Initialize(IHttpClientFactory factory)
    {
        _factory = factory;
        client = _factory.CreateClient("WebApiCaller");
    }

    public static HttpClient Get()
    {
        return client ?? throw new InvalidOperationException("HttpClientInstance 未初始化，请在应用启动时调用 Initialize()");
    }

    public static HttpClient GetNew()
    {
        return _factory?.CreateClient("WebApiCaller") ?? new HttpClient();
    }
}
