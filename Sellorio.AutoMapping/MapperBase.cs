using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Sellorio.AutoMapping;

public abstract class MapperBase
{
    internal Dictionary<Type, (object Mapper, MethodInfo MapMethod)> MapMethods { get; } = [];

    protected MapperBase(params object[] additionalMappers)
    {
        foreach (var mapper in additionalMappers.OfType<MapperBase>())
        {
            foreach (var map in mapper.MapMethods)
            {
                MapMethods[map.Key] = map.Value;
            }
        }

        var mapImplementations = GetType().GetInterfaces().Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IMap<,>)).ToArray();

        foreach (var mapImplementation in mapImplementations)
        {
            MapMethods[mapImplementation] = (this, mapImplementation.GetMethod(nameof(IMap<,>.Map)))!;
        }
    }

    protected TTo Map<TTo>(object from)
    {
        return (TTo)Map(from, typeof(TTo), true)!;
    }

    private object? Map(object? from, Type toType, bool calledFromMapMethod)
    {
        if (from == null)
        {
            return ConstructDefault(toType);
        }

        var fromType = from.GetType();

        if (fromType.IsGenericType && fromType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var hasValue = (bool)fromType.GetProperty(nameof(Nullable<>.HasValue))!.GetValue(from)!;

            if (!hasValue)
            {
                return ConstructDefault(toType);
            }

            var fromValue = fromType.GetProperty(nameof(Nullable<>.Value))!.GetValue(from)!;

            return hasValue ? Map(fromValue, toType, false) : ConstructDefault(toType);
        }

        if (!calledFromMapMethod) // avoid infinite recursive loop
        {
            var mapType = typeof(IMap<,>).MakeGenericType(fromType, toType);

            if (MapMethods.TryGetValue(mapType, out var map))
            {
                return map.MapMethod.Invoke(map.Mapper, [from]);
            }
        }

        return AutoMap(from, toType);
    }

    private object AutoMap(object from, Type toType)
    {
        var fromType = from.GetType();

        if (toType.IsAssignableFrom(fromType))
        {
            return from;
        }

        if (typeof(IConvertible).IsAssignableFrom(fromType))
        {
            var fromConvertible = (IConvertible)from;

            if (TryAutoMapConvertible(fromConvertible, toType, out var to))
            {
                return to!;
            }
        }

        if (fromType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(fromType))
        {
            return
                toType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(toType)
                    ? AutoMapEnumerable(from, toType)
                    : throw new InvalidOperationException("Incompatible mapping from IEnumerable to non-IEnumerable types.");
        }

        return AutoMapObject(from, fromType, toType);
    }

    private static bool TryAutoMapConvertible(IConvertible from, Type toType, out object? to)
    {
        if (toType == typeof(bool))
        {
            to = from.ToBoolean(null);
        }
        else if (toType == typeof(char))
        {
            to = from.ToChar(null);
        }
        else if (toType == typeof(sbyte))
        {
            to = from.ToSByte(null);
        }
        else if (toType == typeof(byte))
        {
            to = from.ToByte(null);
        }
        else if (toType == typeof(short))
        {
            to = from.ToInt16(null);
        }
        else if (toType == typeof(ushort))
        {
            to = from.ToUInt16(null);
        }
        else if (toType == typeof(int))
        {
            to = from.ToInt32(null);
        }
        else if (toType == typeof(uint))
        {
            to = from.ToUInt32(null);
        }
        else if (toType == typeof(long))
        {
            to = from.ToInt64(null);
        }
        else if (toType == typeof(ulong))
        {
            to = from.ToUInt64(null);
        }
        else if (toType == typeof(float))
        {
            to = from.ToSingle(null);
        }
        else if (toType == typeof(double))
        {
            to = from.ToDouble(null);
        }
        else if (toType == typeof(DateTime))
        {
            to = from.ToDateTime(null);
        }
        else
        {
            to = null;
            return false;
        }

        return true;
    }

    private object AutoMapEnumerable(object from, Type toType)
    {
        var fromItems = ((IEnumerable)from).Cast<object>().ToArray();
        var itemCount = fromItems.Length;

        var to = ConstructEnumerable(toType, itemCount, out var itemType);
        var index = 0;

        foreach (var fromItem in fromItems)
        {
            var toItem = itemType == null ? fromItem : Map(fromItem, itemType, false)!;
            AppendItem(to, ref index, toItem);
        }

        return to;
    }

    private object AutoMapObject(object from, Type fromType, Type toType)
    {
        var to = Activator.CreateInstance(toType)!;

        var fromProperties = fromType.GetProperties();
        var toProperties = toType.GetProperties();

        foreach (var fromProperty in fromProperties)
        {
            var toProperty = toProperties.FirstOrDefault(x => x.Name == fromProperty.Name);

            if (toProperty == null)
            {
                continue;
            }

            var fromPropertyValue = fromProperty.GetValue(from);
            var toPropertyValue = Map(fromPropertyValue, toProperty.PropertyType, false)!;

            toProperty.SetValue(to, toPropertyValue);
        }

        return to;
    }

    private static object? ConstructDefault(Type type)
    {
        return
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) || !type.IsValueType
                ? null
                : Activator.CreateInstance(type);
    }

    private static object ConstructEnumerable(Type type, int itemCount, out Type? itemType)
    {
        if (type == typeof(IEnumerable) ||
            type == typeof(ICollection) ||
            type == typeof(Array))
        {
            itemType = null;
            return new object[itemCount];
        }

        if (type == typeof(IList))
        {
            itemType = null;
            return new List<object>();
        }

        if (type.IsArray)
        {
            itemType = type.GetElementType();
            return Array.CreateInstance(itemType ?? typeof(object), itemCount);
        }

        if (type.IsGenericType)
        {
            var genericTypeDefinition = type.GetGenericTypeDefinition();

            if (genericTypeDefinition == typeof(IEnumerable<>))
            {
                itemType = type.GetGenericArguments()[0];
                return Array.CreateInstance(itemType, itemCount);
            }

            if (genericTypeDefinition == typeof(ICollection<>) ||
                genericTypeDefinition == typeof(Collection<>))
            {
                itemType = type.GetGenericArguments()[0];
                return Activator.CreateInstance(typeof(Collection<>).MakeGenericType(itemType))!;
            }

            if (genericTypeDefinition == typeof(IList<>) ||
                genericTypeDefinition == typeof(List<>))
            {
                itemType = type.GetGenericArguments()[0];
                return Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType))!;
            }
        }

        if (!typeof(IList).IsAssignableFrom(type))
        {
            throw new InvalidOperationException("Cannot create instance of custom enumerable that doesn't implement IList.");
        }

        var listInterface = type.GetInterfaces().FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IList<>));
        itemType = listInterface?.GetGenericArguments()[0];

        return Activator.CreateInstance(type)!;
    }

    private static void AppendItem(object to, ref int index, object item)
    {
        if (to is Array array)
        {
            array.SetValue(item, index);
            index++;
        }
        else if (to is IList list)
        {
            list.Add(item);
            index++;
        }
        else
        {
            throw new InvalidOperationException("Unable to append item to destination list. Unexpected enumerable type encountered.");
        }
    }
}
