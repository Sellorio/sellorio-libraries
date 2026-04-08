namespace Sellorio.AutoMapping;

public interface IMap<TFrom, TTo>
{
    TTo Map(TFrom from);
}
