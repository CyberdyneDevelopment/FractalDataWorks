using System;
using System.Collections.Generic;
using System.Text.Json;

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
    internal static object? Read(string kind, JsonElement value) =>
        kind switch
        {
            "Text" => value.ValueKind == JsonValueKind.String ? value.GetString() : null,
            "Number" => value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) ? number : null,
            "Boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : null,
            _ => null,
        };

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

    /// <summary>Writes a setting that takes its value from a run parameter, as <see cref="ParameterOf"/> reads it.</summary>
    /// <param name="parameter">The parameter's name.</param>
    /// <returns>The object to state where a value would be.</returns>
    internal static Dictionary<string, object?> Bound(string parameter) => new(StringComparer.Ordinal) { ["parameter"] = parameter };

    /// <summary>Says what a kind of value is, for a refusal that names what the setting must be.</summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The kind in words.</returns>
    internal static string Article(string kind) => kind switch
    {
        "Text" => "text",
        "Number" => "a whole number",
        "Boolean" => "true or false",
        _ => kind,
    };
}
