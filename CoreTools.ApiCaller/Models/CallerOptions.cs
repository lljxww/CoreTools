using CoreTools.ApiCaller.Models.Config;

namespace CoreTools.ApiCaller.Models;

public class CallerOptions
{
    private static ApiCallerConfig? _config;

    internal static void Init(ApiCallerConfig config)
    {
        _config = config;
    }

    public static ApiCallerConfig Config => _config ?? throw new Exception("请先初始化Caller配置！");
}
