using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Fdw.UI.Components.Blazor.Tests.Helpers;

/// <summary>
/// Non-generic store for pending context objects keyed by context type. Separating static state
/// from the generic stub avoids CA1000 (do not declare static members on generic types).
/// </summary>
/// <remarks>
/// Why: the backing store is <see cref="AsyncLocal{T}"/>, not a plain static, so each test's
/// execution flow gets ITS OWN seed map. The FDW Blazor.Tests project parallelizes test collections;
/// a shared static dictionary would let one class's seed leak into another class's render. The
/// factory's Set and the stub's Take run on the same async flow within a single test, so AsyncLocal
/// isolates them perfectly.
/// </remarks>
internal static class ProviderStubState
{
    private static readonly AsyncLocal<Dictionary<Type, object?>?> s_pending = new();

    private static Dictionary<Type, object?> Current => s_pending.Value ??= [];

    internal static void Set<TContext>(TContext? value) =>
        Current[typeof(TContext)] = value;

    internal static TContext? Take<TContext>() where TContext : new()
    {
        if (Current.TryGetValue(typeof(TContext), out var value))
        {
            Current.Remove(typeof(TContext));
            return (TContext?)value;
        }
        return default;
    }
}

/// <summary>
/// Stand-in for any FDW headless provider whose child template is <c>RenderFragment&lt;TContext&gt;</c>.
/// Lets a test render the consuming page's UI directly with a seeded context, without standing up
/// the provider's HTTP/service stack. Captures any non-ChildContent parameters so providers with
/// extra args (AutoLoadList, ActionId, …) do not fail to bind.
/// </summary>
public sealed class ProviderStub<TContext> : ComponentBase
    where TContext : new()
{
    [Parameter] public RenderFragment<TContext>? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object?>? Extra { get; set; }

    private TContext _context = new();

    protected override void OnInitialized() =>
        // Why: Take<T> removes the seed from the shared store, so it must be captured ONCE here.
        // Re-reading on every BuildRenderTree (e.g. after a filter @onclick) would drop the seed.
        _context = ProviderStubState.Take<TContext>() ?? new TContext();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (ChildContent is not null)
        {
            builder.AddContent(0, ChildContent(_context));
        }
    }
}
