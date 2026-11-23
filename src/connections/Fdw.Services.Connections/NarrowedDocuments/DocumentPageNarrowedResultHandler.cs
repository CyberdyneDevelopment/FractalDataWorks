using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Documents.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Connections.NarrowedDocuments;

/// <summary>Makes the paths of a <see cref="DocumentPage"/> relative to the folder.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(NarrowedResultHandlers), "DocumentPage")]
public sealed class DocumentPageNarrowedResultHandler : NarrowedResultHandlerBase<DocumentPage>
{
    /// <summary>Initializes a new instance of the <see cref="DocumentPageNarrowedResultHandler"/> class.</summary>
    public DocumentPageNarrowedResultHandler()
        : base("DocumentPage")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<DocumentPage> Narrow(DocumentPage value, DocumentScope folder) =>
        NarrowedPaths.RelativePage(folder, value);
}
