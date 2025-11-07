using CoreTools.ApiCaller.Models;
using CoreTools.ApiCaller.Models.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreTools.ApiCaller;

public static class WebApiCallerServiceCollectionExtensions
{
    public static IServiceCollection ConfigureWebApiCaller(this IServiceCollection services,
        string runEnv, string fileName = "apicaller.json")
    {
        CallerOption.RunEnv = runEnv;

        var fileNamePart = fileName.Split('.');
        var envFileName = $"{fileNamePart[0]}.{runEnv}.{fileNamePart[1]}";

        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, fileName), optional: false, reloadOnChange: true)
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, envFileName), optional: true, reloadOnChange: true)
            .Build();

        services.Configure<ApiCallerConfig>(configuration);

        Console.WriteLine($"[Caller] 已加载配置文件: {fileName}");
        Console.WriteLine($"[Caller] 已加载配置文件: {envFileName}");

        _ = services.AddHttpClient("WebApiCaller", c =>
        {
            c.DefaultRequestHeaders.Add("User-Agent", "WebApiCaller");
            c.DefaultRequestHeaders.Connection.Add("keep-alive");
        });

        services.AddSingleton<WebApiCaller>();

        return services;
    }
}
