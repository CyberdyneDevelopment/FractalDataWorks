using System;
using Fdw.Collections;

namespace Fdw.UI.Abstractions.Composition;

/// <summary>
/// Base class for composable component descriptors.
/// </summary>
/// <remarks>
/// Inherit and apply <c>[TypeOption(typeof(ComponentCatalog), "Key")]</c> to publish a component to
/// the catalogue. Downstream assemblies register their own the same way — the entry-point app's
/// generated module initializer discovers them, so an app's component library and FDW's arrive
/// through one mechanism rather than the app maintaining a parallel list.
/// </remarks>
public abstract class ComponentDescriptorBase : TypeOptionBase<int, ComponentDescriptorBase>, IComponentDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentDescriptorBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="key">The stable key a saved layout stores.</param>
    /// <param name="displayName">The palette display name.</param>
    /// <param name="category">The palette grouping.</param>
    /// <param name="description">A short description of what the component shows.</param>
    protected ComponentDescriptorBase(int id, string key, string displayName, string category, string description)
        : base(id, key)
    {
        Key = key;
        DisplayName = displayName;
        Category = category;
        Description = description;
    }

    /// <inheritdoc />
    public string Key { get; }

    /// <inheritdoc />
    public new string DisplayName { get; }

    /// <inheritdoc />
    public new string Category { get; }

    /// <inheritdoc />
    public new string Description { get; }

    /// <inheritdoc />
    public abstract Type ComponentType { get; }

    /// <inheritdoc />
    public virtual int DefaultWidth => 4;

    /// <inheritdoc />
    public virtual int DefaultHeight => 3;

    /// <inheritdoc />
    public virtual int MinimumWidth => 2;

    /// <inheritdoc />
    public virtual int MinimumHeight => 2;
}
