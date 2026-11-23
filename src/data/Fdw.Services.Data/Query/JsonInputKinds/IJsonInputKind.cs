using System.Text.Json;
using Fdw.Collections;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>One way a calculation's input can be stated in JSON, and how it is read.</summary>
public interface IJsonInputKind : ITypeOption<int, JsonInputKindBase>
{
    /// <summary>Gets the JSON value kind this input kind reads.</summary>
    JsonValueKind ValueKind { get; }

    /// <summary>Reads a JSON value of this kind as the value the calculation takes.</summary>
    /// <param name="value">The JSON value.</param>
    /// <returns>The value, or null when the JSON value is of this kind but is not written as this input kind needs.</returns>
    object? Read(JsonElement value);
}
