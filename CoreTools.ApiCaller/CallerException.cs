namespace CoreTools.ApiCaller;

public class CallerException : Exception
{
    public int Code { get; set; } = -1;

    public object? Content { get; set; } = null;

    public CallerException(string message) : base(message) { }

    public CallerException(int code, string message) : base(message)
    {
        Code = code;
    }

    public static bool IsCallerException(Exception ex, out CallerException? aamsException)
    {
        var inner = ex;
        while (inner.InnerException != null)
        {
            inner = inner.InnerException;
        }

        var result = inner is CallerException;

        if (result)
        {
            aamsException = (CallerException)inner;
        }
        else
        {
            aamsException = null;
        }

        return result;
    }
}
