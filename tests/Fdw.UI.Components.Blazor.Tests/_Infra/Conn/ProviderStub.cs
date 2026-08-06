using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Fdw.UI.Components.Blazor.Tests.ConnInfra;

/// <summary>
/// Stand-in for any FDW headless provider whose child template is
/// <c>RenderFragment&lt;TContext&gt;</c>. Lets a test render the consuming page's UI with a
/// seeded context without standing up the provider's HTTP/service stack. Captures any
/// non-ChildContent parameters so providers with extra args don't fail to bind.
/// </summary>
/// <remarks>
/// The seed is carried on the instance (set by <see cref="ProviderFactory{TActual,TContext}"/>
/// when it constructs the stub). Why: a process-wide static handoff races when stub-based tests
/// from different classes run in parallel — instance state keeps each render deterministic.
/// </remarks>
public sealed class ProviderStub<TContext> : ComponentBase
    where TContext : new()
{
    private readonly TContext _context;

    public ProviderStub() => _context = new TContext();

    public ProviderStub(TContext? seed) => _context = seed ?? new TContext();

    [Parameter] public RenderFragment<TContext>? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object?>? Extra { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (ChildContent is not null)
        {
            builder.AddContent(0, ChildContent(_context));
        }
    }
}
