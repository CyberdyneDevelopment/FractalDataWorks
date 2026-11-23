using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Documents.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Connections.NarrowedDocuments;

/// <summary>Makes the path of a <see cref="DocumentInfo"/> relative to the folder.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(NarrowedResultHandlers), "DocumentInfo")]
public sealed class DocumentInfoNarrowedResultHandler : NarrowedResultHandlerBase<DocumentInfo>
{
    /// <summary>Initializes a new instance of the <see cref="DocumentInfoNarrowedResultHandler"/> class.</summary>
    public DocumentInfoNarrowedResultHandler()
        : base("DocumentInfo")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<DocumentInfo> Narrow(DocumentInfo value, DocumentScope folder) =>
        NarrowedPaths.RelativeInfo(folder, value);
}
