using System.Text.Json.Serialization;
using System.Text.Json;
using Sellorio.Results.Messages;

namespace Sellorio.Results.Json;

internal class SerialisablePathItem
{
    [JsonPropertyName("v")]
    public required JsonElement Value { get; init; }

    [JsonPropertyName("t")]
    public required ResultMessagePathItemType Type { get; init; }
}
