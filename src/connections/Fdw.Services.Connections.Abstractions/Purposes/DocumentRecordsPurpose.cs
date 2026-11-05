using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Documents.Records.Abstractions.Capabilities;
using Fdw.Services.Connections.Abstractions.DocumentRecords;

namespace Fdw.Services.Connections.Abstractions.Purposes;

/// <summary>
/// Document records (FDW-3673): a DocumentRecord connection that writes revisions atomically. A record connection has no folders, so a grant
/// or an assignment for it names none.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConnectionPurposes), "DocumentRecords", RestrictToCurrentCompilation = true)]
public sealed class DocumentRecordsPurpose : ConnectionPurposeBase
{
    /// <summary>Initializes a new instance of the <see cref="DocumentRecordsPurpose"/> class.</summary>
    public DocumentRecordsPurpose()
        : base("DocumentRecords", "Document records, kept on a DocumentRecord connection.")
    {
    }

    /// <inheritdoc />
    public override string? Unfit(IGenericConnection connection)
    {
        if (DocumentRecordConnections.Of(connection) is not { } records)
        {
            return "it is not a DocumentRecord connection, and no DocumentRecord extension on this host takes its kind";
        }

        return records.RecordCapabilities.Any(capability => ReferenceEquals(capability, DocumentRecordCapabilities.ByName("AtomicRevisionWrites")))
            ? null
            : "it does not declare AtomicRevisionWrites, which the record store needs";
    }
}
