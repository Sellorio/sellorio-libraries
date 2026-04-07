using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sellorio.Clients.Rest;

internal static class Constants
{
    internal static JsonSerializerOptions DefaultJsonOptions { get; }

    static Constants()
    {
        DefaultJsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        DefaultJsonOptions.Converters.Add(new JsonStringEnumConverter());
    }
}
