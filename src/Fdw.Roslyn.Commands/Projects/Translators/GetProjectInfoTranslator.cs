#pragma warning disable CA1305 // Specify IFormatProvider - project commands use invariant strings

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Projects.Commands;
using Fdw.Roslyn.Commands.Projects.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Fdw.Roslyn.Commands.Projects.Translators;

/// <summary>
/// Translator for GetProjectInfoCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "GetProjectInfoTranslator")]
public sealed class GetProjectInfoTranslator : RoslynCommandTranslatorBase<GetProjectInfoCommand, QueryResult<ProjectInfoResult>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetProjectInfoTranslator"/> class.
    /// </summary>
    public GetProjectInfoTranslator()
        : base("GetProjectInfoTranslator", "Translates GetProjectInfoCommand to retrieve project information")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<QueryResult<ProjectInfoResult>>> Translate(
        GetProjectInfoCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        var project = solution.Projects.FirstOrDefault(p =>
            string.Equals(p.Name, command.ProjectName, StringComparison.OrdinalIgnoreCase));

        if (project is null)
        {
            return Task.FromResult<IGenericResult<QueryResult<ProjectInfoResult>>>(
                GenericResult<QueryResult<ProjectInfoResult>>.Failure(
                RoslynResultCodes.ByName("ProjectNotFound"),
                ResultDetails.Create().With("ProjectName", command.ProjectName)));
        }

        var parseOptions = project.ParseOptions as CSharpParseOptions;
        var compilationOptions = project.CompilationOptions as CSharpCompilationOptions;

        var projectReferences = project.ProjectReferences
            .Select(r => solution.GetProject(r.ProjectId)?.Name ?? "Unknown")
            .ToList();

        var metadataReferences = project.MetadataReferences
            .Select(r => Path.GetFileName(r.Display ?? "Unknown"))
            .ToList();

        var result = new ProjectInfoResult(
            name: project.Name,
            filePath: project.FilePath ?? string.Empty,
            language: project.Language,
            outputKind: project.CompilationOptions?.OutputKind.ToString() ?? "Unknown",
            languageVersion: parseOptions?.LanguageVersion.ToString() ?? "Unknown",
            nullableContextOptions: compilationOptions?.NullableContextOptions.ToString() ?? "Unknown",
            allowUnsafe: compilationOptions?.AllowUnsafe ?? false,
            documentCount: project.Documents.Count(),
            additionalDocumentCount: project.AdditionalDocuments.Count(),
            projectReferences: projectReferences,
            metadataReferences: metadataReferences);

        var queryResult = new QueryResult<ProjectInfoResult>(
            $"Retrieved info for project {project.Name}",
            result);

        return Task.FromResult(GenericResult<QueryResult<ProjectInfoResult>>.Success(queryResult));
    }
}
