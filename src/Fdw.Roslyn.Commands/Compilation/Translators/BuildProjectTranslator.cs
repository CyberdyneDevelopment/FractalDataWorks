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
/// Translator for building a project.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "BuildProject")]
public sealed class BuildProjectTranslator
    : RoslynCommandTranslatorBase<BuildProjectCommand, QueryResult<BuildProjectData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BuildProjectTranslator"/> class.
    /// </summary>
    public BuildProjectTranslator()
        : base("BuildProjectTranslator", "Translates build project commands")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: get compilation, collect diagnostics, build result DTO
    public override async Task<IGenericResult<QueryResult<BuildProjectData>>> Translate(
        BuildProjectCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        var project = solution.Projects.FirstOrDefault(p =>
            string.Equals(p.Name, command.ProjectName, StringComparison.OrdinalIgnoreCase));

        if (project is null)
            return GenericResult<QueryResult<BuildProjectData>>.Failure(
                RoslynResultCodes.ByName("ProjectNotFound"),
                ResultDetails.Create().With("ProjectName", command.ProjectName));

        var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        if (compilation is null)
            return GenericResult<QueryResult<BuildProjectData>>.Failure(
                RoslynResultCodes.ByName("FailedToGetCompilation"));

        var diagnostics = compilation.GetDiagnostics(cancellationToken);

        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        var warnings = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning).ToList();

        var errorList = errors.Select(e =>
        {
            var lineSpan = e.Location.GetLineSpan();
            return new DiagnosticInfo
            {
                Id = e.Id,
                Message = e.GetMessage(),
                Severity = e.Severity.ToString(),
                FilePath = lineSpan.Path ?? string.Empty,
                Line = lineSpan.StartLinePosition.Line + 1,
                Column = lineSpan.StartLinePosition.Character + 1,
                Category = e.Descriptor.Category
            };
        }).ToList();

        var warningList = warnings.Select(w =>
        {
            var lineSpan = w.Location.GetLineSpan();
            return new DiagnosticInfo
            {
                Id = w.Id,
                Message = w.GetMessage(),
                Severity = w.Severity.ToString(),
                FilePath = lineSpan.Path ?? string.Empty,
                Line = lineSpan.StartLinePosition.Line + 1,
                Column = lineSpan.StartLinePosition.Character + 1,
                Category = w.Descriptor.Category
            };
        }).ToList();

        var data = new BuildProjectData
        {
            ProjectName = command.ProjectName,
            Success = errors.Count == 0,
            ErrorCount = errors.Count,
            WarningCount = warnings.Count,
            Errors = errorList,
            Warnings = warningList
        };

        var summary = errors.Count > 0
            ? $"Build failed with {errors.Count} errors"
            : $"Build succeeded with {warnings.Count} warnings";

        var result = new QueryResult<BuildProjectData>(summary, data);

        return GenericResult<QueryResult<BuildProjectData>>.Success(result);
    }
#pragma warning restore MA0051
}
