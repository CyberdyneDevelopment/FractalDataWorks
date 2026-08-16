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
using Fdw.Roslyn.Commands.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.Roslyn.Commands.Analysis.Translators;

/// <summary>
/// Translator for analyzing nullability.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "AnalyzeNullability")]
public sealed class AnalyzeNullabilityTranslator
    : RoslynCommandTranslatorBase<AnalyzeNullabilityCommand, QueryResult<NullabilityAnalysisData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyzeNullabilityTranslator"/> class.
    /// </summary>
    public AnalyzeNullabilityTranslator()
        : base("AnalyzeNullabilityTranslator", "Translates nullability analysis commands")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: iterate parameters, properties, fields, methods for nullability
    public override async Task<IGenericResult<QueryResult<NullabilityAnalysisData>>> Translate(
        AnalyzeNullabilityCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        AnalyzeNullabilityTranslatorLog.Analyzing(Logger, command.FilePath);

        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
        {
            AnalyzeNullabilityTranslatorLog.DocumentNotFound(Logger, command.FilePath);
            return GenericResult<QueryResult<NullabilityAnalysisData>>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));
        }

        var document = solution.GetDocument(documentId);
        if (document is null)
        {
            AnalyzeNullabilityTranslatorLog.FailedToLoadDocument(Logger, command.FilePath);
            return GenericResult<QueryResult<NullabilityAnalysisData>>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel is null || syntaxRoot is null)
        {
            AnalyzeNullabilityTranslatorLog.FailedToAnalyzeDocument(Logger, command.FilePath);
            return GenericResult<QueryResult<NullabilityAnalysisData>>.Failure(
                RoslynResultCodes.ByName("FailedToAnalyzeDocument"));
        }

        var symbols = new List<NullabilitySymbol>();

        // Analyze parameters
        foreach (var param in syntaxRoot.DescendantNodes().OfType<ParameterSyntax>())
        {
            var symbol = semanticModel.GetDeclaredSymbol(param, cancellationToken);
            if (symbol is IParameterSymbol paramSymbol)
            {
                symbols.Add(CreateNullabilityInfo(paramSymbol, "Parameter", param.GetLocation()));
            }
        }

        // Analyze properties
        foreach (var prop in syntaxRoot.DescendantNodes().OfType<PropertyDeclarationSyntax>())
        {
            var symbol = semanticModel.GetDeclaredSymbol(prop, cancellationToken);
            if (symbol is IPropertySymbol propSymbol)
            {
                symbols.Add(CreateNullabilityInfo(propSymbol, "Property", prop.GetLocation()));
            }
        }

        // Analyze fields
        foreach (var field in syntaxRoot.DescendantNodes().OfType<FieldDeclarationSyntax>())
        {
            foreach (var variable in field.Declaration.Variables)
            {
                var symbol = semanticModel.GetDeclaredSymbol(variable, cancellationToken);
                if (symbol is IFieldSymbol fieldSymbol)
                {
                    symbols.Add(CreateNullabilityInfo(fieldSymbol, "Field", variable.GetLocation()));
                }
            }
        }

        // Analyze method return types
        foreach (var method in syntaxRoot.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            var symbol = semanticModel.GetDeclaredSymbol(method, cancellationToken);
            if (symbol is IMethodSymbol methodSymbol)
            {
                symbols.Add(CreateNullabilityInfo(methodSymbol, "MethodReturn", method.GetLocation()));
            }
        }

        var nullableCount = symbols.Count(s => s.IsNullable);
        var nonNullableCount = symbols.Count(s => !s.IsNullable);

        var data = new NullabilityAnalysisData
        {
            Symbols = symbols,
            Summary = new NullabilitySummary
            {
                Total = symbols.Count,
                Nullable = nullableCount,
                NonNullable = nonNullableCount
            }
        };

        var result = new QueryResult<NullabilityAnalysisData>(
            $"Analyzed {symbols.Count} symbols: {nullableCount} nullable, {nonNullableCount} non-nullable",
            data);

        AnalyzeNullabilityTranslatorLog.Analyzed(Logger, command.FilePath, symbols.Count, nullableCount, nonNullableCount);

        return GenericResult<QueryResult<NullabilityAnalysisData>>.Success(result);
    }
#pragma warning restore MA0051

    private static NullabilitySymbol CreateNullabilityInfo(ISymbol symbol, string memberKind, Location location)
    {
        var lineSpan = location.GetLineSpan();
        ITypeSymbol? typeSymbol = symbol switch
        {
            IParameterSymbol p => p.Type,
            IPropertySymbol p => p.Type,
            IFieldSymbol f => f.Type,
            IMethodSymbol m => m.ReturnType,
            _ => null
        };

        var isNullable = typeSymbol?.NullableAnnotation == NullableAnnotation.Annotated;
        var nullableFlow = typeSymbol?.NullableAnnotation.ToString() ?? "Unknown";

        return new NullabilitySymbol
        {
            Name = symbol.Name,
            MemberKind = memberKind,
            TypeName = typeSymbol?.ToDisplayString() ?? "unknown",
            IsNullable = isNullable,
            NullableAnnotation = nullableFlow,
            Line = lineSpan.StartLinePosition.Line + 1,
            Column = lineSpan.StartLinePosition.Character + 1
        };
    }
}
