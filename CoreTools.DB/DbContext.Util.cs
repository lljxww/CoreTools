using System.Text.Json;

namespace CoreTools.DB;

public partial class DbContext
{
    private static T Clone<T>(T instance)
    {
        string json = JsonSerializer.Serialize(instance, jsonSerializerOptions);

        try
        {
            T newInstance = JsonSerializer.Deserialize<T>(json, jsonSerializerOptions)!;
            return newInstance;
        }
        catch (Exception ex)
        {
            ErrorLogHandler?.Invoke(ex);
            throw;
        }
    }
}
