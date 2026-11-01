using System.Collections.Generic;
using Fdw.Data.Abstractions.Rows;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.Rows;

public sealed class RowValuesTests
{
    private sealed class Brewery
    {
        public string BreweryId { get; set; } = string.Empty;

        public byte[]? Hash { get; set; }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ADictionaryRowIsReadByColumnName()
    {
        var row = new Dictionary<string, object?> { ["BreweryId"] = "b-1" };

        var found = RowValues.TryRead(row, "BreweryId", out var value, out var declaredType);

        found.ShouldBeTrue();
        value.ShouldBe("b-1");
        declaredType.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ADictionaryRowIsReadRegardlessOfCase()
    {
        var row = new Dictionary<string, object?> { ["breweryid"] = "b-1" };

        RowValues.TryRead(row, "BreweryId", out var value, out _).ShouldBeTrue();

        value.ShouldBe("b-1");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AReadOnlyDictionaryRowIsReadByColumnName()
    {
        IReadOnlyDictionary<string, object?> row = new ReadOnlyOnly("BreweryId", "b-1");

        RowValues.TryRead(row, "BreweryId", out var value, out _).ShouldBeTrue();

        value.ShouldBe("b-1");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AnEntityRowIsReadByPropertyAndDeclaresItsType()
    {
        var row = new Brewery { BreweryId = "b-1" };

        var found = RowValues.TryRead(row, "BreweryId", out var value, out var declaredType);

        found.ShouldBeTrue();
        value.ShouldBe("b-1");
        declaredType.ShouldBe(typeof(string));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ANullEntityValueIsFoundAndKeepsItsDeclaredType()
    {
        var row = new Brewery();

        var found = RowValues.TryRead(row, "Hash", out var value, out var declaredType);

        found.ShouldBeTrue();
        value.ShouldBeNull();
        declaredType.ShouldBe(typeof(byte[]));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ANullDictionaryValueIsFound()
    {
        var row = new Dictionary<string, object?> { ["BreweryId"] = null };

        RowValues.TryRead(row, "BreweryId", out var value, out _).ShouldBeTrue();

        value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AColumnTheRowLacksIsNotFound()
    {
        var dictionary = new Dictionary<string, object?> { ["Name"] = "x" };

        RowValues.TryRead(dictionary, "BreweryId", out var fromDictionary, out _).ShouldBeFalse();
        RowValues.TryRead(new Brewery(), "Name", out var fromEntity, out _).ShouldBeFalse();

        fromDictionary.ShouldBeNull();
        fromEntity.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ANullRowHasNoColumn()
    {
        RowValues.TryRead(null, "BreweryId", out var value, out _).ShouldBeFalse();

        value.ShouldBeNull();
    }

    // A row that is a read-only dictionary and not a mutable one, so the second dictionary shape is read.
    private sealed class ReadOnlyOnly : IReadOnlyDictionary<string, object?>
    {
        private readonly Dictionary<string, object?> _inner;

        public ReadOnlyOnly(string key, object? value) => _inner = new Dictionary<string, object?> { [key] = value };

        public object? this[string key] => _inner[key];

        public IEnumerable<string> Keys => _inner.Keys;

        public IEnumerable<object?> Values => _inner.Values;

        public int Count => _inner.Count;

        public bool ContainsKey(string key) => _inner.ContainsKey(key);

        public bool TryGetValue(string key, out object? value) => _inner.TryGetValue(key, out value);

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _inner.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
