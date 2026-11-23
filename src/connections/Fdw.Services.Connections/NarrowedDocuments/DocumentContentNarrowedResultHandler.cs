using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Documents.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Connections.NarrowedDocuments;

/// <summary>Makes the path of a <see cref="DocumentContent"/> relative to the folder.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(NarrowedResultHandlers), "DocumentContent")]
public sealed class DocumentContentNarrowedResultHandler : NarrowedResultHandlerBase<DocumentContent>
{
    /// <summary>Initializes a new instance of the <see cref="DocumentContentNarrowedResultHandler"/> class.</summary>
    public DocumentContentNarrowedResultHandler()
        : base("DocumentContent")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<DocumentContent> Narrow(DocumentContent value, DocumentScope folder) =>
        NarrowedPaths.RelativeContent(folder, value);
}
