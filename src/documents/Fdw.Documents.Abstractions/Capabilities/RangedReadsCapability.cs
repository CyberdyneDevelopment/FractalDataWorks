using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Abstractions.Capabilities;

/// <summary>Reads a byte range of a document.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentCapabilities), "RangedReads", RestrictToCurrentCompilation = true)]
public sealed class RangedReadsCapability : DocumentCapabilityBase
{
    /// <summary>Initializes a new instance of the <see cref="RangedReadsCapability"/> class.</summary>
    public RangedReadsCapability()
        : base("RangedReads", "Reads a byte range of a document.")
    {
    }
}
