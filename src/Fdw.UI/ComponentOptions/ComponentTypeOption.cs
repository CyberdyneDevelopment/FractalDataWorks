using System;

namespace Fdw.UI.ComponentOptions;

/// <summary>
/// A declared component that takes its identity from the component class itself.
/// </summary>
/// <typeparam name="TComponent">The provider component this option declares.</typeparam>
/// <remarks>
/// Exists so a member states nothing twice — the type comes from <typeparamref name="TComponent"/>,
/// the name from its type name, the id from the name. An option whose name says one component and
/// whose typeof says another cannot be written.
/// </remarks>
public abstract class ComponentTypeOption<TComponent> : ComponentTypeOptionBase
    where TComponent : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentTypeOption{TComponent}"/> class.
    /// </summary>
    protected ComponentTypeOption()
        : base(DeriveName(typeof(TComponent)), typeof(TComponent), $"The {DeriveName(typeof(TComponent))} component.")
    {
    }
}
