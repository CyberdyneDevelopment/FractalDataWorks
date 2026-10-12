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
using Fdw.Roslyn.Commands.Logging;
using Fdw.Roslyn.Commands.Projects.Commands;
using Fdw.Roslyn.Commands.Projects.Results;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Projects.Translators;

/// <summary>
/// Translator for ListReferencesCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "ListReferencesTranslator")]
public sealed class ListReferencesTranslator : RoslynCommandTranslatorBase<ListReferencesCommand, QueryResult<ReferenceListResult>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListReferencesTranslator"/> class.
    /// </summary>
    public ListReferencesTranslator()
        : base("ListReferencesTranslator", "Translates ListReferencesCommand to retrieve project references")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<QueryResult<ReferenceListResult>>> Translate(
        ListReferencesCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        ListReferencesTranslatorLog.Listing(Logger, command.ProjectName);

        var project = solution.Projects.FirstOrDefault(p =>
            string.Equals(p.Name, command.ProjectName, StringComparison.OrdinalIgnoreCase));

        if (project is null)
        {
            ListReferencesTranslatorLog.ProjectNotFound(Logger, command.ProjectName);
            return Task.FromResult<IGenericResult<QueryResult<ReferenceListResult>>>(
                GenericResult<QueryResult<ReferenceListResult>>.Failure(
                RoslynResultCodes.ByName("ProjectNotFound"),
                ResultDetails.Create().With("ProjectName", command.ProjectName)));
        }

        var projectReferences = project.ProjectReferences
            .Select(r =>
            {
                var refProject = solution.GetProject(r.ProjectId);
                return new ReferenceSummary(
                    type: "Project",
                    name: refProject?.Name ?? "Unknown",
                    filePath: refProject?.FilePath ?? string.Empty);
            })
            .ToList();

        var metadataReferences = project.MetadataReferences
            .Select(r => new ReferenceSummary(
                type: "Assembly",
                name: Path.GetFileName(r.Display ?? "Unknown"),
                filePath: r.Display ?? string.Empty))
            .ToList();

        var allReferences = projectReferences.Concat(metadataReferences).ToList();

        var result = new ReferenceListResult(
            projectName: project.Name,
            referenceCount: allReferences.Count,
            projectReferenceCount: projectReferences.Count,
            metadataReferenceCount: metadataReferences.Count,
            references: allReferences);

        var queryResult = new QueryResult<ReferenceListResult>(
            $"Found {allReferences.Count} references in {project.Name}",
            result);

        ListReferencesTranslatorLog.Listed(Logger, project.Name, allReferences.Count);

        return Task.FromResult(GenericResult<QueryResult<ReferenceListResult>>.Success(queryResult));
    }
}
