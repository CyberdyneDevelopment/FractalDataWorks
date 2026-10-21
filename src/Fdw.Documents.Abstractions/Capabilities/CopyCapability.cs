using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Abstractions.Capabilities;

/// <summary>Copies a document inside the storage, without the content passing through the caller.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentCapabilities), "Copy", RestrictToCurrentCompilation = true)]
public sealed class CopyCapability : DocumentCapabilityBase
{
    /// <summary>Initializes a new instance of the <see cref="CopyCapability"/> class.</summary>
    public CopyCapability()
        : base("Copy", "Copies a document inside the storage, without the content passing through the caller.")
    {
    }
}
