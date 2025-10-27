using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreTools.DB;

public class FlexibleDateTimeConverter : JsonConverter<DateTime>
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
            string? str = reader.GetString();
            if (DateTime.TryParseExact(str, Formats, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateTime dt))
            {
                return dt;
            }

            // 兜底用系统自带的 DateTime.Parse
            return DateTime.TryParse(str, out dt) ? dt : throw new JsonException($"无法解析日期时间: {str}");
        }

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out long timestamp))
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
