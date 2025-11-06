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
    IOptionsMonitor<ApiCallerConfig> monitor,
    IHttpClientFactory clientFactory) : IStartupFilter
{
    private readonly IOptionsMonitor<ApiCallerConfig> _monitor = monitor;
    private readonly IHttpClientFactory _clientFactory = clientFactory;

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            // 初始化逻辑
            CallerOptions.Init(_monitor);
            HttpClientInstance.Initialize(_clientFactory);

            next(app);
        };
    }
}

