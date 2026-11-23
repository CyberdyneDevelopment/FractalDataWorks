using System.Text.Json;
using Fdw.Collections;

namespace Fdw.Services.Etl.Graphs;

/// <summary>What a graph setting holds: a single value (text, a whole number, true or false) or a list.</summary>
public interface IGraphSettingKind : ITypeOption<int, GraphSettingKindBase>
{
    /// <summary>Gets the <c>DataTypes</c> option name a run parameter must have to supply a setting of this kind (of each item, for a list).</summary>
    string ParameterType { get; }

    /// <summary>Gets the kind in words, for a refusal that names what the setting must be; empty for a kind that is not a single value.</summary>
    string Words { get; }

    /// <summary>Reads the single value of this kind out of the JSON a request states.</summary>
    /// <param name="value">The JSON the request states.</param>
    /// <returns>The typed value, or <c>null</c> when the JSON is not this kind of value or the kind is not a single value.</returns>
    object? ReadJson(JsonElement value);

    /// <summary>Makes a setting's value out of the value a run was given for a parameter.</summary>
    /// <param name="value">The parameter's typed value.</param>
    /// <param name="typed">The value as the setting holds it.</param>
    /// <returns><c>true</c> when the value is of this kind or converts to it; <c>false</c> for a kind that is not a single value.</returns>
    bool TryConvert(object value, out object? typed);
}
