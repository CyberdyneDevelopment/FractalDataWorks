using Bunit;
using Fdw.UI.Abstractions.RenderModeOptions;
using Fdw.UI.Rendering.Blazor;
using Fdw.UI.Rendering.Blazor.Components;
using Fdw.UI.Rendering.Spectre;
using Microsoft.Extensions.Logging.Abstractions;
using BunitContext = Bunit.BunitContext;

namespace Fdw.UI.Rendering.Conformance.Tests;

/// <summary>
/// Conformance tests for the Blazor backend against the SAME canonical
/// <see cref="ConformanceFixtures"/> models used by <see cref="SpectreConformanceTests"/>.
/// This is the cross-paradigm proof FDW-546 set out to demonstrate: an imperative console
/// renderer and a retained-mode web renderer both satisfy the same behavioral contract.
/// </summary>
[Trait("Category", "Ui")]
public sealed class BlazorConformanceTests
{
    private static (BlazorUIRenderer Renderer, BlazorRenderContext Context) CreateHarness() =>
        (new BlazorUIRenderer(NullLogger<BlazorUIRenderer>.Instance),
         new BlazorRenderContext(RenderModes.ByName("Edit")));

    [Fact]
    [Trait("Priority", "P1")]
    public async Task RenderWithWrongContextTypeReturnsFailure()
    {
        var (renderer, _) = CreateHarness();
        var wrongContext = new SpectreRenderContext();

        var result = await renderer.Render(
            ConformanceFixtures.CreateTextInput(), wrongContext, Xunit.TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task RenderPageValidSaveReturnsSuccessAndSavesPage()
    {
        using var ctx = new BunitContext();
        var (renderer, context) = CreateHarness();
        var page = ConformanceFixtures.CreateSavablePage();
        var cut = ctx.Render<FdwRenderHost>(p => p.Add(x => x.Context, context));

        var resultTask = renderer.RenderPage(page, context, Xunit.TestContext.Current.CancellationToken);
        cut.WaitForAssertion(() => cut.Find("button.fdw-ui-action-save").ShouldNotBeNull());
        cut.Find("button.fdw-ui-action-save").Click();
        var result = await resultTask;

        result.Success.ShouldBeTrue();
        result.Action.Name.ShouldBe("Save");
        result.SavedConfiguration.ShouldBeSameAs(page);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task RenderPageInvalidRequiredFieldReturnsValidationFailed()
    {
        using var ctx = new BunitContext();
        var (renderer, context) = CreateHarness();
        var page = ConformanceFixtures.CreateInvalidPage();
        var cut = ctx.Render<FdwRenderHost>(p => p.Add(x => x.Context, context));

        var resultTask = renderer.RenderPage(page, context, Xunit.TestContext.Current.CancellationToken);
        cut.WaitForAssertion(() => cut.Find("button.fdw-ui-action-save").ShouldNotBeNull());
        cut.Find("button.fdw-ui-action-save").Click();
        var result = await resultTask;

        result.Success.ShouldBeFalse();
        result.Validation.ShouldNotBeNull();
        result.Validation!.IsValid.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task RenderPageCancelReturnsCancelAction()
    {
        using var ctx = new BunitContext();
        var (renderer, context) = CreateHarness();
        var page = ConformanceFixtures.CreateSavablePage();
        var cut = ctx.Render<FdwRenderHost>(p => p.Add(x => x.Context, context));

        var resultTask = renderer.RenderPage(page, context, Xunit.TestContext.Current.CancellationToken);
        cut.WaitForAssertion(() => cut.Find("button.fdw-ui-action-cancel").ShouldNotBeNull());
        cut.Find("button.fdw-ui-action-cancel").Click();
        var result = await resultTask;

        result.Success.ShouldBeTrue();
        result.Action.Name.ShouldBe("Cancel");
    }
}
