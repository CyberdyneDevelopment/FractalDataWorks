using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.UI.Web.Abstractions;

/// <summary>
/// Base class for component types.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class ComponentTypeBase : TypeOptionBase<int, ComponentTypeBase>, IComponentType
{
    /// <summary>
    /// Gets the category of the component type (e.g., "Input", "Selection", "Complex").
    /// </summary>
    public new string Category { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the component type.</param>
    /// <param name="name">The name of the component type.</param>
    /// <param name="displayName">The display name for the component type.</param>
    /// <param name="category">The category of the component type.</param>
    /// <param name="description">The description of the component type.</param>
    protected ComponentTypeBase(int id, string name, string displayName, string category, string description)
        : base(id, name, $"ComponentTypes:{name}", displayName, description, category)
    {
        Category = category;
    }
}