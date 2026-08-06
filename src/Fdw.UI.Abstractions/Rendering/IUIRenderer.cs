using System.Threading;
using System.Threading.Tasks;
using Fdw.UI.Abstractions.Components;
using Fdw.UI.Abstractions.Pages;

namespace Fdw.UI.Abstractions.Rendering;

/// <summary>
/// Core contract for UI renderers.
/// </summary>
/// <remarks>
/// <para>
/// Implementations translate component models to framework-specific renderables.
/// The same IPageModel can be rendered by Spectre.Console, RazorConsole, Blazor, etc.
/// </para>
/// <para>
/// Renderers are registered as TypeOptions in the UIRenderers collection,
/// enabling runtime switching between rendering backends.
/// </para>
/// </remarks>
public interface IUIRenderer
{
    /// <summary>
    /// Gets a value indicating whether this renderer supports interactive mode.
    /// </summary>
    bool SupportsInteractiveMode { get; }

    /// <summary>
    /// Gets a value indicating whether this renderer supports ANSI colors.
    /// </summary>
    bool SupportsAnsiColors { get; }

    /// <summary>
    /// Gets a value indicating whether this renderer supports focus management.
    /// </summary>
    bool SupportsFocusManagement { get; }

    /// <summary>
    /// Gets a value indicating whether this renderer supports hot reload.
    /// </summary>
    bool SupportsHotReload { get; }

    /// <summary>
    /// Renders a component model.
    /// </summary>
    /// <param name="model">The component model to render.</param>
    /// <param name="context">The render context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A render result.</returns>
    Task<RenderResult> Render(
        IComponentModel model,
        IRenderContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Prompts the user for input and returns the updated model.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="model">The input component model.</param>
    /// <param name="context">The render context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A prompt result containing the user's input.</returns>
    Task<PromptResult<T>> Prompt<T>(
        IInputComponentModel<T> model,
        IRenderContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders a full page.
    /// </summary>
    /// <param name="page">The page model to render.</param>
    /// <param name="context">The render context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A page result.</returns>
    Task<PageResult> RenderPage(
        IPageModel page,
        IRenderContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders a list page (columns, rows and actions) and reports the action the user chose.
    /// </summary>
    /// <remarks>
    /// Brings the <see cref="IListPageModel"/> archetype inside the renderer contract, so a caller
    /// holding an <see cref="IUIRenderer"/> resolved from <see cref="UIRenderers"/> can paint a list
    /// without naming a backend. <c>IListPageModel</c> is not an <see cref="IPageModel"/> — a list has
    /// no sections — so it needs its own entry point rather than riding <see cref="RenderPage"/>.
    /// </remarks>
    /// <param name="page">The list page model to render.</param>
    /// <param name="context">The render context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list page result carrying the chosen action, if any.</returns>
    Task<ListPageResult> RenderListPage(
        IListPageModel page,
        IRenderContext context,
        CancellationToken cancellationToken = default);
}