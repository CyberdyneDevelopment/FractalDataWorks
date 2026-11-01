using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Abstractions.Capabilities;

/// <summary>Reports a hash of a document's content that the storage computed.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentCapabilities), "ContentHash", RestrictToCurrentCompilation = true)]
public sealed class ContentHashCapability : DocumentCapabilityBase
{
    /// <summary>Initializes a new instance of the <see cref="ContentHashCapability"/> class.</summary>
    public ContentHashCapability()
        : base("ContentHash", "Reports a hash of a document's content that the storage computed.")
    {
    }
}
