using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Abstractions.Capabilities;

/// <summary>Keeps earlier versions of a document the storage itself versions.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentCapabilities), "Versioning", RestrictToCurrentCompilation = true)]
public sealed class VersioningCapability : DocumentCapabilityBase
{
    /// <summary>Initializes a new instance of the <see cref="VersioningCapability"/> class.</summary>
    public VersioningCapability()
        : base("Versioning", "Keeps earlier versions of a document the storage itself versions.")
    {
    }
}
