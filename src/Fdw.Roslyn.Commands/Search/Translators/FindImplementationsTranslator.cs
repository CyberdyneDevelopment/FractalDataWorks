using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Logging;
using Fdw.Roslyn.Commands.Search.Commands;
using Fdw.Roslyn.Commands.Search.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Fdw.Roslyn.Commands.Search.Translators;

/// <summary>
/// Translator for the FindImplementationsCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "FindImplementations")]
public sealed class FindImplementationsTranslator : RoslynCommandTranslatorBase<FindImplementationsCommand, QueryResult<IReadOnlyList<ImplementationInfo>>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindImplementationsTranslator"/> class.
    /// </summary>
    public FindImplementationsTranslator()
        : base("FindImplementations", "Finds all implementations of an interface or abstract member")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: resolve symbol, find implementations via SymbolFinder, build results
    public override async Task<IGenericResult<QueryResult<IReadOnlyList<ImplementationInfo>>>> Translate(
        FindImplementationsCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(command.FilePath))
        {
            FindImplementationsTranslatorLog.FilePathRequired(Logger);
            return GenericResult<QueryResult<IReadOnlyList<ImplementationInfo>>>.Failure(
                RoslynResultCodes.ByName("FilePathRequired"));
        }

        FindImplementationsTranslatorLog.Finding(Logger, command.FilePath, command.Position);

        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
        {
            FindImplementationsTranslatorLog.DocumentNotFound(Logger, command.FilePath);
            return GenericResult<QueryResult<IReadOnlyList<ImplementationInfo>>>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));
        }

        var document = solution.GetDocument(documentId);
        if (document is null)
        {
            FindImplementationsTranslatorLog.FailedToLoadDocument(Logger, command.FilePath);
            return GenericResult<QueryResult<IReadOnlyList<ImplementationInfo>>>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel is null || syntaxRoot is null)
        {
            FindImplementationsTranslatorLog.FailedToAnalyzeDocument(Logger, command.FilePath);
            return GenericResult<QueryResult<IReadOnlyList<ImplementationInfo>>>.Failure(
                RoslynResultCodes.ByName("FailedToAnalyzeDocument"));
        }

        var token = syntaxRoot.FindToken(command.Position);
        var symbol = semanticModel.GetSymbolInfo(token.Parent!, cancellationToken).Symbol
                  ?? semanticModel.GetDeclaredSymbol(token.Parent!, cancellationToken);

        if (symbol is null)
        {
            FindImplementationsTranslatorLog.NoSymbolFoundAtOffset(Logger, command.FilePath, command.Position);
            return GenericResult<QueryResult<IReadOnlyList<ImplementationInfo>>>.Failure(
                RoslynResultCodes.ByName("NoSymbolFoundAtOffset"),
                ResultDetails.Create().With("Position", command.Position));
        }

        var implementations = new List<ImplementationInfo>();

        // Why: the workspace strips UnresolvedAnalyzerReference at the load boundary
        // (RoslynWorkspaceFactory + SolutionExtensions.WithoutUnresolvedAnalyzers), so the
        // DependentTypeFinder checksum path no longer throws here — no defensive catch needed.
        if (symbol is INamedTypeSymbol typeSymbol)
        {
            var implementingTypes = await SymbolFinder.FindImplementationsAsync(
                typeSymbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);

            foreach (var impl in implementingTypes)
            {
                if (impl.Locations.Length > 0)
                {
                    var lineSpan = impl.Locations[0].GetLineSpan();
                    implementations.Add(new ImplementationInfo(
                        impl.Name,
                        impl.ToDisplayString(),
                        string.Empty,
                        lineSpan.Path ?? string.Empty,
                        lineSpan.StartLinePosition.Line + 1,
                        lineSpan.StartLinePosition.Character + 1));
                }
            }
        }
        else if (symbol is IMethodSymbol or IPropertySymbol or IEventSymbol)
        {
            var implementingMembers = await SymbolFinder.FindImplementationsAsync(
                symbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);

            foreach (var impl in implementingMembers)
            {
                if (impl.Locations.Length > 0)
                {
                    var lineSpan = impl.Locations[0].GetLineSpan();
                    implementations.Add(new ImplementationInfo(
                        impl.Name,
                        impl.ToDisplayString(),
                        impl.ContainingType?.Name ?? string.Empty,
                        lineSpan.Path ?? string.Empty,
                        lineSpan.StartLinePosition.Line + 1,
                        lineSpan.StartLinePosition.Character + 1));
                }
            }
        }

        var summary = $"Found {implementations.Count} implementations of '{symbol.Name}'";
        var result = new QueryResult<IReadOnlyList<ImplementationInfo>>(summary, implementations);

        FindImplementationsTranslatorLog.Found(Logger, symbol.Name, implementations.Count);

        return GenericResult<QueryResult<IReadOnlyList<ImplementationInfo>>>.Success(result, summary);
    }
#pragma warning restore MA0051
}
