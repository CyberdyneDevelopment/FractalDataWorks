#pragma warning disable CA1305 // Specify IFormatProvider - workspace commands use invariant strings

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Workspace.Commands;
using Fdw.Roslyn.Commands.Workspace.Results;
using Fdw.Workspace.Roslyn;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Workspace.Translators;

/// <summary>
/// Translator for getting workspace information.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "GetWorkspaceInfo")]
public sealed class GetWorkspaceInfoTranslator
    : RoslynCommandTranslatorBase<GetWorkspaceInfoCommand, QueryResult<WorkspaceInfoData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetWorkspaceInfoTranslator"/> class.
    /// </summary>
    public GetWorkspaceInfoTranslator()
        : base("GetWorkspaceInfoTranslator", "Translates get workspace info commands")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<QueryResult<WorkspaceInfoData>>> Translate(
        GetWorkspaceInfoCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        var projectCount = solution.Projects.Count();
        var documentCount = solution.Projects.Sum(p => p.Documents.Count());

        var projects = solution.Projects.Select(p => new Results.ProjectInfo
        {
            Name = p.Name,
            DocumentCount = p.Documents.Count(),
            Language = p.Language
        }).ToList();

        var data = new WorkspaceInfoData
        {
            SolutionFilePath = solution.FilePath ?? string.Empty,
            ProjectCount = projectCount,
            DocumentCount = documentCount,
            Projects = projects
        };

        var result = new QueryResult<WorkspaceInfoData>(
            $"Workspace contains {projectCount} projects and {documentCount} documents",
            data);

        return Task.FromResult<IGenericResult<QueryResult<WorkspaceInfoData>>>(
            GenericResult<QueryResult<WorkspaceInfoData>>.Success(result));
    }
}
