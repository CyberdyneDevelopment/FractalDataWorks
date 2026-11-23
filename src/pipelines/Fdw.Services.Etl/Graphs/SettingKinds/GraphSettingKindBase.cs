using System.Text.Json;
using Fdw.Collections;

namespace Fdw.Services.Etl.Graphs;

/// <summary>Base class for what a graph setting holds.</summary>
public abstract class GraphSettingKindBase : TypeOptionBase<GraphSettingKindBase>, IGraphSettingKind
{
    /// <summary>Initializes a new instance of the <see cref="GraphSettingKindBase"/> class.</summary>
    /// <param name="name">The option name, the word a stored setting names its kind by.</param>
    /// <param name="parameterType">The <c>DataTypes</c> option name a run parameter must have to supply a setting of this kind.</param>
    /// <param name="words">The kind in words; empty for a kind that is not a single value.</param>
    protected GraphSettingKindBase(string name, string parameterType, string words)
        : base(name)
    {
        ParameterType = parameterType;
        Words = words;
    }

    /// <inheritdoc />
    public string ParameterType { get; }

    /// <inheritdoc />
    public string Words { get; }

    /// <inheritdoc />
    public virtual object? ReadJson(JsonElement value) => null;

    /// <inheritdoc />
    public virtual bool TryConvert(object value, out object? typed)
    {
        typed = null;
        return false;
    }
}
