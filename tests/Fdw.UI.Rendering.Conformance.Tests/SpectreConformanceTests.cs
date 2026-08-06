using Fdw.UI.Abstractions.Rendering;
using Fdw.UI.Rendering.Spectre;
using Microsoft.Extensions.Logging.Abstractions;
using Spectre.Console.Testing;

namespace Fdw.UI.Rendering.Conformance.Tests;

/// <summary>
/// Conformance tests for the Spectre.Console backend against the canonical
/// <see cref="ConformanceFixtures"/> models. Paired with <see cref="BlazorConformanceTests"/> —
/// the same fixtures, the same assertions, two renderers.
/// </summary>
[Trait("Category", "Ui")]
public sealed class SpectreConformanceTests
{
    private static (SpectreUIRenderer Renderer, SpectreRenderContext Context, TestConsole Console) CreateHarness()
    {
        var console = new TestConsole().Interactive();
        var context = new SpectreRenderContext(console);
        var renderer = new SpectreUIRenderer(NullLogger<SpectreUIRenderer>.Instance);
        return (renderer, context, console);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task RenderWithWrongContextTypeReturnsFailure()
    {
        var (renderer, _, _) = CreateHarness();
        var wrongContext = new Fdw.UI.Rendering.Blazor.BlazorRenderContext(RenderModesForTest());

        var result = await renderer.Render(
            ConformanceFixtures.CreateTextInput(), wrongContext, TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task RenderPageValidSaveReturnsSuccessAndSavesPage()
    {
        var (renderer, context, console) = CreateHarness();
        // Why: PromptForPageAction reads a SelectionPrompt<string> whose first choice is "Save";
        // pushing Enter alone accepts the default (first) choice.
        console.Input.PushKey(ConsoleKey.Enter);
        var page = ConformanceFixtures.CreateSavablePage();

        var result = await renderer.RenderPage(page, context, TestContext.Current.CancellationToken);

        result.Success.ShouldBeTrue();
        result.Action.Name.ShouldBe("Save");
        result.SavedConfiguration.ShouldBeSameAs(page);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task RenderPageInvalidRequiredFieldReturnsValidationFailed()
    {
        var (renderer, context, console) = CreateHarness();
        console.Input.PushKey(ConsoleKey.Enter);
        var page = ConformanceFixtures.CreateInvalidPage();

        var result = await renderer.RenderPage(page, context, TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.Validation.ShouldNotBeNull();
        result.Validation!.IsValid.ShouldBeFalse();
    }

    private static Fdw.UI.Abstractions.RenderModeOptions.IRenderMode RenderModesForTest() =>
        Fdw.UI.Abstractions.RenderModeOptions.RenderModes.ByName("Edit");
}
