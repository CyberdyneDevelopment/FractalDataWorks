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
using Fdw.Roslyn.Commands.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Fdw.Roslyn.Commands.Compilation.Translators;

/// <summary>
/// Translator for validating syntax.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "ValidateSyntax")]
public sealed class ValidateSyntaxTranslator
    : RoslynCommandTranslatorBase<ValidateSyntaxCommand, QueryResult<ValidateSyntaxData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateSyntaxTranslator"/> class.
    /// </summary>
    public ValidateSyntaxTranslator()
        : base("ValidateSyntaxTranslator", "Translates validate syntax commands")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: parse syntax tree, collect diagnostics and errors
    public override async Task<IGenericResult<QueryResult<ValidateSyntaxData>>> Translate(
        ValidateSyntaxCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        SyntaxTree syntaxTree;
        string? filePath = null;

        ValidateSyntaxTranslatorLog.Validating(Logger, command.FilePath ?? string.Empty, !string.IsNullOrEmpty(command.Code));

        if (!string.IsNullOrEmpty(command.FilePath))
        {
            filePath = command.FilePath;
            var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();

            if (documentId is null)
            {
                ValidateSyntaxTranslatorLog.DocumentNotFound(Logger, command.FilePath);
                return GenericResult<QueryResult<ValidateSyntaxData>>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));
            }

            var document = solution.GetDocument(documentId);
            if (document is null)
            {
                ValidateSyntaxTranslatorLog.FailedToLoadDocument(Logger, command.FilePath);
                return GenericResult<QueryResult<ValidateSyntaxData>>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));
            }

            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            if (tree is null)
            {
                ValidateSyntaxTranslatorLog.FailedToGetSyntaxTree(Logger, command.FilePath);
                return GenericResult<QueryResult<ValidateSyntaxData>>.Failure(
                RoslynResultCodes.ByName("FailedToGetSyntaxTree"));
            }

            syntaxTree = tree;
        }
        else if (!string.IsNullOrEmpty(command.Code))
        {
            syntaxTree = CSharpSyntaxTree.ParseText(command.Code, cancellationToken: cancellationToken);
        }
        else
        {
            ValidateSyntaxTranslatorLog.EitherFilePathOrCodeRequired(Logger);
            return GenericResult<QueryResult<ValidateSyntaxData>>.Failure(
                RoslynResultCodes.ByName("EitherFilePathOrCodeRequired"));
        }

        var diagnostics = syntaxTree.GetDiagnostics(cancellationToken).ToList();
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

        var errorList = errors.Select(d =>
        {
            var lineSpan = d.Location.GetLineSpan();
            return new CompilationDiagnosticInfo
            {
                Id = d.Id,
                Message = d.GetMessage(),
                Severity = d.Severity.ToString(),
                FilePath = lineSpan.Path ?? string.Empty,
                Line = lineSpan.StartLinePosition.Line + 1,
                Column = lineSpan.StartLinePosition.Character + 1,
                Category = string.Empty
            };
        }).ToList();

        var isValid = errors.Count == 0;

        var data = new ValidateSyntaxData
        {
            IsValid = isValid,
            ErrorCount = errors.Count,
            Errors = errorList,
            FilePath = filePath
        };

        var summary = isValid
            ? "Syntax is valid"
            : $"Found {errors.Count} syntax errors";

        var result = new QueryResult<ValidateSyntaxData>(summary, data);

        ValidateSyntaxTranslatorLog.Validated(Logger, isValid, errors.Count);

        return GenericResult<QueryResult<ValidateSyntaxData>>.Success(result);
    }
#pragma warning restore MA0051
}
