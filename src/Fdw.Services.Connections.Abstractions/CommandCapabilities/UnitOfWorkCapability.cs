using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.CommandCapabilities;

/// <summary>The connection can run an ordered list of data commands in one transaction, rolled back on any failure.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CommandCapabilityTypes), "UnitOfWork", RestrictToCurrentCompilation = true)]
public sealed class UnitOfWorkCapability : CommandCapabilityTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWorkCapability"/> class.
    /// </summary>
    public UnitOfWorkCapability()
        : base(
            name: "UnitOfWork",
            displayName: "Unit of work (one transaction)",
            configurationFields: [])
    {
    }
}
