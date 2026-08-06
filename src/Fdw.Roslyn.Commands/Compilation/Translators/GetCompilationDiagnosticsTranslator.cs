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

namespace Fdw.Roslyn.Commands.Compilation.Translators;

/// <summary>
/// Translator for getting compilation diagnostics.
/// </summary>
// Why: renamed from GetDiagnostics to GetCompilationDiagnostics to match the renamed command (avoids
// TypeOption name collision when Analysis and Compilation packages were folded into one assembly).
[TypeOption(typeof(RoslynCommandTranslators), "GetCompilationDiagnostics")]
public sealed class GetCompilationDiagnosticsTranslator
    : RoslynCommandTranslatorBase<GetCompilationDiagnosticsCommand, QueryResult<DiagnosticsData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetCompilationDiagnosticsTranslator"/> class.
    /// </summary>
    public GetCompilationDiagnosticsTranslator()
        : base("GetCompilationDiagnosticsTranslator", "Translates get compilation diagnostics commands")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: resolve target, get diagnostics, filter and map results
    public override async Task<IGenericResult<QueryResult<DiagnosticsData>>> Translate(
        GetCompilationDiagnosticsCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        var minSeverity = command.Severity.ToUpperInvariant() switch
        {
            "HIDDEN" => DiagnosticSeverity.Hidden,
            "INFO" => DiagnosticSeverity.Info,
            "WARNING" => DiagnosticSeverity.Warning,
            "ERROR" => DiagnosticSeverity.Error,
            _ => DiagnosticSeverity.Warning
        };

        IEnumerable<Diagnostic> diagnostics;
        string? filePath = null;
        string? projectName = null;

        if (!string.IsNullOrEmpty(command.FilePath))
        {
            filePath = command.FilePath;
            var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
            if (documentId is null)
                return GenericResult<QueryResult<DiagnosticsData>>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));

            var document = solution.GetDocument(documentId);
            if (document is null)
                return GenericResult<QueryResult<DiagnosticsData>>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel is null)
                return GenericResult<QueryResult<DiagnosticsData>>.Failure(
                RoslynResultCodes.ByName("FailedToGetSemanticModel"));

            diagnostics = semanticModel.GetDiagnostics(cancellationToken: cancellationToken);
        }
        else if (!string.IsNullOrEmpty(command.ProjectName))
        {
            projectName = command.ProjectName;
            var project = solution.Projects.FirstOrDefault(p =>
                string.Equals(p.Name, command.ProjectName, StringComparison.OrdinalIgnoreCase));

            if (project is null)
                return GenericResult<QueryResult<DiagnosticsData>>.Failure(
                RoslynResultCodes.ByName("ProjectNotFound"),
                ResultDetails.Create().With("ProjectName", command.ProjectName));

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (compilation is null)
                return GenericResult<QueryResult<DiagnosticsData>>.Failure(
                RoslynResultCodes.ByName("FailedToGetCompilation"));

            diagnostics = compilation.GetDiagnostics(cancellationToken);
        }
        else
        {
            return GenericResult<QueryResult<DiagnosticsData>>.Failure(
                RoslynResultCodes.ByName("EitherFilePathOrProjectNameRequired"));
        }

        var filteredDiagnostics = diagnostics
            .Where(d => d.Severity >= minSeverity)
            .ToList();

        var diagnosticList = filteredDiagnostics.Select(d =>
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
        }).ToList();

        var data = new DiagnosticsData
        {
            FilePath = filePath,
            ProjectName = projectName,
            DiagnosticCount = filteredDiagnostics.Count,
            Diagnostics = diagnosticList,
            MinSeverity = command.Severity
        };

        var result = new QueryResult<DiagnosticsData>(
            $"Found {filteredDiagnostics.Count} diagnostics",
            data);

        return GenericResult<QueryResult<DiagnosticsData>>.Success(result);
    }
#pragma warning restore MA0051
}
