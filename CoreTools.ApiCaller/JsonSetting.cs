using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreTools.ApiCaller;

internal class JsonSetting
{
    private static readonly JsonSerializerOptions _default = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = {
            new FlexibleDateTimeConverter()
        }
    };

    private static readonly JsonSerializerOptions _camelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = {
            new FlexibleDateTimeConverter()
        }
    };

    public static JsonSerializerOptions DEFAULT_SERIALIZER_OPTION { get; } = _default;

    public static JsonSerializerOptions CAMEL_CASE_POLICY_OPTION { get; } = _camelCase;
}

internal class FlexibleDateTimeConverter : JsonConverter<DateTime>
{
    private static readonly string[] Formats =
    [
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy/MM/dd HH:mm:ss",
        "yyyy/MM/ddTHH:mm:ss",
        "yyyy-MM-dd",
        "yyyy/MM/dd",
        "yyyyMMdd",
        "yyyyMMddHHmmss"
    ];

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (DateTime.TryParseExact(str, Formats, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dt))
            {
                return dt;
            }

            // 兜底用系统自带的 DateTime.Parse
            return DateTime.TryParse(str, out dt) ? dt : throw new JsonException($"无法解析日期时间: {str}");
        }

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out var timestamp))
        {
            // 兼容 Unix 时间戳（秒）
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
        }

        throw new JsonException($"不支持的 TokenType {reader.TokenType} 解析为 DateTime");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // 统一序列化格式
        writer.WriteStringValue(value.ToString("yyyy-MM-dd HH:mm:ss"));
    }
}

