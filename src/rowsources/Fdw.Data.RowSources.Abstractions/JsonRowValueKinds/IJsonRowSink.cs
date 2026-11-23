using System.Text.Json;

namespace Fdw.Data.RowSources.Json.Abstractions;

/// <summary>Where a JSON value kind puts what it reads: a field value, or a nested object or array to flatten into the row.</summary>
public interface IJsonRowSink
{
    /// <summary>Adds a field to the current row.</summary>
    /// <param name="name">The flattened field name.</param>
    /// <param name="value">The field's value.</param>
    void AddValue(string name, object? value);

    /// <summary>Flattens a nested object into the row.</summary>
    /// <param name="element">The object.</param>
    /// <param name="prefix">The field name the object's properties are named under.</param>
    void FlattenObject(JsonElement element, string prefix);

    /// <summary>Flattens a nested array into the row, one field per element.</summary>
    /// <param name="array">The array.</param>
    /// <param name="name">The field name the elements are named under.</param>
    void FlattenArray(JsonElement array, string name);
}
