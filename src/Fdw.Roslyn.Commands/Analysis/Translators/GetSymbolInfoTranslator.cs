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
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Analysis.Translators;

/// <summary>
/// Translator for retrieving symbol information.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "GetSymbolInfo")]
public sealed class GetSymbolInfoTranslator
    : RoslynCommandTranslatorBase<GetSymbolInfoCommand, QueryResult<SymbolInfoData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetSymbolInfoTranslator"/> class.
    /// </summary>
    public GetSymbolInfoTranslator()
        : base("GetSymbolInfoTranslator", "Translates symbol info retrieval commands")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: resolve symbol, extract type-specific info, build DTO
    public override async Task<IGenericResult<QueryResult<SymbolInfoData>>> Translate(
        GetSymbolInfoCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        GetSymbolInfoTranslatorLog.Retrieving(Logger, command.FilePath, command.Line, command.Column);

        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
        {
            GetSymbolInfoTranslatorLog.DocumentNotFound(Logger, command.FilePath);
            return GenericResult<QueryResult<SymbolInfoData>>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));
        }

        var document = solution.GetDocument(documentId);
        if (document is null)
        {
            GetSymbolInfoTranslatorLog.FailedToLoadDocument(Logger, command.FilePath);
            return GenericResult<QueryResult<SymbolInfoData>>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel is null || syntaxRoot is null)
        {
            GetSymbolInfoTranslatorLog.FailedToAnalyzeDocument(Logger, command.FilePath);
            return GenericResult<QueryResult<SymbolInfoData>>.Failure(
                RoslynResultCodes.ByName("FailedToAnalyzeDocument"));
        }

        var position = text.Lines.GetPosition(new LinePosition(command.Line - 1, command.Column - 1));
        var token = syntaxRoot.FindToken(position);
        var symbol = semanticModel.GetSymbolInfo(token.Parent!, cancellationToken).Symbol
                  ?? semanticModel.GetDeclaredSymbol(token.Parent!, cancellationToken);

        if (symbol is null)
        {
            GetSymbolInfoTranslatorLog.NoSymbolFoundAtLineColumn(Logger, command.FilePath, command.Line, command.Column);
            return GenericResult<QueryResult<SymbolInfoData>>.Failure(
                RoslynResultCodes.ByName("NoSymbolFoundAtLineColumn"),
                ResultDetails.Create().With("Line", command.Line).With("Column", command.Column));
        }

        var symbolData = new SymbolInfoData
        {
            Name = symbol.Name,
            FullName = symbol.ToDisplayString(),
            Kind = symbol.Kind.ToString(),
            Accessibility = symbol.DeclaredAccessibility.ToString(),
            IsStatic = symbol.IsStatic,
            IsAbstract = symbol.IsAbstract,
            IsVirtual = symbol.IsVirtual,
            IsOverride = symbol.IsOverride,
            IsSealed = symbol.IsSealed,
            IsExtern = symbol.IsExtern,
            IsImplicitlyDeclared = symbol.IsImplicitlyDeclared,
            ContainingNamespace = symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
            ContainingType = symbol.ContainingType?.ToDisplayString() ?? string.Empty
        };

        // Add type-specific info
        var additionalInfo = new Dictionary<string, object>(StringComparer.Ordinal);

        if (symbol is INamedTypeSymbol typeSymbol)
        {
            additionalInfo["typeKind"] = typeSymbol.TypeKind.ToString();
            additionalInfo["isGeneric"] = typeSymbol.IsGenericType;
            additionalInfo["isRecord"] = typeSymbol.IsRecord;
            additionalInfo["baseType"] = typeSymbol.BaseType?.ToDisplayString() ?? string.Empty;
            additionalInfo["interfaces"] = typeSymbol.AllInterfaces.Select(i => i.ToDisplayString()).ToList();
            additionalInfo["typeParameters"] = typeSymbol.TypeParameters.Select(t => t.Name).ToList();
        }
        else if (symbol is IMethodSymbol methodSymbol)
        {
            additionalInfo["returnType"] = methodSymbol.ReturnType.ToDisplayString();
            additionalInfo["isAsync"] = methodSymbol.IsAsync;
            additionalInfo["isExtensionMethod"] = methodSymbol.IsExtensionMethod;
            additionalInfo["methodKind"] = methodSymbol.MethodKind.ToString();
            additionalInfo["parameters"] = methodSymbol.Parameters.Select(p => new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["name"] = p.Name,
                ["type"] = p.Type.ToDisplayString(),
                ["isOptional"] = p.IsOptional,
                ["hasDefaultValue"] = p.HasExplicitDefaultValue
            }).ToList();
        }
        else if (symbol is IPropertySymbol propertySymbol)
        {
            additionalInfo["propertyType"] = propertySymbol.Type.ToDisplayString();
            additionalInfo["isIndexer"] = propertySymbol.IsIndexer;
            additionalInfo["isReadOnly"] = propertySymbol.IsReadOnly;
            additionalInfo["isWriteOnly"] = propertySymbol.IsWriteOnly;
            additionalInfo["hasGetter"] = propertySymbol.GetMethod is not null;
            additionalInfo["hasSetter"] = propertySymbol.SetMethod is not null;
        }
        else if (symbol is IFieldSymbol fieldSymbol)
        {
            additionalInfo["fieldType"] = fieldSymbol.Type.ToDisplayString();
            additionalInfo["isConst"] = fieldSymbol.IsConst;
            additionalInfo["isReadOnly"] = fieldSymbol.IsReadOnly;
            additionalInfo["isVolatile"] = fieldSymbol.IsVolatile;
            if (fieldSymbol.HasConstantValue)
                additionalInfo["constantValue"] = fieldSymbol.ConstantValue?.ToString() ?? "null";
        }

        if (additionalInfo.Count > 0)
            symbolData = symbolData with { AdditionalInfo = additionalInfo };

        // Add location info
        if (symbol.Locations.Length > 0 && symbol.Locations[0].IsInSource)
        {
            var lineSpan = symbol.Locations[0].GetLineSpan();
            symbolData = symbolData with
            {
                DefinitionFile = lineSpan.Path ?? string.Empty,
                DefinitionLine = lineSpan.StartLinePosition.Line + 1,
                DefinitionColumn = lineSpan.StartLinePosition.Character + 1
            };
        }

        // Add documentation
        var docComment = symbol.GetDocumentationCommentXml(cancellationToken: cancellationToken);
        if (!string.IsNullOrEmpty(docComment))
            symbolData = symbolData with { Documentation = docComment };

        var result = new QueryResult<SymbolInfoData>(
            $"Retrieved info for '{symbol.Name}'",
            symbolData);

        GetSymbolInfoTranslatorLog.Retrieved(Logger, symbol.Name);

        return GenericResult<QueryResult<SymbolInfoData>>.Success(result);
    }
#pragma warning restore MA0051
}
