using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace CoreTools.ApiCaller;

/// <summary>
/// 
/// </summary>
/// <param name="configure"></param>
public class CallerStartupFilter(Action<IApplicationBuilder> configure) : IStartupFilter
{
    private readonly Action<IApplicationBuilder> _configure = configure ?? throw new ArgumentNullException(nameof(configure));

    /// <summary>
    /// 
    /// </summary>
    /// <param name="next"></param>
    /// <returns></returns>
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            _configure(app);
            next(app);
        };
    }
}
