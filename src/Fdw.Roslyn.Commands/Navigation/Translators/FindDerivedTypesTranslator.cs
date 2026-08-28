using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Logging;
using Fdw.Roslyn.Commands.Navigation.Commands;
using Fdw.Roslyn.Commands.Navigation.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Navigation.Translators;

/// <summary>
/// Translator for FindDerivedTypes command.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "FindDerivedTypesTranslator")]
public sealed class FindDerivedTypesTranslator : RoslynCommandTranslatorBase<FindDerivedTypesCommand, QueryResult<IReadOnlyList<TypeInfoResult>>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindDerivedTypesTranslator"/> class.
    /// </summary>
    public FindDerivedTypesTranslator()
        : base("FindDerivedTypesTranslator", "Translates FindDerivedTypes command to find derived types")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: resolve type, find derived classes and implementations
    public override async Task<IGenericResult<QueryResult<IReadOnlyList<TypeInfoResult>>>> Translate(
        FindDerivedTypesCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        FindDerivedTypesTranslatorLog.Finding(Logger, command.FilePath, command.Line, command.Column, command.Transitive);

        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
        {
            FindDerivedTypesTranslatorLog.DocumentNotFound(Logger, command.FilePath);
            return GenericResult<QueryResult<IReadOnlyList<TypeInfoResult>>>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));
        }

        var document = solution.GetDocument(documentId);
        if (document is null)
        {
            FindDerivedTypesTranslatorLog.FailedToLoadDocument(Logger, command.FilePath);
            return GenericResult<QueryResult<IReadOnlyList<TypeInfoResult>>>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel is null || syntaxRoot is null)
        {
            FindDerivedTypesTranslatorLog.FailedToAnalyzeDocument(Logger, command.FilePath);
            return GenericResult<QueryResult<IReadOnlyList<TypeInfoResult>>>.Failure(
                RoslynResultCodes.ByName("FailedToAnalyzeDocument"));
        }

        var position = text.Lines.GetPosition(new LinePosition(command.Line - 1, command.Column - 1));
        var token = syntaxRoot.FindToken(position);
        var symbol = semanticModel.GetSymbolInfo(token.Parent!, cancellationToken).Symbol
                  ?? semanticModel.GetDeclaredSymbol(token.Parent!, cancellationToken);

        if (symbol is not INamedTypeSymbol typeSymbol)
        {
            FindDerivedTypesTranslatorLog.SymbolNotType(Logger, command.FilePath, command.Line, command.Column);
            return GenericResult<QueryResult<IReadOnlyList<TypeInfoResult>>>.Failure(
                RoslynResultCodes.ByName("SymbolNotType"));
        }

        var derivedTypes = new List<TypeInfoResult>();

        var foundTypes = await SymbolFinder.FindDerivedClassesAsync(
            typeSymbol, solution, command.Transitive, cancellationToken: cancellationToken).ConfigureAwait(false);

        foreach (var derived in foundTypes)
        {
            if (derived.Locations.Length > 0 && derived.Locations[0].IsInSource)
            {
                var lineSpan = derived.Locations[0].GetLineSpan();
                derivedTypes.Add(new TypeInfoResult
                {
                    Name = derived.Name,
                    FullName = derived.ToDisplayString(),
                    FilePath = lineSpan.Path ?? string.Empty,
                    Line = lineSpan.StartLinePosition.Line + 1,
                    Column = lineSpan.StartLinePosition.Character + 1
                });
            }
        }

        if (typeSymbol.TypeKind == TypeKind.Interface)
        {
            var implementations = await SymbolFinder.FindImplementationsAsync(
                typeSymbol, solution, command.Transitive, cancellationToken: cancellationToken).ConfigureAwait(false);

            foreach (var impl in implementations)
            {
                if (impl.Locations.Length > 0 && impl.Locations[0].IsInSource)
                {
                    var lineSpan = impl.Locations[0].GetLineSpan();
                    derivedTypes.Add(new TypeInfoResult
                    {
                        Name = impl.Name,
                        FullName = impl.ToDisplayString(),
                        FilePath = lineSpan.Path ?? string.Empty,
                        Line = lineSpan.StartLinePosition.Line + 1,
                        Column = lineSpan.StartLinePosition.Character + 1,
                        Relationship = "Implementation"
                    });
                }
            }
        }

        var summary = $"Found {derivedTypes.Count} derived type(s) for '{typeSymbol.Name}'";
        var result = new QueryResult<IReadOnlyList<TypeInfoResult>>(summary, derivedTypes);

        FindDerivedTypesTranslatorLog.Found(Logger, typeSymbol.Name, derivedTypes.Count);

        return GenericResult<QueryResult<IReadOnlyList<TypeInfoResult>>>.Success(result, summary);
    }
#pragma warning restore MA0051
}
