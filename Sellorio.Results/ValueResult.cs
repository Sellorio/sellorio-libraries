namespace Sellorio.Results;

public static class ValueResult
{
    public static ValueResult<TValue> Success<TValue>(TValue value)
    {
        return ValueResult<TValue>.Success(value);
    }
}
