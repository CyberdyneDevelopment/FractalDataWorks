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

namespace Fdw.Roslyn.Commands.Compilation.Translators;

/// <summary>
/// Translator for compiling a document.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "CompileDocument")]
public sealed class CompileDocumentTranslator
    : RoslynCommandTranslatorBase<CompileDocumentCommand, QueryResult<CompileDocumentData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompileDocumentTranslator"/> class.
    /// </summary>
    public CompileDocumentTranslator()
        : base("CompileDocumentTranslator", "Translates compile document commands")
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<QueryResult<CompileDocumentData>>> Translate(
        CompileDocumentCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        CompileDocumentTranslatorLog.Compiling(Logger, command.FilePath);

        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
        {
            CompileDocumentTranslatorLog.DocumentNotFound(Logger, command.FilePath);
            return GenericResult<QueryResult<CompileDocumentData>>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));
        }

        var document = solution.GetDocument(documentId);
        if (document is null)
        {
            CompileDocumentTranslatorLog.FailedToLoadDocument(Logger, command.FilePath);
            return GenericResult<QueryResult<CompileDocumentData>>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));
        }

        var project = document.Project;
        var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

        if (compilation is null)
        {
            CompileDocumentTranslatorLog.FailedToGetCompilation(Logger, command.FilePath);
            return GenericResult<QueryResult<CompileDocumentData>>.Failure(
                RoslynResultCodes.ByName("FailedToGetCompilation"));
        }

        var diagnostics = compilation.GetDiagnostics(cancellationToken);

        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        var warnings = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning).ToList();

        var diagnosticList = diagnostics
            .Where(d => d.Severity >= DiagnosticSeverity.Warning)
            .Select(d =>
            {
                var lineSpan = d.Location.GetLineSpan();
                return new DiagnosticInfo
                {
                    Id = d.Id,
                    Message = d.GetMessage(),
                    Severity = d.Severity.ToString(),
                    FilePath = lineSpan.Path ?? string.Empty,
                    Line = lineSpan.StartLinePosition.Line + 1,
                    Column = lineSpan.StartLinePosition.Character + 1,
                    Category = d.Descriptor.Category
                };
            })
            .ToList();

        var data = new CompileDocumentData
        {
            FilePath = command.FilePath,
            Success = errors.Count == 0,
            ErrorCount = errors.Count,
            WarningCount = warnings.Count,
            Diagnostics = diagnosticList
        };

        var summary = errors.Count > 0
            ? $"Compilation failed with {errors.Count} errors"
            : $"Compilation succeeded with {warnings.Count} warnings";

        var result = new QueryResult<CompileDocumentData>(summary, data);

        CompileDocumentTranslatorLog.Compiled(Logger, command.FilePath, errors.Count, warnings.Count);

        return GenericResult<QueryResult<CompileDocumentData>>.Success(result);
    }
}
