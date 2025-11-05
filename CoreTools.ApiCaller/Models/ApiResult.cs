using System.ComponentModel;
using System.Text.Json;

namespace CoreTools.ApiCaller.Models;

/// <summary>
/// ApiCaller接口返回结果包装类（System.Text.Json版本）
/// </summary>
public class ApiResult : IDisposable
{
    private bool isSet = false;
    private bool success;
    private int? code;
    private string message = string.Empty;
    private string rawStr = string.Empty;

    private JsonDocument? jsonObject;

    public ApiResult() { }

    public ApiResult(string resultStr)
    {
        RawStr = resultStr;
    }

    public ApiResult(bool success, string message)
    {
        this.success = success;
        this.message = message;
        this.isSet = true;
    }

    /// <summary>
    /// 执行结果
    /// </summary>
    public bool Success
    {
        get
        {
            if (!isSet)
            {
                success = TryGetValueAsBool(nameof(Success))
                       || TryGetValueAsBool("IsSuccess");
                isSet = true;
            }

            return success;
        }
        set
        {
            success = value;
            isSet = true;
        }
    }

    /// <summary>
    /// 接口原始返回字符串
    /// </summary>
    public string RawStr
    {
        get => rawStr;
        set
        {
            rawStr = value;
            jsonObject?.Dispose();
            jsonObject = null;
            isSet = false;
            code = null;
            message = string.Empty;
        }
    }

    /// <summary>
    /// 执行信息
    /// </summary>
    public string Message
    {
        get
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                message = this[nameof(Message)] ?? string.Empty;
            }

            return message;
        }
        set => message = value;
    }

    /// <summary>
    /// 返回状态码，默认 -1
    /// </summary>
    public int Code
    {
        get
        {
            if (!code.HasValue)
            {
                var codeStr = this[nameof(Code)];
                code = int.TryParse(codeStr, out var c) ? c : -1;
            }

            return code.Value;
        }
    }

    /// <summary>
    /// JsonDocument 对象（延迟解析，非法 JSON 时返回 null）
    /// </summary>
    public JsonDocument? JsonObject
    {
        get
        {
            if (jsonObject != null)
            {
                return jsonObject;
            }

            if (string.IsNullOrWhiteSpace(rawStr))
            {
                return null;
            }

            try
            {
                jsonObject = JsonDocument.Parse(rawStr, new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                });
            }
            catch (JsonException)
            {
                return null;
            }
            catch (Exception)
            {
                return null;
            }

            return jsonObject;
        }
    }

    /// <summary>
    /// 索引器，支持深层属性访问，忽略大小写
    /// </summary>
    public string this[string propertyName]
    {
        get
        {
            if (string.IsNullOrWhiteSpace(propertyName) || string.IsNullOrWhiteSpace(rawStr))
            {
                return string.Empty;
            }

            var parts = propertyName.Split('.');
            var element = JsonObject?.RootElement ?? default;

            if (element.ValueKind == JsonValueKind.Undefined)
            {
                return string.Empty;
            }

            foreach (var part in parts)
            {
                if (!TryGetPropertyIgnoreCase(element, part, out var child))
                {
                    return string.Empty;
                }

                element = child;
            }

            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? string.Empty,
                JsonValueKind.Number => element.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => string.Empty,
                _ => element.GetRawText()
            };
        }
    }

    /// <summary>
    /// 获取指定属性并转换类型，失败返回 defaultValue
    /// </summary>
    public T? GetValue<T>(string propertyName, T? defaultValue = default)
    {
        var value = this[propertyName];
        return TryConvert(value, out T? result) ? result : defaultValue;
    }

    /// <summary>
    /// 将原始结果反序列化为指定类型
    /// </summary>
    public T? TryConvert<T>(T? defaultValue = default)
    {
        if (string.IsNullOrWhiteSpace(rawStr))
        {
            return defaultValue;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(rawStr, JsonSetting.DEFAULT_SERIALIZER_OPTION);
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// 使用对象构建 ApiResult
    /// </summary>
    public static ApiResult Build(object obj)
    {
        return new ApiResult(JsonSerializer.Serialize(obj, JsonSetting.DEFAULT_SERIALIZER_OPTION));
    }

    /// <summary>
    /// 尝试类型转换
    /// </summary>
    private static bool TryConvert<T>(string input, out T result)
    {
        result = default!;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        if (targetType.IsEnum)
        {
            if (Enum.TryParse(targetType, input, true, out var enumVal))
            {
                result = (T)enumVal;
                return true;
            }

            return false;
        }

        if (targetType == typeof(Guid))
        {
            if (Guid.TryParse(input, out var guidVal))
            {
                result = (T)(object)guidVal;
                return true;
            }

            return false;
        }

        var converter = TypeDescriptor.GetConverter(targetType);
        if (converter != null && converter.IsValid(input))
        {
            result = (T)converter.ConvertFromString(input)!;
            return true;
        }

        try
        {
            result = (T)Convert.ChangeType(input, targetType);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 尝试获取 JsonElement 属性（忽略大小写）
    /// </summary>
    private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement child)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            child = default;
            return false;
        }

        foreach (var prop in element.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                child = prop.Value;
                return true;
            }
        }

        child = default;
        return false;
    }

    /// <summary>
    /// 尝试解析 bool
    /// </summary>
    private bool TryGetValueAsBool(string propertyName)
    {
        var str = this[propertyName];
        if (bool.TryParse(str, out var val))
        {
            return val;
        }

        if (int.TryParse(str, out var intVal))
        {
            return intVal != 0;
        }

        return false;
    }

    /// <summary>
    /// 释放 JsonDocument
    /// </summary>
    public void Dispose()
    {
        jsonObject?.Dispose();
        jsonObject = null;
        GC.SuppressFinalize(this);
    }
}
