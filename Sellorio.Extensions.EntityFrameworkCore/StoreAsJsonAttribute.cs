using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text.Json;

namespace Sellorio.Extensions.EntityFrameworkCore;

[AttributeUsage(AttributeTargets.Property)]
public sealed class StoreAsJsonAttribute : Attribute
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var propertyInfo in entityType.ClrType.GetProperties())
            {
                var thisAttribute = propertyInfo.GetCustomAttribute<StoreAsJsonAttribute>();

                if (thisAttribute != null)
                {
                    var converterType = typeof(ValueConverter<>).MakeGenericType(propertyInfo.PropertyType);
                    var converter = (Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter)converterType
                        .GetConstructors()
                        .First()
                        .Invoke([]);

                    if (entityType.IsOwned())
                    {
                        var property = entityType.FindProperty(propertyInfo.Name)
                            ?? entityType.AddProperty(propertyInfo.Name, propertyInfo.PropertyType, propertyInfo);

                        property.SetValueConverter(converter);
                    }
                    else
                    {
                        modelBuilder.Entity(entityType.ClrType).Property(propertyInfo.Name).HasConversion(converter);
                    }
                }
            }
        }
    }

    private class ValueConverter<TTarget> : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<TTarget, string>
    {
        public ValueConverter()
            : base(
                x => JsonSerializer.Serialize(x, default(JsonSerializerOptions)),
                x => JsonSerializer.Deserialize<TTarget>(x, default(JsonSerializerOptions))!)
        {
        }
    }
}