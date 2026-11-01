using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Fdw.Services.Workspaces.Charts;

/// <summary>
/// Reads a saved chart's <c>Encoding</c>: a JSON object with <c>bindings</c> (slot name to a field name, an
/// object <c>{"field": ..., "aggregate": ...}</c>, or a list of them) and optionally <c>options</c> (an object the chart's own drawing reads, not checked here).
/// </summary>
/// <remarks>
/// Strict on purpose: a key it does not know is refused, not ignored, because a typo such as <c>binding</c>
/// would otherwise save a chart with nothing bound. A slot set to null is unbound, which is how a form that
/// has the slot but no field yet writes it.
/// </remarks>
public static class ChartEncodingReader
{
    /// <summary>Reads an encoding.</summary>
    /// <param name="encoding">The encoding as stored.</param>
    /// <returns>The bindings, or the reason it cannot be read.</returns>
    public static (ChartEncoding? Encoding, string? Reason) Read(string encoding)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(encoding);
        }
        catch (JsonException ex)
        {
            return (null, $"Encoding is not valid JSON: {ex.Message}");
        }

        using (document)
        {
            var reason = Shape(document.RootElement);
            return reason is null ? Bindings(document.RootElement.GetProperty("bindings")) : (null, reason);
        }
    }

    private static string? Shape(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return "Encoding must be a JSON object with 'bindings' and, optionally, 'options'";

        foreach (var property in root.EnumerateObject())
        {
            if (!string.Equals(property.Name, "bindings", StringComparison.Ordinal) && !string.Equals(property.Name, "options", StringComparison.Ordinal))
                return $"Encoding has a key '{property.Name}' it does not know; it takes 'bindings' and 'options'";
        }

        if (!root.TryGetProperty("bindings", out var bindings) || bindings.ValueKind != JsonValueKind.Object)
            return "Encoding must have 'bindings', an object of slot names to bindings";

        return root.TryGetProperty("options", out var options) && options.ValueKind != JsonValueKind.Object && options.ValueKind != JsonValueKind.Null
            ? "Encoding 'options' must be an object"
            : null;
    }

    private static (ChartEncoding? Encoding, string? Reason) Bindings(JsonElement bindings)
    {
        var bound = new Dictionary<string, IReadOnlyList<ChartBinding>>(StringComparer.OrdinalIgnoreCase);
        foreach (var slot in bindings.EnumerateObject())
        {
            var found = Slot(slot.Name, slot.Value);
            if (found.Reason is not null) return (null, found.Reason);
            if (found.Bindings!.Count == 0) continue;
            if (!bound.TryAdd(slot.Name, found.Bindings)) return (null, $"slot '{slot.Name}' is bound twice");
        }

        return (new ChartEncoding(bound), null);
    }

    private static (List<ChartBinding>? Bindings, string? Reason) Slot(string slot, JsonElement value)
    {
        var bound = new List<ChartBinding>();
        if (value.ValueKind == JsonValueKind.Null) return (bound, null);

        var elements = value.ValueKind == JsonValueKind.Array ? value.EnumerateArray().ToList() : new List<JsonElement> { value };
        foreach (var element in elements)
        {
            var one = One(slot, element);
            if (one.Reason is not null) return (null, one.Reason);
            bound.Add(one.Binding!);
        }

        return (bound, null);
    }

    private static (ChartBinding? Binding, string? Reason) One(string slot, JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object) return Aggregated(slot, element);

        var field = element.ValueKind == JsonValueKind.String ? element.GetString() : null;
        return string.IsNullOrWhiteSpace(field)
            ? (null, $"slot '{slot}' must be a field name, an object with 'field' and 'aggregate', or a list of them")
            : (new ChartBinding(field, null), null);
    }

    private static (ChartBinding? Binding, string? Reason) Aggregated(string slot, JsonElement element)
    {
        string? field = null;
        string? aggregate = null;
        foreach (var property in element.EnumerateObject())
        {
            var text = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : null;
            if (string.IsNullOrWhiteSpace(text)) return (null, $"slot '{slot}': '{property.Name}' must be text");
            if (string.Equals(property.Name, "field", StringComparison.OrdinalIgnoreCase)) field = text;
            else if (string.Equals(property.Name, "aggregate", StringComparison.OrdinalIgnoreCase)) aggregate = text;
            else return (null, $"slot '{slot}' has a key '{property.Name}' it does not know; a binding takes 'field' and 'aggregate'");
        }

        return aggregate is null
            ? (null, $"slot '{slot}': an object binding must name an 'aggregate'; a field as it stands is written as its name")
            : (new ChartBinding(field, aggregate), null);
    }
}
