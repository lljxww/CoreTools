using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using SqlSugar;

namespace CoreTools.DB;

public partial class DbContext
{
    #region Json序列化配置
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
    #endregion

    #region 日志
    private static bool TraceLogEnabled = false;

    public static void SetTraceLogEnabled(bool enabled)
    {
        TraceLogEnabled = enabled;
    }

    private static Action<string>? TraceLogHandler = null;

    public static void SetTraceLogHandler(Action<string> action)
    {
        TraceLogHandler = action;
    }

    private static Action<Exception>? ErrorLogHandler = null;

    public static void SetErrorLogHandler(Action<Exception> action)
    {
        ErrorLogHandler = action;
    }
    #endregion

    #region SqlSugar的Aop配置(可用于配置数据加密解密
    private static Action<object, DataFilterModel>? DataExecuting = null;

    public static void SetDataExecuting(Action<object, DataFilterModel> action)
    {
        DataExecuting = action;
    }

    private static Action<object, DataAfterModel>? DataExecuted = null;

    public static void SetDataExecuted(Action<object, DataAfterModel> action)
    {
        DataExecuted = action;
    }

    private static ConfigureExternalServices? CustomConfigureExternalService = null;

    public static void SetConfigureExternalServices(ConfigureExternalServices services)
    {
        CustomConfigureExternalService = services;
    }
    #endregion
}
