using System.ComponentModel;
using System.Text.Json;

namespace CoreTools.ApiCaller.Models;

/// <summary>
/// ApiCaller接口返回结果包装类（System.Text.Json版本）
/// </summary>
[Serializable]
public class ApiResult
{
    private bool isSet = false;
    private bool success = false;

    /// <summary>
    /// 执行结果(试用,实际情况以RawStr自行判断)
    /// </summary>
    public bool Success
    {
        get
        {
            if (isSet)
            {
                return success;
            }

            try
            {
                return Convert.ToBoolean(this[nameof(Success)]);
            }
            catch
            {
                try
                {
                    return Convert.ToBoolean(this["IsSuccess"]);
                }
                catch
                {
                    return false;
                }
            }
        }
        set
        {
            success = value;
            isSet = true;
        }
    }

    private string rawStr = string.Empty;

    /// <summary>
    /// 接口的原始返回结果
    /// </summary>
    public string RawStr
    {
        get => rawStr;
        set
        {
            try
            {
                JsonObject = JsonDocument.Parse(value);
            }
            catch
            {
                JsonObject = default;
            }

            rawStr = value;
        }
    }

    private string message = string.Empty;

    /// <summary>
    /// 执行信息
    /// </summary>
    public string Message
    {
        get
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                try
                {
                    return this[nameof(Message)];
                }
                catch
                {
                    return string.Empty;
                }
            }
            else
            {
                return message;
            }
        }
        set => message = value;
    }

    public int Code
    {
        get
        {
            var codeStr = this[nameof(Code)];
            return string.IsNullOrWhiteSpace(codeStr) ? -1 : int.Parse(codeStr);
        }
    }

    public ApiResult(string resultStr)
    {
        RawStr = resultStr;
    }

    public ApiResult(bool success, string message)
    {
        this.success = success;
        this.message = message;
    }

    public ApiResult()
    {
    }

    [NonSerialized]
    public JsonDocument? JsonObject;

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement child)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    child = prop.Value;
                    return true;
                }
            }
        }

        child = default;
        return false;
    }

    /// <summary>
    /// 返回结果索引器
    /// </summary>
    public string this[string propertyName]
    {
        get
        {
            try
            {
                JsonObject ??= JsonDocument.Parse(rawStr);

                var parts = propertyName.Split('.');
                var element = JsonObject.RootElement;

                foreach (var part in parts)
                {
                    if (TryGetPropertyIgnoreCase(element, part, out var child))
                    {
                        element = child;
                    }
                    else
                    {
                        return string.Empty;
                    }
                }

                return element.ValueKind switch
                {
                    JsonValueKind.String => element.GetString(),
                    JsonValueKind.Number => element.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => element.GetRawText()
                } ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    public T? GetValue<T>(string propertyName, T? defaultValue = default)
    {
        var value = this[propertyName];
        return TryConvert<T>(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// 将Result的原始字符串反序列化为指定的格式
    /// </summary>
    public T? TryConvert<T>(T? defaultValue = default)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(RawStr, JsonSetting.DEFAULT_SERIALIZER_OPTION);
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// 使用对象构建ApiResult实例
    /// </summary>
    public static ApiResult Build(object obj)
    {
        return new ApiResult(JsonSerializer.Serialize(obj, JsonSetting.DEFAULT_SERIALIZER_OPTION));
    }

    private static bool TryConvert<T>(string input, out T result)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                result = default!;
                return false;
            }

            var targetType = typeof(T);

            // 如果是 Nullable<T>，取里面的实际类型
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            // 特殊处理枚举
            if (underlyingType.IsEnum)
            {
                if (Enum.TryParse(underlyingType, input, true, out var enumValue))
                {
                    result = (T)enumValue;
                    return true;
                }

                result = default!;
                return false;
            }

            // 特殊处理 Guid
            if (underlyingType == typeof(Guid))
            {
                if (Guid.TryParse(input, out var guidValue))
                {
                    result = (T)(object)guidValue;
                    return true;
                }

                result = default!;
                return false;
            }

            // 尝试用 TypeConverter
            var converter = TypeDescriptor.GetConverter(underlyingType);
            if (converter != null && converter.IsValid(input))
            {
                result = (T)converter.ConvertFromString(input)!;
                return true;
            }

            // 最后尝试 ChangeType
            result = (T)Convert.ChangeType(input, underlyingType);
            return true;
        }
        catch
        {
            result = default!;
            return false;
        }
    }
}
