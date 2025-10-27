# 自用.net工具

## CoreTools.DB
基于SqlSugar的操作封装(没有任何其他高级的东西), 目的是兼容原自有.net framework ORM的方法, 避免代码大规模改动

## CoreTools.ApiCaller
WebApi调用工具. 使用配置文件统一管理请求服务和接口信息, 支持自定义认证, 日志, 缓存等.

初始化参考:

Program.cs
``` csharp
// 以AutoFac为例
containerBuilder.RegisterType<StartupTask>()
    .As<IHostedService>()
    .SingleInstance();
```

StartupTask.cs
``` csharp
public class StartupTask(ILifetimeScope lifetimeScope) : IHostedService
{
    private readonly ILifetimeScope _lifetimeScope = lifetimeScope;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // 其他配置

        // 配置WebApiCaller(扫描指定类型同命名空间下的所有ICallerAuthorize实现)
        WebApiCaller.RegisterAuthorizers(typeof(JwtAuthorize));
        using var scope = _lifetimeScope.BeginLifetimeScope();
        var loginEvents = scope.Resolve<LoginEvents>();
        WebApiCallerConfigure(_appSettings.RegexConfig, loginEvents, _env.IsDevelopment());

        // 其他配置

        return Task.CompletedTask;
    }

    private static bool _configured = false;

    private static void WebApiCallerConfigure(RegexConfig regexConfig, LoginEvents loginEvents, bool isDevelopment)
    {
        WebApiCaller.OnExecuted += context => {
            // 配置自定义操作
        };
    
        // 其他事件配置
    }
}
```

使用:
``` csharp
ApiResult result = await WebApiCaller.InvokeAsync("HFS.DownloadFile", new { fileCode = fileName }, new RequestOption
{
    CustomAuthorizeInfo = _jwtService.GenerateSystemJwt()
});

Assert.IsTrue(result.Success);
Assert.AreEqual("hello", result["key_of_hello"]);
```