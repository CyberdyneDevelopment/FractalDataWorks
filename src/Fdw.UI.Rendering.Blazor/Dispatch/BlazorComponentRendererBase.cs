using System;
using Fdw.Collections;
using Fdw.UI.Abstractions.Components;

namespace Fdw.UI.Rendering.Blazor.Dispatch;

/// <summary>
/// Base class for Blazor component-renderer options.
/// </summary>
/// <remarks>
/// Inherit and apply <c>[TypeOption(typeof(BlazorComponentRenderers), "Name")]</c> to map a
/// component model to the Blazor component that paints it. Downstream assemblies register their
/// own the same way — the entry-point app's generated module initializer discovers them.
/// </remarks>
public abstract class BlazorComponentRendererBase : TypeOptionBase<int, BlazorComponentRendererBase>, IBlazorComponentRenderer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlazorComponentRendererBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The option name.</param>
    /// <param name="precedence">
    /// Dispatch precedence; lower is consulted first. Concrete model mappings should sit below the
    /// interface-level fallbacks that would otherwise also claim them.
    /// </param>
    protected BlazorComponentRendererBase(int id, string name, int precedence)
        : base(id, name)
    {
        Precedence = precedence;
    }

    /// <inheritdoc />
    public int Precedence { get; }

    /// <inheritdoc />
    public abstract Type ComponentType { get; }

    /// <inheritdoc />
    public abstract bool CanRender(IComponentModel model);
}
