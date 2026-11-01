using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.CommandCapabilities;

/// <summary>The connection can flag rows deleted (a deleted-flag column, optionally a deleted-at column) instead of removing them.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CommandCapabilityTypes), "SoftDelete", RestrictToCurrentCompilation = true)]
public sealed class SoftDeleteCapability : CommandCapabilityTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SoftDeleteCapability"/> class.
    /// </summary>
    public SoftDeleteCapability()
        : base(
            name: "SoftDelete",
            displayName: "Soft delete (flag instead of remove)",
            configurationFields: [])
    {
    }
}
