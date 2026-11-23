using System.Text.Json;
using Fdw.Collections;

namespace Fdw.Services.Etl.Graphs;

/// <summary>A kind of single JSON value and how its invariant text is read.</summary>
public interface IJsonScalarText : ITypeOption<int, JsonScalarTextBase>
{
    /// <summary>Gets the JSON value kind this option reads.</summary>
    JsonValueKind ValueKind { get; }

    /// <summary>Reads the value as invariant text.</summary>
    /// <param name="value">The JSON value, of this option's kind.</param>
    /// <returns>The text.</returns>
    string? InvariantText(JsonElement value);
}
