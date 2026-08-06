#pragma warning disable CA1305 // Specify IFormatProvider - project commands use invariant strings

using System;
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

namespace Fdw.Roslyn.Commands.Projects.Translators;

/// <summary>
/// Translator for ListDocumentsCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "ListDocumentsTranslator")]
public sealed class ListDocumentsTranslator : RoslynCommandTranslatorBase<ListDocumentsCommand, QueryResult<DocumentListResult>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListDocumentsTranslator"/> class.
    /// </summary>
    public ListDocumentsTranslator()
        : base("ListDocumentsTranslator", "Translates ListDocumentsCommand to retrieve project documents")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<QueryResult<DocumentListResult>>> Translate(
        ListDocumentsCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        var project = solution.Projects.FirstOrDefault(p =>
            string.Equals(p.Name, command.ProjectName, StringComparison.OrdinalIgnoreCase));

        if (project is null)
        {
            return Task.FromResult<IGenericResult<QueryResult<DocumentListResult>>>(
                GenericResult<QueryResult<DocumentListResult>>.Failure(
                RoslynResultCodes.ByName("ProjectNotFound"),
                ResultDetails.Create().With("ProjectName", command.ProjectName)));
        }

        var documents = project.Documents.AsEnumerable();

        if (!string.IsNullOrEmpty(command.Pattern))
        {
            documents = documents.Where(d =>
                d.Name.Contains(command.Pattern, StringComparison.OrdinalIgnoreCase));
        }

        var documentList = documents
            .Select(d => new DocumentSummary(
                name: d.Name,
                filePath: d.FilePath ?? string.Empty,
                folders: d.Folders.ToList()))
            .ToList();

        var result = new DocumentListResult(project.Name, documentList.Count, documentList);

        var queryResult = new QueryResult<DocumentListResult>(
            $"Found {documentList.Count} documents in {project.Name}",
            result);

        return Task.FromResult(GenericResult<QueryResult<DocumentListResult>>.Success(queryResult));
    }
}
