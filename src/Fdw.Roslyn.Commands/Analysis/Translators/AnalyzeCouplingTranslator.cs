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
/// Translator for analyzing type coupling.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "AnalyzeCoupling")]
public sealed class AnalyzeCouplingTranslator
    : RoslynCommandTranslatorBase<AnalyzeCouplingCommand, QueryResult<CouplingAnalysisData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyzeCouplingTranslator"/> class.
    /// </summary>
    public AnalyzeCouplingTranslator()
        : base("AnalyzeCouplingTranslator", "Translates coupling analysis commands")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: calculate efferent/afferent coupling, compute instability
    public override async Task<IGenericResult<QueryResult<CouplingAnalysisData>>> Translate(
        AnalyzeCouplingCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
            return GenericResult<QueryResult<CouplingAnalysisData>>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));

        var document = solution.GetDocument(documentId);
        if (document is null)
            return GenericResult<QueryResult<CouplingAnalysisData>>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel is null || syntaxRoot is null)
            return GenericResult<QueryResult<CouplingAnalysisData>>.Failure(
                RoslynResultCodes.ByName("FailedToAnalyzeDocument"));

        var position = text.Lines.GetPosition(new LinePosition(command.Line - 1, command.Column - 1));
        var token = syntaxRoot.FindToken(position);
        var symbol = semanticModel.GetSymbolInfo(token.Parent!, cancellationToken).Symbol
                  ?? semanticModel.GetDeclaredSymbol(token.Parent!, cancellationToken);

        if (symbol is not INamedTypeSymbol typeSymbol)
            return GenericResult<QueryResult<CouplingAnalysisData>>.Failure(
                RoslynResultCodes.ByName("SymbolNotType"));

        // Calculate Efferent Coupling (Ce)
        var efferentTypes = new HashSet<string>(StringComparer.Ordinal);
        var efferentDetails = new List<TypeReference>();

        var typeDecl = token.Parent?.AncestorsAndSelf()
            .OfType<TypeDeclarationSyntax>()
            .FirstOrDefault();

        if (typeDecl is not null)
        {
            foreach (var node in typeDecl.DescendantNodes())
            {
                var nodeSymbol = semanticModel.GetSymbolInfo(node, cancellationToken).Symbol;
                INamedTypeSymbol? referencedType = nodeSymbol switch
                {
                    INamedTypeSymbol t => t,
                    IMethodSymbol m => m.ContainingType,
                    IPropertySymbol p => p.ContainingType,
                    IFieldSymbol f => f.ContainingType,
                    _ => null
                };

                if (referencedType is not null &&
                    !SymbolEqualityComparer.Default.Equals(referencedType, typeSymbol) &&
                    !referencedType.ToDisplayString().StartsWith("System.", StringComparison.Ordinal))
                {
                    var fullName = referencedType.ToDisplayString();
                    if (efferentTypes.Add(fullName))
                    {
                        efferentDetails.Add(new TypeReference
                        {
                            Name = referencedType.Name,
                            FullName = fullName,
                            Namespace = referencedType.ContainingNamespace?.ToDisplayString() ?? string.Empty
                        });
                    }
                }
            }
        }

        // Calculate Afferent Coupling (Ca)
        var afferentTypes = new HashSet<string>(StringComparer.Ordinal);
        var afferentDetails = new List<TypeReference>();

        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (compilation is null)
                continue;

            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var model = compilation.GetSemanticModel(syntaxTree);
                var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

                foreach (var typeRef in root.DescendantNodes().OfType<TypeSyntax>())
                {
                    var refSymbol = model.GetSymbolInfo(typeRef, cancellationToken).Symbol;
                    if (refSymbol is INamedTypeSymbol refType &&
                        SymbolEqualityComparer.Default.Equals(refType, typeSymbol))
                    {
                        var containingDecl = typeRef.AncestorsAndSelf()
                            .OfType<TypeDeclarationSyntax>()
                            .FirstOrDefault();

                        if (containingDecl is not null)
                        {
                            var containingSymbol = model.GetDeclaredSymbol(containingDecl, cancellationToken);
                            if (containingSymbol is INamedTypeSymbol containingType &&
                                !SymbolEqualityComparer.Default.Equals(containingType, typeSymbol))
                            {
                                var fullName = containingType.ToDisplayString();
                                if (afferentTypes.Add(fullName))
                                {
                                    afferentDetails.Add(new TypeReference
                                    {
                                        Name = containingType.Name,
                                        FullName = fullName,
                                        Namespace = containingType.ContainingNamespace?.ToDisplayString() ?? string.Empty
                                    });
                                }
                            }
                        }
                    }
                }
            }
        }

        var ce = efferentDetails.Count;
        var ca = afferentDetails.Count;
        var instability = (ce + ca) > 0 ? (double)ce / (ce + ca) : 0.0;

        var data = new CouplingAnalysisData
        {
            TypeName = typeSymbol.ToDisplayString(),
            EfferentCoupling = ce,
            AfferentCoupling = ca,
            Instability = instability,
            EfferentTypes = efferentDetails,
            AfferentTypes = afferentDetails
        };

        var result = new QueryResult<CouplingAnalysisData>(
            $"Coupling for '{typeSymbol.Name}': Ce={ce}, Ca={ca}, Instability={instability:F2}",
            data);

        return GenericResult<QueryResult<CouplingAnalysisData>>.Success(result);
    }
#pragma warning restore MA0051
}
