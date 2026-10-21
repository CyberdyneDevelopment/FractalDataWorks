using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Abstractions.Capabilities;

/// <summary>Moves a document inside the storage, without the content passing through the caller.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentCapabilities), "Move", RestrictToCurrentCompilation = true)]
public sealed class MoveCapability : DocumentCapabilityBase
{
    /// <summary>Initializes a new instance of the <see cref="MoveCapability"/> class.</summary>
    public MoveCapability()
        : base("Move", "Moves a document inside the storage, without the content passing through the caller.")
    {
    }
}
