using System.Text.Json;
using Fdw.Collections;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>Base class for the shapes the value a graph request states for a setting can have.</summary>
public abstract class SettingValueShapeBase : TypeOptionBase<SettingValueShapeBase>, ISettingValueShape
{
    /// <summary>Initializes a new instance of the <see cref="SettingValueShapeBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    protected SettingValueShapeBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract bool Matches(object? value);

    /// <inheritdoc />
    public abstract JsonElement ToElement(object? value, JsonSerializerOptions options);
}
