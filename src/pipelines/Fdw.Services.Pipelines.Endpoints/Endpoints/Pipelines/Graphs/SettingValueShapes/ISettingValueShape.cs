using System.Text.Json;
using Fdw.Collections;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>A shape the value a graph request states for a setting can have, and how it becomes the JSON the setting is read from.</summary>
public interface ISettingValueShape : ITypeOption<int, SettingValueShapeBase>
{
    /// <summary>Whether the value has this shape.</summary>
    /// <param name="value">The value the request states.</param>
    /// <returns><c>true</c> when it does.</returns>
    bool Matches(object? value);

    /// <summary>Makes the JSON the setting is read from.</summary>
    /// <param name="value">A value this shape <see cref="Matches"/>, or any value for the shape that takes the rest.</param>
    /// <param name="options">How an object is written as JSON.</param>
    /// <returns>The JSON.</returns>
    JsonElement ToElement(object? value, JsonSerializerOptions options);
}
