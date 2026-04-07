using System.Text.Json.Serialization;
using System.Text.Json;
using System.Collections.Generic;

namespace Sellorio.Results.Json;

internal class SerialisableResult
{
    [JsonPropertyName("m")]
    public required IEnumerable<SerialisableResultMessage> Messages { get; init; }

    [JsonPropertyName("v")]
    public required JsonElement Value { get; init; }
}