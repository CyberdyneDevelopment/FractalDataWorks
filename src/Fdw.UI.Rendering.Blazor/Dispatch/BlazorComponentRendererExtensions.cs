using System.Collections.Generic;
using System.Linq;
using Fdw.UI.Abstractions.Components;

namespace Fdw.UI.Rendering.Blazor.Dispatch;

/// <summary>
/// Resolution helpers over <see cref="BlazorComponentRenderers"/>.
/// </summary>
public static class BlazorComponentRendererExtensions
{
    /// <summary>
    /// Finds the registered renderer for a component model, honouring precedence.
    /// </summary>
    /// <param name="model">The model about to be painted.</param>
    /// <returns>
    /// The matching option, or <see langword="null"/> when no registered option claims the model.
    /// </returns>
    /// <remarks>
    /// Returns null rather than a catch-all so the caller can say plainly that nothing is
    /// registered for this model type. Substituting a generic fallback component here would render
    /// something misleading — a control the author never asked for, silently — instead of
    /// surfacing that the mapping is missing.
    /// </remarks>
    public static IBlazorComponentRenderer? ResolveFor(IComponentModel model)
    {
        if (model is null)
        {
            return null;
        }

        IBlazorComponentRenderer? best = null;
        foreach (var renderer in BlazorComponentRenderers.All())
        {
            if (!renderer.CanRender(model))
            {
                continue;
            }

            if (best is null || renderer.Precedence < best.Precedence)
            {
                best = renderer;
            }
        }

        return best;
    }

    /// <summary>
    /// Gets every registered renderer in dispatch order, most specific first.
    /// </summary>
    /// <returns>The registered renderers ordered by precedence.</returns>
    public static IReadOnlyList<IBlazorComponentRenderer> InDispatchOrder() =>
        [.. BlazorComponentRenderers.All().OrderBy(r => r.Precedence)];
}
