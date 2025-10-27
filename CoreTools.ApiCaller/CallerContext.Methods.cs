using System.Text.Json;
using CoreTools.ApiCaller.Models.Config;

namespace CoreTools.ApiCaller;

public partial class CallerContext
{
    public override string ToString()
    {
        string paramJson;
        try
        {
            var options = ServiceItem.UseCamelCase
                                ? JsonSetting.CAMEL_CASE_POLICY_OPTION
                                : JsonSetting.DEFAULT_SERIALIZER_OPTION;
            paramJson = JsonSerializer.Serialize(OriginParam, options);
        }
        catch
        {
            paramJson = "[Unserializable Parameter]";
        }

        string result;
        try
        {
            result = ApiResult?.RawStr ?? "[No Result]";
        }
        catch
        {
            result = "[Unserializable Result]";
        }

        return $@"
-----------------------------------------------------------
|> TIME: {DateTime.Now:yyyy/MM/dd HH:mm:ss}
|> METHOD: {ServiceItem.Label}.{ApiItem.Label}
|> USE CAMEL CASE: {ServiceItem.UseCamelCase}
|> URL: {HttpMethod} {FinalUrl}
|> PARAM: {paramJson}
|> PARAM TYPE: {ApiItem?.ParamType}
|> RESULT: {result}
-----------------------------------------------------------";
    }
}
