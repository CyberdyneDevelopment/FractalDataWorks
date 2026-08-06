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

namespace Fdw.Roslyn.Commands.Analysis.Translators;

/// <summary>
/// Translator for retrieving diagnostics.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "GetDiagnostics")]
public sealed class GetDiagnosticsTranslator
    : RoslynCommandTranslatorBase<GetDiagnosticsCommand, QueryResult<DiagnosticsData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetDiagnosticsTranslator"/> class.
    /// </summary>
    public GetDiagnosticsTranslator()
        : base("GetDiagnosticsTranslator", "Translates diagnostics retrieval commands")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: resolve target, get diagnostics, filter by severity
    public override async Task<IGenericResult<QueryResult<DiagnosticsData>>> Translate(
        GetDiagnosticsCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<DiagnosticSeverity>(command.Severity, ignoreCase: true, out var minSeverity))
            minSeverity = DiagnosticSeverity.Warning;

        var diagnostics = new List<DiagnosticInfo>();

        if (!string.IsNullOrEmpty(command.FilePath))
        {
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
            if (semanticModel is not null)
            {
                foreach (var d in semanticModel.GetDiagnostics(cancellationToken: cancellationToken)
                    .Where(d => d.Severity >= minSeverity))
                {
                    diagnostics.Add(CreateDiagnosticInfo(d));
                }
            }
        }
        else
        {
            var projects = string.IsNullOrEmpty(command.ProjectName)
                ? solution.Projects
                : solution.Projects.Where(p => string.Equals(p.Name, command.ProjectName, StringComparison.Ordinal));

            foreach (var project in projects)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                if (compilation is not null)
                {
                    foreach (var d in compilation.GetDiagnostics(cancellationToken)
                        .Where(d => d.Severity >= minSeverity))
                    {
                        diagnostics.Add(CreateDiagnosticInfo(d));
                    }
                }
            }
        }

        var summary = diagnostics
            .GroupBy(d => d.Severity, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        var data = new DiagnosticsData
        {
            Diagnostics = diagnostics,
            Summary = summary,
            Count = diagnostics.Count
        };

        var result = new QueryResult<DiagnosticsData>(
            $"Found {diagnostics.Count} diagnostics",
            data);

        return GenericResult<QueryResult<DiagnosticsData>>.Success(result);
    }
#pragma warning restore MA0051

    private static DiagnosticInfo CreateDiagnosticInfo(Diagnostic diagnostic)
    {
        var lineSpan = diagnostic.Location.GetLineSpan();
        return new DiagnosticInfo
        {
            Id = diagnostic.Id,
            Message = diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture),
            Severity = diagnostic.Severity.ToString(),
            FilePath = lineSpan.Path ?? string.Empty,
            Line = lineSpan.StartLinePosition.Line + 1,
            Column = lineSpan.StartLinePosition.Character + 1
        };
    }
}
