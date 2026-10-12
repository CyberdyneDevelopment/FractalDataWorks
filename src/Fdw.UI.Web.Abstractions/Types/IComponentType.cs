using Fdw.Collections;

namespace Fdw.UI.Web.Abstractions;

/// <summary>
/// Interface for component types.
/// </summary>
public interface IComponentType : ITypeOption<int, ComponentTypeBase>
{
    /// <summary>
    /// Gets the category of the component type (e.g., "Input", "Selection", "Complex").
    /// </summary>
    new string Category { get; }
}