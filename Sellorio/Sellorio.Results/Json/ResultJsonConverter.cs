using Sellorio.Results.Messages;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sellorio.Results.Json;

internal class ResultJsonConverter : JsonConverter<IResult?>
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(IResult).IsAssignableFrom(typeToConvert);
    }

    public override IResult? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var serialisableResult = JsonSerializer.Deserialize<SerialisableResult>(ref reader, options);

        if (serialisableResult == null)
        {
            return null;
        }

        if (serialisableResult.Messages == null)
        {
            throw new JsonException("Messages should not be null.");
        }

        var value = DeserializeValue(serialisableResult.Value, typeToConvert, options);
        var result = CreateResult(typeToConvert, value, serialisableResult);

        return result;
    }

    public override void Write(Utf8JsonWriter writer, IResult? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        var serialisableResult = new SerialisableResult
        {
            Messages = value.Messages.Select(ConvertToSerialisable).ToList(),
            Value = SerializeValue(value, options)
        };

        JsonSerializer.Serialize(writer, serialisableResult, options);
    }

    private static object? DeserializeValue(JsonElement jsonValue, Type resultType, JsonSerializerOptions options)
    {
        object? value = null;

        PerResultType(
            resultType,
            forResult: () => value = null,
            forResult1: () => value = null,
            forValueResult1: () => value = JsonSerializer.Deserialize(jsonValue, resultType.GetGenericArguments()[0], options),
            forValueResult2: () => value = JsonSerializer.Deserialize(jsonValue, resultType.GetGenericArguments()[1], options));

        return value;
    }

    private static IResult CreateResult(Type resultType, object? value, SerialisableResult serialisableResult)
    {
        IResult? result = null;

        PerResultType(
            resultType,
            forResult: () => result = Result.Create(serialisableResult.Messages!.Select(ConvertFromSerialisable).ToArray()),
            forResult1: () =>
                result =
                    (IResult)resultType.GetMethod(nameof(Result<object>.Create), BindingFlags.Static | BindingFlags.Public)!
                        .Invoke(null, [serialisableResult.Messages!.Select(ConvertFromSerialisable).ToArray()])!,
            forValueResult1: () =>
                result =
                    (IResult)(serialisableResult.Messages!.Any(x => x.Severity is ResultMessageSeverity.Critical or ResultMessageSeverity.Error or ResultMessageSeverity.NotFound)
                        ? resultType.GetMethod(nameof(ValueResult<object>.Failure), BindingFlags.Static | BindingFlags.Public, [typeof(ResultMessage[])])!
                            .Invoke(null, [serialisableResult.Messages!.Select(ConvertFromSerialisable).ToArray()])
                        : resultType.GetMethod(
                            nameof(ValueResult<object>.Success),
                            BindingFlags.Static | BindingFlags.Public,
                            [resultType.GetGenericArguments()[0], typeof(ResultMessage[])])!
                                .Invoke(null, [value, serialisableResult.Messages!.Select(ConvertFromSerialisable).ToArray()]))!,
            forValueResult2: () =>
                result =
                    (IResult)(serialisableResult.Messages!.Any(x => x.Severity is ResultMessageSeverity.Critical or ResultMessageSeverity.Error or ResultMessageSeverity.NotFound)
                        ? resultType.GetMethod(nameof(ValueResult<object, object>.Failure), BindingFlags.Static | BindingFlags.Public, [typeof(ResultMessage[])])!
                            .Invoke(null, [serialisableResult.Messages!.Select(ConvertFromSerialisable).ToArray()])
                        : resultType.GetMethod(
                            nameof(ValueResult<object, object>.Success),
                            BindingFlags.Static | BindingFlags.Public,
                            [resultType.GetGenericArguments()[1], typeof(ResultMessage[])])!
                                .Invoke(null, [value, serialisableResult.Messages!.Select(ConvertFromSerialisable).ToArray()]))!);

        return result ?? throw new InvalidOperationException("Failed to create result.");
    }

    private static JsonElement SerializeValue(IResult result, JsonSerializerOptions options)
    {
        JsonElement jsonValue = default;

        PerResultType(
            result.GetType(),
            forResult: () => jsonValue = JsonSerializer.SerializeToElement((object?)null),
            forResult1: () => jsonValue = JsonSerializer.SerializeToElement((object?)null),
            forValueResult1: () =>
            {
                var value = result.WasSuccess ? result.GetType().GetProperty(nameof(ValueResult<object>.Value))!.GetValue(result) : default;
                jsonValue = JsonSerializer.SerializeToElement(value, options);
            },
            forValueResult2: () =>
            {
                var value = result.WasSuccess ? result.GetType().GetProperty(nameof(ValueResult<object, object>.Value))!.GetValue(result) : default;
                jsonValue = JsonSerializer.SerializeToElement(value, options);
            });

        return jsonValue;
    }

    private static void PerResultType(Type resultType, Action forResult, Action forResult1, Action forValueResult1, Action forValueResult2)
    {
        if (resultType == typeof(Result))
        {
            forResult.Invoke();
            return;
        }

        var resultGenericType = resultType.GetGenericTypeDefinition();

        if (resultGenericType == typeof(Result<>))
        {
            forResult1.Invoke();
            return;
        }

        if (resultGenericType == typeof(ValueResult<>))
        {
            forValueResult1.Invoke();
            return;
        }
        else if (resultGenericType == typeof(ValueResult<,>))
        {
            forValueResult2.Invoke();
            return;
        }
        else
        {
            throw new InvalidOperationException("Unexpected result type. Do not use ResultJsonConverter on custom result types.");
        }
    }

    private static ResultMessage ConvertFromSerialisable(SerialisableResultMessage message)
    {
        if (message.Text == null)
        {
            throw new JsonException("Message text should not be null.");
        }

        return
            new ResultMessage(
                message.Text,
                message.Severity,
                message.Path?
                    .Select(x => new ResultMessagePathItem(x.Type, x.Value.ValueKind == JsonValueKind.String ? x.Value.GetString()! : x.Value.GetInt32()))
                    .ToImmutableArray());
    }

    private static SerialisableResultMessage ConvertToSerialisable(ResultMessage message)
    {
        if (message.Text == null)
        {
            throw new JsonException("Message text should not be null.");
        }

        return new SerialisableResultMessage
        {
            Text = message.Text,
            Severity = message.Severity,
            Path =
                message.Path?
                    .Select(x => new SerialisablePathItem { Type = x.Type, Value = JsonSerializer.SerializeToElement(x.Value) })
                    .ToImmutableArray()
        };
    }
}
