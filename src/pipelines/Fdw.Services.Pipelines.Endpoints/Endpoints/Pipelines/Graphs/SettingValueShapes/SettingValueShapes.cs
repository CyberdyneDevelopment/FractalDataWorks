using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// The shapes the value a graph request states for a setting can have: JSON already, nothing, and any other object, which is written as JSON.
/// </summary>
/// <remarks>The first two exclude one another; <see cref="Of"/> answers the third for every value they do not match.</remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(SettingValueShapeBase), typeof(ISettingValueShape), typeof(SettingValueShapes))]
public abstract partial class SettingValueShapes : TypeCollectionBase<SettingValueShapeBase, ISettingValueShape>
{
    /// <summary>Finds the shape of a value.</summary>
    /// <param name="value">The value the request states.</param>
    /// <returns>The shape that matches the value, or the shape that writes any other object as JSON.</returns>
    public static ISettingValueShape Of(object? value)
        => All().FirstOrDefault(shape => shape.Matches(value)) is { } matched ? matched : Written;
}
