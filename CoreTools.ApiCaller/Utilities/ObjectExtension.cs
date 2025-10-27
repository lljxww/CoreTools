using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CoreTools.ApiCaller.Utilities;

public static class ObjectExtension
{
    public static Dictionary<string, string> AsDictionary(this object source)
    {
        if (source == null)
        {
            return [];
        }

        // 已经是 Dictionary<string, string>
        if (source is Dictionary<string, string> dict)
        {
            return dict;
        }

        // JsonElement（对象）
        if (source is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
        {
            var result = new Dictionary<string, string>();
            foreach (var prop in jsonElement.EnumerateObject())
            {
                result[prop.Name] = prop.Value.ToString();
            }

            return result;
        }

        // JsonDocument
        if (source is JsonDocument jsonDoc && jsonDoc.RootElement.ValueKind == JsonValueKind.Object)
        {
            return jsonDoc.RootElement.AsDictionary();
        }

        // JsonObject (System.Text.Json.Nodes)
        if (source is JsonObject jsonObj)
        {
            var result = new Dictionary<string, string>();
            foreach (var kvp in jsonObj)
            {
                result[kvp.Key] = kvp.Value?.ToJsonString() ?? string.Empty;
            }

            return result;
        }

        // JsonNode (可能是 JsonObject, JsonValue, JsonArray)
        if (source is JsonNode jsonNode)
        {
            return jsonNode is JsonObject obj
                ? obj.AsDictionary()
                : new Dictionary<string, string>
                {
                    ["Value"] = jsonNode.ToJsonString()
                };
        }

        // 普通对象
        return source.GetType()
                     .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(p => p.CanRead)
                     .ToDictionary(
                         propInfo => propInfo.Name,
                         propInfo => propInfo.GetValue(source)?.ToString() ?? string.Empty
                     );
    }
}
