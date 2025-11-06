using CoreTools.ApiCaller.Models;
using CoreTools.ApiCaller.Models.Config;
using CoreTools.ApiCaller.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace CoreTools.ApiCaller;

/// <summary>
/// 
/// </summary>
/// <param name="configure"></param>
public class CallerStartupFilter(
    Action<IApplicationBuilder> next,
    IOptionsMonitor<ApiCallerConfig> monitor,
    IHttpClientFactory clientFactory) : IStartupFilter
{
    private readonly Action<IApplicationBuilder> _next = next;
    private readonly IOptionsMonitor<ApiCallerConfig> _monitor = monitor;
    private readonly IHttpClientFactory _clientFactory = clientFactory;

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            // 应用完全启动后再初始化
            CallerOptions.Init(_monitor);
            HttpClientInstance.Initialize(_clientFactory);
            _next(app);
        };
    }
}

