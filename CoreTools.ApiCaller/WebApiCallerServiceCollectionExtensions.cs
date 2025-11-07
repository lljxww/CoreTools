using CoreTools.ApiCaller.Models.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreTools.ApiCaller;

public static class WebApiCallerServiceCollectionExtensions
{
    public static IServiceCollection ConfigureWebApiCaller(this IServiceCollection services,
        string runEnv, string fileName = "apicaller.json")
    {
        var fileNamePart = fileName.Split('.');
        var envFileName = $"{fileNamePart[0]}.{runEnv}.{fileNamePart[1]}";

        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, fileName), optional: false, reloadOnChange: true)
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, envFileName), optional: true, reloadOnChange: true)
            .Build();

        services.Configure<ApiCallerConfig>(configuration);

        _ = services.AddHttpClient("WebApiCaller", c =>
        {
            c.DefaultRequestHeaders.Add("User-Agent", "WebApiCaller");
            c.DefaultRequestHeaders.Connection.Add("keep-alive");
        });

        services.AddSingleton<WebApiCaller>();

        return services;
    }
}
