using Fdw.UI.Abstractions.Rendering;

namespace Fdw.UI.Rendering.Conformance.Tests;

/// <summary>
/// Proves the ONE-seam claim from FDW-546: every renderer registers under the single
/// <see cref="UIRenderers"/> TypeCollection in <c>Fdw.UI.Abstractions</c> — no duplicated or
/// terminal-anchored registry, and Blazor is a first-class member alongside Spectre.
/// </summary>
[Trait("Category", "Ui")]
public sealed class UIRendererSeamTests
{
    [Fact]
    [Trait("Priority", "P1")]
    public void AllIncludesSpectreAndBlazor()
    {
        var names = UIRenderers.All().Select(r => r.Name).ToList();

        names.ShouldContain("Spectre");
        names.ShouldContain("Blazor");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void SpectreRendererTypeReportsTerminalCapabilities()
    {
        var spectre = UIRenderers.ByName("Spectre");

        spectre.SupportsInteractiveMode.ShouldBeTrue();
        spectre.SupportsAnsiColors.ShouldBeTrue();
        spectre.SupportsHotReload.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void BlazorRendererTypeReportsRetainedModeCapabilities()
    {
        var blazor = UIRenderers.ByName("Blazor");

        blazor.SupportsInteractiveMode.ShouldBeTrue();
        blazor.SupportsAnsiColors.ShouldBeFalse();
        blazor.SupportsFocusManagement.ShouldBeTrue();
        blazor.SupportsHotReload.ShouldBeTrue();
    }
}
