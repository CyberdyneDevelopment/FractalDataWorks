using System;
using Fdw.UI.ComponentOptions;

namespace Fdw.Services.Settings.Components.SettingsComponentOptions;

/// <summary>
/// Base for a component over the settings domain.
/// </summary>
/// <remarks>
/// Non-generic because a TypeCollection binds to one closed member base and an open generic cannot
/// be that type; the generic form below derives from this so members close it.
/// </remarks>
public abstract class SettingsComponentBase : ComponentTypeOptionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsComponentBase"/> class.
    /// </summary>
    /// <param name="name">The option's name.</param>
    /// <param name="componentType">The provider component this option declares.</param>
    /// <param name="description">What the component shows.</param>
    protected SettingsComponentBase(string name, Type componentType, string description)
        : base(name, componentType, description, "SettingsComponent")
    {
    }
}

/// <summary>
/// Base for a settings component that takes its identity from the component class.
/// </summary>
/// <typeparam name="TComponent">The provider component.</typeparam>
public abstract class SettingsComponentBase<TComponent> : SettingsComponentBase
    where TComponent : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsComponentBase{TComponent}"/> class.
    /// </summary>
    protected SettingsComponentBase()
        : base(DeriveName(typeof(TComponent)), typeof(TComponent), $"The {DeriveName(typeof(TComponent))} component.")
    {
    }
}
