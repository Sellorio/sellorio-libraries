using System.Collections;
using System.Collections.ObjectModel;

namespace Sellorio.AutoMapping.Tests;

public class MapperBaseTests
{
    private static readonly int[] _oneTwoThree = [1, 2, 3];
    private static readonly int[] _sevenEightNine = [7, 8, 9];

    [Fact]
    public void Convert_ReturnsNull_WhenSourceIsNullAndDestinationIsReferenceType()
    {
        var mapper = new TestMapper();

        var result = mapper.Convert<PersonDestination>(null);

        Assert.Null(result);
    }

    [Fact]
    public void Convert_ReturnsDefault_WhenSourceIsNullAndDestinationIsValueType()
    {
        var mapper = new TestMapper();

        var result = mapper.Convert<int>(null);

        Assert.Equal(0, result);
    }

    [Fact]
    public void Convert_ReturnsSameInstance_WhenDestinationTypeIsAssignableFromSourceType()
    {
        var mapper = new TestMapper();
        var source = new PersonDestination
        {
            Name = "Alice",
            Age = 42,
        };

        var result = mapper.Convert<PersonDestination>(source);

        Assert.Same(source, result);
    }

    [Fact]
    public void Convert_MapsConvertibleValues()
    {
        var mapper = new TestMapper();

        var longResult = mapper.Convert<long>(123);
        var doubleResult = mapper.Convert<double>(123);
        var boolResult = mapper.Convert<bool>(1);

        Assert.Equal(123L, longResult);
        Assert.Equal(123d, doubleResult);
        Assert.True(boolResult);
    }

    [Fact]
    public void Convert_MapsObjectsRecursively_ByMatchingPropertyNames()
    {
        var mapper = new TestMapper();
        var source = new PersonSource
        {
            Name = "Alice",
            Age = 42,
            Home = new AddressSource
            {
                Street = "Main St",
                Number = 12,
            },
            Scores = [1, 2, 3],
        };

        var result = mapper.Convert<PersonDestination>(source);

        Assert.NotNull(result);
        Assert.Equal("Alice", result.Name);
        Assert.Equal(42L, result.Age);
        Assert.NotNull(result.Home);
        Assert.Equal("Main St", result.Home.Street);
        Assert.Equal(12L, result.Home.Number);
        Assert.NotNull(result.Scores);
        Assert.Equal(new long[] { 1, 2, 3 }, result.Scores);
        Assert.Null(result.Unmatched);
    }

    [Fact]
    public void Convert_UsesAdditionalMapper_ForNestedMappings()
    {
        var mapper = new TestMapper(new MoneyMapper());
        var source = new InvoiceSource
        {
            Total = new MoneySource
            {
                Cents = 1234,
                Currency = "usd",
            },
        };

        var result = mapper.Convert<InvoiceDestination>(source);

        Assert.NotNull(result.Total);
        Assert.Equal(12.34m, result.Total.Amount);
        Assert.Equal("USD", result.Total.Currency);
    }

    [Fact]
    public void Convert_UsesAdditionalMapper_ToPreserveCircularReferences()
    {
        var mapper = new TestMapper(new CircularNodeMapper());
        var root = new CircularNodeSource
        {
            Name = "Root",
        };
        var child = new CircularNodeSource
        {
            Name = "Child",
            Parent = root,
        };

        root.Child = child;

        var result = mapper.Convert<CircularNodeContainerDestination>(new CircularNodeContainerSource
        {
            Root = root,
        });

        Assert.NotNull(result.Root);
        Assert.Equal("Root", result.Root.Name);
        Assert.NotNull(result.Root.Child);
        Assert.Equal("Child", result.Root.Child.Name);
        Assert.Same(result.Root, result.Root.Child.Parent);
    }

    [Fact]
    public void Convert_MapsEnumerables_ToRequestedCollectionTypes()
    {
        var mapper = new TestMapper();

        var arrayResult = mapper.Convert<long[]>(_oneTwoThree);
        var collectionResult = mapper.Convert<Collection<long>>(new List<int> { 4, 5, 6 });
        var customListResult = mapper.Convert<CustomLongList>(_sevenEightNine);

        Assert.Equal(new long[] { 1, 2, 3 }, arrayResult);
        Assert.Equal(new long[] { 4, 5, 6 }, collectionResult);
        Assert.IsType<CustomLongList>(customListResult);
        Assert.Equal(new long[] { 7, 8, 9 }, customListResult);
    }

    [Fact]
    public void Convert_ReturnsSourceEnumerable_WhenDestinationIsNonGenericEnumerableInterface()
    {
        var mapper = new TestMapper();
        var source = new[] { 1, 2, 3 };

        var result = mapper.Convert<IEnumerable>(source);

        var values = result.Cast<object>().ToArray();

        Assert.Same(source, result);
        Assert.Equal(new object[] { 1, 2, 3 }, values);
    }

    [Fact]
    public void Convert_Throws_WhenMappingEnumerableToNonEnumerableDestination()
    {
        var mapper = new TestMapper();

        var exception = Assert.Throws<InvalidOperationException>(() => mapper.Convert<PersonDestination>(_oneTwoThree));

        Assert.Equal("Incompatible mapping from IEnumerable to non-IEnumerable types.", exception.Message);
    }

    [Fact]
    public void Convert_Throws_WhenDestinationEnumerableDoesNotImplementIList()
    {
        var mapper = new TestMapper();

        var exception = Assert.Throws<InvalidOperationException>(() => mapper.Convert<UnsupportedEnumerable<long>>(_oneTwoThree));

        Assert.Equal("Cannot create instance of custom enumerable that doesn't implement IList.", exception.Message);
    }

    private sealed class TestMapper(params object[] additionalMappers) : MapperBase(additionalMappers)
    {
        public TTo Convert<TTo>(object? source)
        {
            return Map<TTo>(source!);
        }
    }

    private sealed class CircularNodeMapper : MapperBase, IMap<CircularNodeSource, CircularNodeDestination>
    {
        private readonly Dictionary<CircularNodeSource, CircularNodeDestination> _mappedNodes = [];

        public CircularNodeMapper()
            : base()
        {
        }

        public CircularNodeDestination Map(CircularNodeSource from)
        {
            if (_mappedNodes.TryGetValue(from, out var existing))
            {
                return existing;
            }

            var result = new CircularNodeDestination
            {
                Name = from.Name,
            };

            _mappedNodes[from] = result;
            result.Parent = from.Parent == null ? null : Map(from.Parent);
            result.Child = from.Child == null ? null : Map(from.Child);

            return result;
        }
    }

    private sealed class MoneyMapper : MapperBase, IMap<MoneySource, MoneyDestination>
    {
        public MoneyMapper()
            : base()
        {
        }

        public MoneyDestination Map(MoneySource from)
        {
            return new MoneyDestination
            {
                Amount = from.Cents / 100m,
                Currency = from.Currency.ToUpperInvariant(),
            };
        }
    }

    private sealed class PersonSource
    {
        public string? Name { get; set; }

        public int Age { get; set; }

        public AddressSource? Home { get; set; }

        public int[]? Scores { get; set; }
    }

    private sealed class PersonDestination
    {
        public string? Name { get; set; }

        public long Age { get; set; }

        public AddressDestination? Home { get; set; }

        public long[]? Scores { get; set; }

        public string? Unmatched { get; set; }
    }

    private sealed class AddressSource
    {
        public string? Street { get; set; }

        public int Number { get; set; }
    }

    private sealed class AddressDestination
    {
        public string? Street { get; set; }

        public long Number { get; set; }
    }

    private sealed class InvoiceSource
    {
        public MoneySource? Total { get; set; }
    }

    private sealed class InvoiceDestination
    {
        public MoneyDestination? Total { get; set; }
    }

    private sealed class MoneySource
    {
        public int Cents { get; set; }

        public string Currency { get; set; } = string.Empty;
    }

    private sealed class MoneyDestination
    {
        public decimal Amount { get; set; }

        public string Currency { get; set; } = string.Empty;
    }

    private sealed class CircularNodeContainerSource
    {
        public CircularNodeSource? Root { get; set; }
    }

    private sealed class CircularNodeContainerDestination
    {
        public CircularNodeDestination? Root { get; set; }
    }

    private sealed class CircularNodeSource
    {
        public string? Name { get; set; }

        public CircularNodeSource? Parent { get; set; }

        public CircularNodeSource? Child { get; set; }
    }

    private sealed class CircularNodeDestination
    {
        public string? Name { get; set; }

        public CircularNodeDestination? Parent { get; set; }

        public CircularNodeDestination? Child { get; set; }
    }

    private sealed class CustomLongList : List<long>
    {
    }

    private sealed class UnsupportedEnumerable<T> : IEnumerable<T>
    {
        public IEnumerator<T> GetEnumerator()
        {
            return Enumerable.Empty<T>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
