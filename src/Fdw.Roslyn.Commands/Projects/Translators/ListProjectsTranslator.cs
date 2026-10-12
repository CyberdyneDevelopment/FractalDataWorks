#pragma warning disable CA1305 // Specify IFormatProvider - project commands use invariant strings

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
/// Translator for ListProjectsCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "ListProjectsTranslator")]
public sealed class ListProjectsTranslator : RoslynCommandTranslatorBase<ListProjectsCommand, QueryResult<ProjectListResult>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListProjectsTranslator"/> class.
    /// </summary>
    public ListProjectsTranslator()
        : base("ListProjectsTranslator", "Translates ListProjectsCommand to retrieve all projects")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<QueryResult<ProjectListResult>>> Translate(
        ListProjectsCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        ListProjectsTranslatorLog.Listing(Logger);

        var projects = solution.Projects
            .Select(p => new ProjectSummary(
                name: p.Name,
                filePath: p.FilePath ?? string.Empty,
                language: p.Language,
                documentCount: p.Documents.Count(),
                outputKind: p.CompilationOptions?.OutputKind.ToString() ?? "Unknown"))
            .ToList();

        var result = new ProjectListResult(projects.Count, projects);

        var queryResult = new QueryResult<ProjectListResult>(
            $"Found {projects.Count} projects",
            result);

        ListProjectsTranslatorLog.Listed(Logger, projects.Count);

        return Task.FromResult(GenericResult<QueryResult<ProjectListResult>>.Success(queryResult));
    }
}
