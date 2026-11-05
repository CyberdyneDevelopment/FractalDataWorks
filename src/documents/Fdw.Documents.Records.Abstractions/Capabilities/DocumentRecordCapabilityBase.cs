using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Documents.Records.Abstractions.Capabilities;

/// <summary>The base of the capabilities a DocumentRecord connection declares (FDW-3673).</summary>
[ExcludeFromCodeCoverage]
public abstract class DocumentRecordCapabilityBase : TypeOptionBase<DocumentRecordCapabilityBase>, IDocumentRecordCapability
{
    /// <summary>Initializes the sentinel the collection answers for an unknown name.</summary>
    protected DocumentRecordCapabilityBase()
        : base(0, "NotFound")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DocumentRecordCapabilityBase"/> class.</summary>
    /// <param name="name">The capability's name.</param>
    /// <param name="description">What a connection that declares it can do.</param>
    protected DocumentRecordCapabilityBase(string name, string description)
        : base(name, $"DocumentRecordCapabilities:{name}", name, description, "DocumentRecordCapability")
    {
    }
}
