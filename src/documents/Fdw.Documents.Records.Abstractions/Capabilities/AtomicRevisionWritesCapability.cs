using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Records.Abstractions.Capabilities;

/// <summary>
/// Makes each write and the raise of the organization's revision one atomic step, and refuses a write whose expected revision is not the
/// current one (FDW-3673). The record store needs it: without it two writers could both succeed on the same revision.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentRecordCapabilities), "AtomicRevisionWrites", RestrictToCurrentCompilation = true)]
public sealed class AtomicRevisionWritesCapability : DocumentRecordCapabilityBase
{
    /// <summary>Initializes a new instance of the <see cref="AtomicRevisionWritesCapability"/> class.</summary>
    public AtomicRevisionWritesCapability()
        : base("AtomicRevisionWrites", "Makes each write and the raise of the organization's revision one atomic step.")
    {
    }
}
