using System;
using System.Collections.Generic;
using System.Text.Json;
using Fdw.Services.Etl.Graphs;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Reads the single value of a graph setting out of the JSON a request states (FDW-1396, FDW-1403), by the kind the setting
/// binding says it holds, for components and steps alike.
/// </summary>
internal static class GraphSettingValue
{
    /// <summary>Reads a value of a kind.</summary>
    /// <param name="kind">What the setting holds: <c>Text</c>, <c>Number</c> or <c>Boolean</c>.</param>
    /// <param name="value">The JSON the request states.</param>
    /// <returns>The typed value, or null when the JSON is not that kind of value.</returns>
    internal static object? Read(string kind, JsonElement value) => GraphSettingKinds.ByName(kind).ReadJson(value);

    /// <summary>
    /// Reads a setting that takes its value from a run parameter (FDW-1722): an object that names the parameter, <c>{ "parameter": "Name" }</c>,
    /// where a value would be.
    /// </summary>
    /// <param name="value">The JSON the request states.</param>
    /// <returns>The parameter's name, or null when the JSON is not such an object.</returns>
    internal static string? ParameterOf(JsonElement value) =>
        value.ValueKind == JsonValueKind.Object
        && value.TryGetProperty("parameter", out var name)
        && name.ValueKind == JsonValueKind.String
        && name.GetString() is { Length: > 0 } text
        && !string.IsNullOrWhiteSpace(text)
            ? text
            : null;

    /// <summary>
    /// Reads the value of a declared setting as the invariant text it is stored as (FDW-6133): a string as it is, a number as JSON writes it, and true or
    /// false. The text is read as the setting's data type when the graph is judged.
    /// </summary>
    /// <param name="value">The JSON the request states.</param>
    /// <returns>The text, or null when the JSON is not a single value.</returns>
    internal static string? InvariantText(JsonElement value) =>
        ReferenceEquals(JsonScalarTexts.ByValueKind(value.ValueKind), JsonScalarTexts.NotFound) ? null : JsonScalarTexts.ByValueKind(value.ValueKind).InvariantText(value);

    /// <summary>Writes a setting that takes its value from a run parameter, as <see cref="ParameterOf"/> reads it.</summary>
    /// <param name="parameter">The parameter's name.</param>
    /// <returns>The object to state where a value would be.</returns>
    internal static Dictionary<string, object?> Bound(string parameter) => new(StringComparer.Ordinal) { ["parameter"] = parameter };

    /// <summary>Says what a kind of value is, for a refusal that names what the setting must be.</summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The kind in words.</returns>
    internal static string Article(string kind) => GraphSettingKinds.ByName(kind).Words is { Length: > 0 } words ? words : kind;
}
