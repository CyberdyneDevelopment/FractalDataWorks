using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Fdw.UI.Components.Blazor.Tests.DataInfra;

/// <summary>
/// Non-generic static store for pending context objects keyed by context type.
/// Separating static state from the generic class avoids CA1000.
/// </summary>
internal static class ProviderStubState
{
    // Why concurrent: xUnit runs test collections in parallel and bUnit instantiates components on
    // its own renderer thread, so Set and Take genuinely interleave across threads. A plain
    // Dictionary corrupts its buckets under that access and throws from an unrelated test.
    private static readonly ConcurrentDictionary<Type, object?> s_pending = new();

    internal static void Set<TContext>(TContext? value) =>
        s_pending[typeof(TContext)] = value;

    internal static TContext? Take<TContext>() where TContext : new() =>
        s_pending.TryRemove(typeof(TContext), out var value) ? (TContext?)value : default;
}

/// <summary>
/// Stand-in for any FDW headless provider whose child template is
/// <c>RenderFragment&lt;TContext&gt;</c>. Lets a test render the consuming
/// page's UI without standing up the provider's HTTP/service stack.
/// Captures any non-ChildContent parameters so providers with extra args
/// (RefreshInterval, DefaultDays, etc.) don't fail to bind.
/// </summary>
public sealed class ProviderStub<TContext> : ComponentBase
    where TContext : new()
{
    [Parameter] public RenderFragment<TContext>? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object?>? Extra { get; set; }

    private TContext _context = new();

    protected override void OnInitialized()
    {
        _context = ProviderStubState.Take<TContext>() ?? new TContext();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (ChildContent is not null)
        {
            builder.AddContent(0, ChildContent(_context));
        }
    }
}
