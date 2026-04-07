
using System;
using System.Collections.Generic;

namespace Sellorio.Results.Messages;

public class ResultMessagePathItem : IEquatable<ResultMessagePathItem>
{
    public ResultMessagePathItemType Type { get; }
    public object Value { get; }

    internal ResultMessagePathItem(ResultMessagePathItemType type, object value)
    {
        Type = type;
        Value = value;
    }

    public override bool Equals(object? obj)
    {
        return base.Equals(obj as ResultMessagePathItem);
    }

    public bool Equals(ResultMessagePathItem? other)
    {
        return other != null && Type == other.Type && Value.Equals(other.Value);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, Value);
    }

    public static bool operator ==(ResultMessagePathItem? left, ResultMessagePathItem? right)
    {
        return EqualityComparer<ResultMessagePathItem>.Default.Equals(left, right);
    }

    public static bool operator !=(ResultMessagePathItem? left, ResultMessagePathItem? right)
    {
        return !(left == right);
    }
}
