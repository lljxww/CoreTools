namespace CoreTools.ApiCaller;

public static partial class WebApiCaller
{
    public static bool Inited { get; private set; } = false;

    internal static readonly Dictionary<string, Func<CallerContext, CallerContext>> AuthorizeFuncs = [];
}
