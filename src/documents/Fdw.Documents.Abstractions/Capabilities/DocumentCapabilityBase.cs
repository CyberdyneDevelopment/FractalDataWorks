using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Documents.Abstractions.Capabilities;

/// <summary>The base of the capabilities a document connection declares.</summary>
[ExcludeFromCodeCoverage]
public abstract class DocumentCapabilityBase : TypeOptionBase<DocumentCapabilityBase>, IDocumentCapability
{
    /// <summary>Initializes the sentinel the collection answers for an unknown name.</summary>
    protected DocumentCapabilityBase()
        : base(0, "NotFound")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DocumentCapabilityBase"/> class.</summary>
    /// <param name="name">The capability's name.</param>
    /// <param name="description">What a connection that declares it can do.</param>
    protected DocumentCapabilityBase(string name, string description)
        : base(name, $"DocumentCapabilities:{name}", name, description, "DocumentCapability")
    {
    }
}
