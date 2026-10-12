using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.UI.Web.Abstractions;

/// <summary>
/// Base class for component types.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class ComponentTypeBase : TypeOptionBase<ComponentTypeBase>, IComponentType
{
    /// <summary>
    /// Gets the category of the component type (e.g., "Input", "Selection", "Complex").
    /// </summary>
    public new string Category { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentTypeBase"/> class.
    /// </summary>
    /// <param name="name">The name of the component type.</param>
    /// <param name="displayName">The display name for the component type.</param>
    /// <param name="category">The category of the component type.</param>
    /// <param name="description">The description of the component type.</param>
    protected ComponentTypeBase(string name, string displayName, string category, string description)
        : base(name, $"ComponentTypes:{name}", displayName, description, category)
    {
        Category = category;
    }
}