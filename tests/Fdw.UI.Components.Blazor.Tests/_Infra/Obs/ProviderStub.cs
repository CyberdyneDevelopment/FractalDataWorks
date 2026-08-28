using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Fdw.UI.Components.Blazor.Tests.ObsInfra;

/// <summary>
/// Stand-in for any FDW headless provider whose child template is a
/// <c>RenderFragment&lt;TContext&gt;</c>. Lets a test render a hosted FDW page's markup against a
/// seeded context without standing up the provider's HTTP/service stack. Captures any
/// non-ChildContent parameters (PromotionId, RefreshInterval, etc.) so providers with extra args
/// still bind.
/// </summary>
public sealed class ProviderStub<TContext> : ComponentBase
    where TContext : new()
{
    [Parameter] public RenderFragment<TContext>? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object?>? Extra { get; set; }

    private TContext _context = new();

    protected override void OnInitialized() =>
        _context = ProviderStubState.Take<TContext>() ?? new TContext();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (ChildContent is not null)
        {
            builder.AddContent(0, ChildContent(_context));
        }
    }
}
