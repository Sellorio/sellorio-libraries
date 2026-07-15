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
            var entityBuilder = modelBuilder.Entity(entityType.ClrType);

            foreach (var propertyInfo in entityType.ClrType.GetProperties())
            {
                var thisAttribute = propertyInfo.GetCustomAttribute<StoreAsJsonAttribute>();

                if (thisAttribute != null)
                {
                    var propertyBuilder = entityBuilder.Property(propertyInfo.Name);
                    var converterType = typeof(ValueConverter<>).MakeGenericType(propertyInfo.PropertyType);
                    var converter = (Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter)converterType.GetConstructors().First().Invoke([]);

                    propertyBuilder.HasConversion(converter);
                }
            }
        }
    }

    private class ValueConverter<TTarget> : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<TTarget, string>
    {
        public ValueConverter()
            : base(x => JsonSerializer.Serialize(x, default(JsonSerializerOptions)), x => JsonSerializer.Deserialize<TTarget>(x, default(JsonSerializerOptions))!)
        {
        }
    }
}
