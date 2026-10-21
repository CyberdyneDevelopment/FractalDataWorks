using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Abstractions.Capabilities;

/// <summary>Refuses an upload, copy or move onto a path that already holds a document, without a check-then-write gap.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentCapabilities), "CreateOnlyWrites", RestrictToCurrentCompilation = true)]
public sealed class CreateOnlyWritesCapability : DocumentCapabilityBase
{
    /// <summary>Initializes a new instance of the <see cref="CreateOnlyWritesCapability"/> class.</summary>
    public CreateOnlyWritesCapability()
        : base("CreateOnlyWrites", "Refuses an upload, copy or move onto a path that already holds a document, without a check-then-write gap.")
    {
    }
}
