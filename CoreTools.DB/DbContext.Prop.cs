using SqlSugar;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreTools.DB;

public partial class DbContext
{
    private static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = {
            new FlexibleDateTimeConverter(),
        }
    };

    public static void SetJsonSerializerOptions(Action<JsonSerializerOptions> configure)
    {
        configure(jsonSerializerOptions);
    }

    private static bool TraceLogEnabled = false;

    private static void SetTraceLogEnabled(bool enabled)
    {
        TraceLogEnabled = enabled;
    }

    private static Action<string>? TraceLogHandler = null;

    private static void SetTraceLogHandler(Action<string> action)
    {
        TraceLogHandler = action;
    }

    private static Action<Exception>? ErrorLogHandler = null;

    private static void SetErrorLogHandler(Action<Exception> action)
    {
        ErrorLogHandler = action;
    }

    private static Action<object, DataFilterModel>? DataExecuting = null;

    private static void SetDataExecuting(Action<object, DataFilterModel> action)
    {
        DataExecuting = action;
    }

    private static Action<object, DataAfterModel>? DataExecuted = null;

    private static void SetDataExecuted(Action<object, DataAfterModel> action)
    {
        DataExecuted = action;
    }
}
