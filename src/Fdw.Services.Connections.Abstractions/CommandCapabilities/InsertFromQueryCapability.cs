using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.CommandCapabilities;

/// <summary>The connection can run a set-based INSERT ... SELECT on one server, including across databases on that server.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CommandCapabilityTypes), "InsertFromQuery", RestrictToCurrentCompilation = true)]
public sealed class InsertFromQueryCapability : CommandCapabilityTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InsertFromQueryCapability"/> class.
    /// </summary>
    public InsertFromQueryCapability()
        : base(
            name: "InsertFromQuery",
            displayName: "Insert from query (INSERT ... SELECT)",
            configurationFields: [])
    {
    }
}
