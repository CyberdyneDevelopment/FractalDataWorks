using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.VsCodeShell.Hosting;
using Fdw.VsCodeShell.Abstractions;

namespace Fdw.VsCodeShell.Tests;

/// <summary>
/// Covers the contributes.commands JSON spliced into the staged package.json at publish time.
/// </summary>
/// <remarks>
/// The MSBuild wiring (Exec the host, read the file, substitute the property) can only be exercised by a
/// full publish, but the JSON it splices — including how a title with a JSON-special character is escaped —
/// is pure and is locked down here. Escaping was the specifically flagged risk.
/// </remarks>
public class VsCodeContributesExportTests
{
    private sealed class Handler : IVsCodeCommandHandler
    {
        public Task<IGenericResult<object?>> Invoke(EditorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult<IGenericResult<object?>>(GenericResult<object?>.Success(null));
    }

    private sealed class QuoteHandler : IVsCodeCommandHandler
    {
        public Task<IGenericResult<object?>> Invoke(EditorContext context, CancellationToken cancellationToken = default)
            => Task.FromResult<IGenericResult<object?>>(GenericResult<object?>.Success(null));
    }

    private sealed class PlainCommand : VsCodeCommandTypeBase<Handler>
    {
        public PlainCommand() : base("Plain", "pidgin.plain", "Plain Title", "Pidgin", "none") { }
    }

    private sealed class QuotedCommand : VsCodeCommandTypeBase<QuoteHandler>
    {
        // A title carrying " and ; — the two characters that break naive MSBuild property splicing.
        public QuotedCommand() : base("Quoted", "pidgin.quoted", "Say \"hi\"; now", null, "none") { }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void EmitsOneCommandEntryPerOptionWithPaletteCategory()
    {
        var json = VsCodeShellContributesExport.BuildJson(new IVsCodeCommandType[] { new PlainCommand() });

        using var doc = JsonDocument.Parse(json);
        var command = doc.RootElement.GetProperty("commands")[0];
        command.GetProperty("command").GetString().ShouldBe("pidgin.plain");
        command.GetProperty("title").GetString().ShouldBe("Plain Title");
        command.GetProperty("category").GetString().ShouldBe("Pidgin");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void SpecialCharactersInTitleSurviveAsValidJson()
    {
        var json = VsCodeShellContributesExport.BuildJson(new IVsCodeCommandType[] { new QuotedCommand() });

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("commands")[0].GetProperty("title").GetString().ShouldBe("Say \"hi\"; now");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void OmitsCategoryWhenNoneIsDeclared()
    {
        var json = VsCodeShellContributesExport.BuildJson(new IVsCodeCommandType[] { new QuotedCommand() });

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("commands")[0].TryGetProperty("category", out _).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void EmitsSingleLineJson()
    {
        var json = VsCodeShellContributesExport.BuildJson(new IVsCodeCommandType[] { new PlainCommand() });

        json.ShouldNotContain("\n");
    }
}
