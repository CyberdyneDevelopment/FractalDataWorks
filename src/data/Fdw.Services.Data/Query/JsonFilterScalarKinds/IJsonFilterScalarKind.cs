using System.Text.Json;
using Fdw.Collections;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>One kind of JSON value a filter can carry as a scalar, and how it is read.</summary>
public interface IJsonFilterScalarKind : ITypeOption<int, JsonFilterScalarKindBase>
{
    /// <summary>Gets the JSON value kind this reads.</summary>
    JsonValueKind ValueKind { get; }

    /// <summary>Reads a JSON value of this kind.</summary>
    /// <param name="value">The JSON value.</param>
    /// <returns>The value, or null when it is of this kind but outside the range a filter can carry.</returns>
    object? Read(JsonElement value);
}
