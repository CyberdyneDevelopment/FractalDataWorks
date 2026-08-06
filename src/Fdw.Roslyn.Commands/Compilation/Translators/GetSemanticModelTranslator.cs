#pragma warning disable CA1305 // Specify IFormatProvider - code compilation uses invariant strings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Compilation.Commands;
using Fdw.Roslyn.Commands.Compilation.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.Roslyn.Commands.Compilation.Translators;

/// <summary>
/// Translator for getting semantic model.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "GetSemanticModel")]
public sealed class GetSemanticModelTranslator
    : RoslynCommandTranslatorBase<GetSemanticModelCommand, QueryResult<SemanticModelData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetSemanticModelTranslator"/> class.
    /// </summary>
    public GetSemanticModelTranslator()
        : base("GetSemanticModelTranslator", "Translates get semantic model commands")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: get semantic model, collect type declarations
    public override async Task<IGenericResult<QueryResult<SemanticModelData>>> Translate(
        GetSemanticModelCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
            return GenericResult<QueryResult<SemanticModelData>>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));

        var document = solution.GetDocument(documentId);
        if (document is null)
            return GenericResult<QueryResult<SemanticModelData>>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel is null || syntaxRoot is null)
            return GenericResult<QueryResult<SemanticModelData>>.Failure(
                RoslynResultCodes.ByName("FailedToGetSemanticModel"));

        var declaredSymbols = new List<SymbolDeclaration>();

        // Collect type declarations
        foreach (var typeDecl in syntaxRoot.DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            var symbol = semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);
            if (symbol is not null)
            {
                declaredSymbols.Add(new SymbolDeclaration
                {
                    Name = symbol.Name,
                    Kind = symbol.Kind.ToString(),
                    FullyQualifiedName = symbol.ToDisplayString(),
                    Accessibility = symbol.DeclaredAccessibility.ToString(),
                    IsAbstract = symbol.IsAbstract,
                    IsSealed = symbol.IsSealed,
                    IsStatic = symbol.IsStatic
                });
            }
        }

        // Collect method declarations
        foreach (var methodDecl in syntaxRoot.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            if (semanticModel.GetDeclaredSymbol(methodDecl, cancellationToken) is not IMethodSymbol methodSymbol)
                continue;

            declaredSymbols.Add(new SymbolDeclaration
            {
                Name = methodSymbol.Name,
                Kind = methodSymbol.Kind.ToString(),
                FullyQualifiedName = methodSymbol.ToDisplayString(),
                Accessibility = methodSymbol.DeclaredAccessibility.ToString(),
                ReturnType = methodSymbol.ReturnType.ToDisplayString(),
                IsAsync = methodSymbol.IsAsync,
                IsVirtual = methodSymbol.IsVirtual,
                IsOverride = methodSymbol.IsOverride
            });
        }

        var referencedAssemblies = semanticModel.Compilation.ReferencedAssemblyNames
            .Select(a => a.Name)
            .ToList();

        var data = new SemanticModelData
        {
            FilePath = command.FilePath,
            DeclaredSymbols = declaredSymbols,
            ReferencedAssemblies = referencedAssemblies,
            LanguageVersion = (semanticModel.Compilation as CSharpCompilation)?.LanguageVersion.ToString() ?? "Unknown"
        };

        var result = new QueryResult<SemanticModelData>(
            $"Retrieved semantic model for {command.FilePath}",
            data);

        return GenericResult<QueryResult<SemanticModelData>>.Success(result);
    }
#pragma warning restore MA0051
}
