using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.VsCodeShell.Hosting;
using Fdw.VsCodeShell.Manifest;
using Fdw.VsCodeShell.Abstractions;

namespace Fdw.VsCodeShell.Tests;

/// <summary>
/// Covers projection of command options into the wire manifest the bootstrap consumes.
/// </summary>
public class VsCodeManifestProjectionTests
{
    private sealed class CanvasHandler : IVsCodeCommandHandler
    {
        public Task<IGenericResult<object?>> Invoke(EditorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult<IGenericResult<object?>>(GenericResult<object?>.Success(null));
    }

    private sealed class PlainHandler : IVsCodeCommandHandler
    {
        public Task<IGenericResult<object?>> Invoke(EditorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult<IGenericResult<object?>>(GenericResult<object?>.Success(null));
    }

    private sealed class CanvasCommand : VsCodeCommandTypeBase<CanvasHandler>
    {
        public CanvasCommand() : base(
            "OpenCanvas", "pidgin.openCanvas", "Open Canvas", "Pidgin", "none",
            new VsCodeWebview("pidgin.canvas", "Canvas", "/")) { }
    }

    private sealed class PlainCommand : VsCodeCommandTypeBase<PlainHandler>
    {
        public PlainCommand() : base("ExplainSymbol", "pidgin.explainSymbol", "Explain Symbol", "Pidgin", "cursor") { }
    }

    private static readonly VsCodeShellOptions Options = new()
    {
        ExtensionId = "fractaldataworks.test",
        DisplayName = "Test",
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void WebviewOpenCommandIdIsProjectedFromItsOwningCommand()
    {
        var manifest = VsCodeManifestFactory.Create(Options, new IVsCodeCommandType[] { new CanvasCommand() });

        manifest.Webviews.Count.ShouldBe(1);
        manifest.Webviews[0].OpenCommandId.ShouldBe("pidgin.openCanvas");
        manifest.Webviews[0].ViewType.ShouldBe("pidgin.canvas");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void EveryWebviewResolvesToADeclaredCommand()
    {
        var manifest = VsCodeManifestFactory.Create(
            Options,
            new IVsCodeCommandType[] { new CanvasCommand(), new PlainCommand() });

        var declaredIds = manifest.Commands.Select(c => c.Id).ToHashSet(System.StringComparer.Ordinal);

        manifest.Webviews.ShouldAllBe(w => declaredIds.Contains(w.OpenCommandId));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CommandWithoutAWebviewContributesNone()
    {
        var manifest = VsCodeManifestFactory.Create(Options, new IVsCodeCommandType[] { new PlainCommand() });

        manifest.Commands.Count.ShouldBe(1);
        manifest.Webviews.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CommandDescriptorCarriesPaletteCategoryAndContextKind()
    {
        var manifest = VsCodeManifestFactory.Create(Options, new IVsCodeCommandType[] { new PlainCommand() });

        manifest.Commands[0].Id.ShouldBe("pidgin.explainSymbol");
        manifest.Commands[0].Category.ShouldBe("Pidgin");
        manifest.Commands[0].ContextKind.ShouldBe("cursor");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ContributesJsonOmitsTitlePrefixSoThePaletteDoesNotDoubleTheCategory()
    {
        var manifest = VsCodeManifestFactory.Create(Options, new IVsCodeCommandType[] { new CanvasCommand() });

        manifest.Commands[0].Title.ShouldBe("Open Canvas");
        manifest.Commands[0].Title.ShouldNotStartWith("Pidgin:");
    }
}
