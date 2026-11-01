using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Abstractions.Capabilities;

/// <summary>Keeps named metadata with a document.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentCapabilities), "Metadata", RestrictToCurrentCompilation = true)]
public sealed class MetadataCapability : DocumentCapabilityBase
{
    /// <summary>Initializes a new instance of the <see cref="MetadataCapability"/> class.</summary>
    public MetadataCapability()
        : base("Metadata", "Keeps named metadata with a document.")
    {
    }
}
