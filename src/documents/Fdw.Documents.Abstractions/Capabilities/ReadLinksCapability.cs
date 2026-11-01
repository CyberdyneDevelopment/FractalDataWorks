using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Abstractions.Capabilities;

/// <summary>Creates a temporary link a caller can read a document from without the connection's credentials.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentCapabilities), "ReadLinks", RestrictToCurrentCompilation = true)]
public sealed class ReadLinksCapability : DocumentCapabilityBase
{
    /// <summary>Initializes a new instance of the <see cref="ReadLinksCapability"/> class.</summary>
    public ReadLinksCapability()
        : base("ReadLinks", "Creates a temporary link a caller can read a document from without the connection's credentials.")
    {
    }
}
