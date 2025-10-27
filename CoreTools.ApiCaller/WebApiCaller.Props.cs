namespace CoreTools.ApiCaller;

public static partial class WebApiCaller
{
    internal static readonly Dictionary<string, Func<CallerContext, CallerContext>> AuthorizeFuncs = [];
}
