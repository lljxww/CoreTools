using CoreTools.ApiCaller.Models.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreTools.ApiCaller;

public static class WebApiCallerServiceCollectionExtensions
{
    public static IServiceCollection ConfigureWebApiCaller(this IServiceCollection services,
        string runEnv, string fileName = "apicaller.json")
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, fileName), optional: false, reloadOnChange: true)
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, GetConfigName(runEnv, fileName)), optional: true, reloadOnChange: true)
            .Build();

        services.Configure<ApiCallerConfig>(configuration);

        _ = services.AddHttpClient("WebApiCaller", c =>
        {
            c.DefaultRequestHeaders.Add("User-Agent", "WebApiCaller");
            c.DefaultRequestHeaders.Connection.Add("keep-alive");
        });

        return services;
    }

    private static string GetConfigName(string runEnv, string fileName)
    {
        var fileNamePart = fileName.Split('.');
        return $"{fileNamePart[0]}.{runEnv}.{fileNamePart[1]}";
    }
}
