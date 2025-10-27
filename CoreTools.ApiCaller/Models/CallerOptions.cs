using CoreTools.ApiCaller.Models.Config;
using Microsoft.Extensions.Options;

namespace CoreTools.ApiCaller.Models;

public class CallerOptions
{
    private static ApiCallerConfig? _config;

    public static void Init(IOptionsMonitor<ApiCallerConfig> monitor)
    {
        _config = monitor.CurrentValue;
        monitor.OnChange(opt => _config = opt);
    }

    public static ApiCallerConfig Config => _config ?? throw new Exception("请先初始化Caller配置！");
}
