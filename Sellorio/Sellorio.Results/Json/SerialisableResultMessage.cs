using Sellorio.Results.Messages;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sellorio.Results.Json;

internal class SerialisableResultMessage
{
    [JsonPropertyName("p")]
    public required IEnumerable<SerialisablePathItem>? Path { get; init; }

    [JsonPropertyName("s")]
    public required ResultMessageSeverity Severity { get; init; }

    [JsonPropertyName("t")]
    public required string Text { get; init; }
}
