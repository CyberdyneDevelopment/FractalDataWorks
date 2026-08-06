using System;
using Fdw.Collections;
using Fdw.UI.Abstractions.Components;

namespace Fdw.UI.Rendering.Blazor.Dispatch;

/// <summary>
/// Maps a headless component model to the Blazor component that paints it.
/// </summary>
/// <remarks>
/// <para>
/// Why a TypeCollection instead of the switch this replaces: dispatch used to be a
/// <c>switch (Model)</c> over concrete types with a hard-coded <c>IsSupported</c> gate beside it,
/// which closed the set. A component model declared in any assembly but this one could not render —
/// so the headless architecture was extensible everywhere except at the point where extension
/// actually shows up on screen. Registering the mapping as a TypeOption means a downstream package
/// adds its own model and its renderer the same way it adds any other option, with no edit here.
/// </para>
/// <para>
/// This lives in the Blazor renderer, not in the render-agnostic abstractions, on purpose: a model
/// is backend-neutral, but the thing that paints it never is. Each renderer backend owns its own
/// registry of model-to-visual mappings.
/// </para>
/// </remarks>
public interface IBlazorComponentRenderer : ITypeOption<int, BlazorComponentRendererBase>
{
    /// <summary>
    /// Gets the dispatch precedence; lower wins when several options match the same model.
    /// </summary>
    /// <remarks>
    /// Ordering is part of the contract, not an implementation detail: the models form an
    /// overlapping hierarchy (a <c>TextInputModel</c> is also an <c>IInputComponentModel</c>), so a
    /// concrete mapping must be consulted before the interface-level fallback that would also
    /// claim it. The old switch encoded this in case order, where it was invisible and unextendable;
    /// making it a number lets a downstream option slot in deliberately.
    /// </remarks>
    int Precedence { get; }

    /// <summary>
    /// Gets the Blazor component type that renders a matching model.
    /// </summary>
    Type ComponentType { get; }

    /// <summary>
    /// Determines whether this option renders the supplied model.
    /// </summary>
    /// <param name="model">The component model about to be painted.</param>
    /// <returns><see langword="true"/> when this option can paint it.</returns>
    bool CanRender(IComponentModel model);
}
