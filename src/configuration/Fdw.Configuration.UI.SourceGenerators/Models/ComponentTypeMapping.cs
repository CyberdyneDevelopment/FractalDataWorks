using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Configuration.UI.SourceGenerators.Models;

/// <summary>
/// Maps to ComponentTypes in UI.Web.Abstractions.
/// </summary>
/// <remarks>
/// Why not a TypeCollection: a source generator runs inside the compiler before TypeCollections exist, so it cannot load Fdw.Collections.
/// </remarks>
public sealed class ComponentTypeMapping
{
    /// <summary>Single-line text input component.</summary>
    public static readonly ComponentTypeMapping TextInput = new(1, nameof(TextInput));

    /// <summary>Numeric input component for numbers.</summary>
    public static readonly ComponentTypeMapping NumericInput = new(2, nameof(NumericInput));

    /// <summary>Boolean toggle switch component.</summary>
    public static readonly ComponentTypeMapping Switch = new(3, nameof(Switch));

    /// <summary>Date/time picker component.</summary>
    public static readonly ComponentTypeMapping DateTimePicker = new(4, nameof(DateTimePicker));

    /// <summary>Multi-line text area component.</summary>
    public static readonly ComponentTypeMapping TextArea = new(5, nameof(TextArea));

    /// <summary>Dropdown selection component.</summary>
    public static readonly ComponentTypeMapping Dropdown = new(6, nameof(Dropdown));

    /// <summary>Collection editor component.</summary>
    public static readonly ComponentTypeMapping Collection = new(7, nameof(Collection));

    private ComponentTypeMapping(int id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>Gets every component type.</summary>
    public static IReadOnlyList<ComponentTypeMapping> All { get; } = [TextInput, NumericInput, Switch, DateTimePicker, TextArea, Dropdown, Collection];

    /// <summary>Gets the component type's id.</summary>
    public int Id { get; }

    /// <summary>Gets the component type's name.</summary>
    public string Name { get; }

    /// <summary>The component type with this name, or null when there is none.</summary>
    /// <param name="name">The name.</param>
    /// <returns>The component type, or null.</returns>
    public static ComponentTypeMapping? ByName(string name) =>
        All.FirstOrDefault(mapping => string.Equals(mapping.Name, name, StringComparison.Ordinal));

    /// <inheritdoc />
    public override string ToString() => Name;
}
