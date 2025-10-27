namespace CoreTools.ApiCaller;

public interface ICallerAuthorize
{
    string GetAuthorizeName();

    /// <summary>
    /// 处理请求中的认证
    /// </summary>
    /// <returns></returns>
    CallerContext Func(CallerContext context);
}
