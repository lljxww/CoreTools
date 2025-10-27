using CoreTools.ApiCaller.Models;

namespace CoreTools.ApiCaller;

public static partial class WebApiCaller
{
    private static void RaiseEvent(MulticastDelegate? eventDelegate, CallerContext context)
    {
        var delegates = eventDelegate?.GetInvocationList();
        if (delegates == null)
        {
            return;
        }

        foreach (var handler in delegates)
        {
            try
            {
                var action = handler.DynamicInvoke(context);
                if (action is Task task)
                {
                    _ = Task.Run(() => task); // fire-and-forget 异步执行
                }
            }
            catch
            {
            }
        }
    }

    public static void RaiseEvent(MulticastDelegate? eventDelegate, CallerContext context, Exception ex)
    {
        var delegates = eventDelegate?.GetInvocationList();
        if (delegates == null)
        {
            return;
        }

        foreach (var handler in delegates)
        {
            try
            {
                handler.DynamicInvoke(context, ex);
            }
            catch
            {
            }
        }
    }

    private static ApiResult? SafeGetCache(MulticastDelegate? eventDelegate, CallerContext context)
    {
        if (eventDelegate == null)
        {
            return null;
        }

        try
        {
            // 这里只有一个返回值，取最后一个非 null 的结果
            var delegates = eventDelegate.GetInvocationList();
            foreach (var d in delegates)
            {
                try
                {
                    if (d.DynamicInvoke(context) is ApiResult result)
                    {
                        return result;
                    }
                }
                catch
                {
                }
            }
        }
        catch
        {
        }

        return null;
    }
}
