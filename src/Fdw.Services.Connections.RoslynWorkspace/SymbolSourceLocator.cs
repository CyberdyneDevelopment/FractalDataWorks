using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.RoslynWorkspace.Abstractions;
using Fdw.Services.Connections.RoslynWorkspace.Abstractions.Results;
using Fdw.Services.Connections.RoslynWorkspace.Abstractions.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.RoslynWorkspace;

/// <summary>
/// Resolves a Roslyn DocumentationCommentId to source text via symbol location lookup.
/// </summary>
internal static class SymbolSourceLocator
{
    /// <summary>
    /// Finds the symbol matching the given DocumentationCommentId in the workspace solution
    /// and returns its source text, optionally sliced to the given line range.
    /// </summary>
    internal static async Task<IGenericResult<RawText>> GetSymbolSource(
        Solution solution,
        string symbolId,
        RawTextLineRange? lines,
        string connectionName,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(symbolId))
            return GenericResult<RawText>.Failure(
                RoslynWorkspaceConnectionLog.InvalidSymbolId(logger, connectionName, symbolId, "symbolId is null or whitespace"));

        ISymbol? symbol = null;
        foreach (var project in solution.Projects)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (compilation is null)
                continue;

            var candidates = DocumentationCommentId.GetSymbolsForDeclarationId(symbolId, compilation);
            if (candidates.IsDefaultOrEmpty)
                continue;

            symbol = candidates[0];
            break;
        }

        if (symbol is null)
            return GenericResult<RawText>.Failure(
                RoslynWorkspaceConnectionLog.SymbolNotFound(logger, connectionName, symbolId));

        var location = symbol.Locations.FirstOrDefault(l => l.IsInSource);
        if (location is null)
            return GenericResult<RawText>.Failure(
                RoslynWorkspaceConnectionLog.InvalidSymbolId(
                    logger, connectionName, symbolId,
                    "symbol has no source locations — may be metadata-only"));

        var sourceTree = location.SourceTree;
        if (sourceTree is null)
            return GenericResult<RawText>.Failure(
                RoslynWorkspaceConnectionLog.InvalidSymbolId(
                    logger, connectionName, symbolId,
                    "symbol source location has no syntax tree"));

        cancellationToken.ThrowIfCancellationRequested();

        var sourceText = await sourceTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var span = location.SourceSpan;
        var startLine = sourceText.Lines.GetLinePosition(span.Start).Line;
        var endLine = sourceText.Lines.GetLinePosition(span.End).Line;

        string text;
        RawTextLineRange resultRange;

        if (lines is not null)
        {
            var requestedStart = System.Math.Max(lines.StartLine - 1, startLine);
            var requestedEnd = System.Math.Min(lines.EndLine - 1, endLine);

            if (requestedStart > requestedEnd)
                requestedStart = startLine;

            var sliceStart = sourceText.Lines[requestedStart].Start;
            var sliceEnd = sourceText.Lines[requestedEnd].EndIncludingLineBreak;
            text = sourceText.ToString(Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(sliceStart, sliceEnd));
            resultRange = new RawTextLineRange(requestedStart + 1, requestedEnd + 1);
        }
        else
        {
            var fullStart = sourceText.Lines[startLine].Start;
            var fullEnd = sourceText.Lines[endLine].EndIncludingLineBreak;
            text = sourceText.ToString(Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(fullStart, fullEnd));
            resultRange = new RawTextLineRange(startLine + 1, endLine + 1);
        }

        return GenericResult<RawText>.Success(new RawText(text, resultRange));
    }
}
