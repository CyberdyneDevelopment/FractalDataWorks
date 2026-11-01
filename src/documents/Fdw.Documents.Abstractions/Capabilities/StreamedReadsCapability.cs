using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Abstractions.Capabilities;

/// <summary>Reads a document as a stream, without holding it whole.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentCapabilities), "StreamedReads", RestrictToCurrentCompilation = true)]
public sealed class StreamedReadsCapability : DocumentCapabilityBase
{
    /// <summary>Initializes a new instance of the <see cref="StreamedReadsCapability"/> class.</summary>
    public StreamedReadsCapability()
        : base("StreamedReads", "Reads a document as a stream, without holding it whole.")
    {
    }
}
