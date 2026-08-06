using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Analysis.Commands;
using Fdw.Roslyn.Commands.Analysis.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Analysis.Translators;

/// <summary>
/// Translator for analyzing type dependencies.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "AnalyzeDependencies")]
public sealed class AnalyzeDependenciesTranslator
    : RoslynCommandTranslatorBase<AnalyzeDependenciesCommand, QueryResult<DependencyAnalysisData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyzeDependenciesTranslator"/> class.
    /// </summary>
    public AnalyzeDependenciesTranslator()
        : base("AnalyzeDependenciesTranslator", "Translates dependency analysis commands")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: resolve type, iterate descendant nodes, collect dependencies
    public override async Task<IGenericResult<QueryResult<DependencyAnalysisData>>> Translate(
        AnalyzeDependenciesCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
            return GenericResult<QueryResult<DependencyAnalysisData>>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));

        var document = solution.GetDocument(documentId);
        if (document is null)
            return GenericResult<QueryResult<DependencyAnalysisData>>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel is null || syntaxRoot is null)
            return GenericResult<QueryResult<DependencyAnalysisData>>.Failure(
                RoslynResultCodes.ByName("FailedToAnalyzeDocument"));

        var position = text.Lines.GetPosition(new LinePosition(command.Line - 1, command.Column - 1));
        var token = syntaxRoot.FindToken(position);
        var symbol = semanticModel.GetSymbolInfo(token.Parent!, cancellationToken).Symbol
                  ?? semanticModel.GetDeclaredSymbol(token.Parent!, cancellationToken);

        if (symbol is not INamedTypeSymbol typeSymbol)
            return GenericResult<QueryResult<DependencyAnalysisData>>.Failure(
                RoslynResultCodes.ByName("SymbolNotType"));

        var dependencies = new HashSet<string>(StringComparer.Ordinal);
        var dependencyDetails = new List<TypeDependency>();

        var typeDecl = token.Parent?.AncestorsAndSelf()
            .OfType<TypeDeclarationSyntax>()
            .FirstOrDefault();

        if (typeDecl is not null)
        {
            foreach (var node in typeDecl.DescendantNodes())
            {
                var nodeSymbol = semanticModel.GetSymbolInfo(node, cancellationToken).Symbol;
                if (nodeSymbol is INamedTypeSymbol referencedType &&
                    !SymbolEqualityComparer.Default.Equals(referencedType, typeSymbol))
                {
                    var fullName = referencedType.ToDisplayString();
                    if (!command.IncludeSystemTypes && fullName.StartsWith("System.", StringComparison.Ordinal))
                        continue;

                    if (dependencies.Add(fullName))
                    {
                        dependencyDetails.Add(new TypeDependency
                        {
                            Name = referencedType.Name,
                            FullName = fullName,
                            Kind = referencedType.TypeKind.ToString(),
                            Namespace = referencedType.ContainingNamespace?.ToDisplayString() ?? string.Empty
                        });
                    }
                }
            }
        }

        var data = new DependencyAnalysisData
        {
            TypeName = typeSymbol.ToDisplayString(),
            Dependencies = dependencyDetails,
            Count = dependencyDetails.Count
        };

        var result = new QueryResult<DependencyAnalysisData>(
            $"Found {dependencyDetails.Count} dependencies for '{typeSymbol.Name}'",
            data);

        return GenericResult<QueryResult<DependencyAnalysisData>>.Success(result);
    }
#pragma warning restore MA0051
}
