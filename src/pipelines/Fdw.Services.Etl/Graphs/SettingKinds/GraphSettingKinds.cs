using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// What a graph setting holds: <c>Text</c>, <c>Number</c> and <c>Boolean</c> single values, and the lists
/// <c>ColumnList</c>, <c>Distinct</c>, <c>SortFieldList</c> and <c>FieldMapPairs</c>.
/// </summary>
/// <remarks>A stored setting names its kind by the option's name, matched exactly through <c>ByName</c>; a name that matches none gets <c>NotFound</c>, which supplies no parameter type, reads no JSON and converts no value.</remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(GraphSettingKindBase), typeof(IGraphSettingKind), typeof(GraphSettingKinds))]
public abstract partial class GraphSettingKinds : TypeCollectionBase<GraphSettingKindBase, IGraphSettingKind>
{
    /// <summary>Says which <c>DataTypes</c> option a parameter must have to supply a setting of a kind.</summary>
    /// <param name="settingKind">What the setting holds.</param>
    /// <returns>The data type name (of each item, for a list), or an empty text for a kind no parameter can supply.</returns>
    public static string ParameterTypeFor(string? settingKind)
        => (settingKind is { } word ? ByName(word) : NotFound) is var kind && !ReferenceEquals(kind, NotFound)
            ? kind.ParameterType
            : string.Empty;
}
