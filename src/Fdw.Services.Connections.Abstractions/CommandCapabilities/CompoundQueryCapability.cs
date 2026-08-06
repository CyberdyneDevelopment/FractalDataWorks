using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.CommandCapabilities;

/// <summary>
/// Compound (pushed-down JOIN) query capability — executes a multi-table SELECT with
/// explicit JOIN clauses against a single store. The join is performed by the backend
/// (not in-memory), so all sources must live in one connection.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CommandCapabilityTypes), "CompoundQuery", RestrictToCurrentCompilation = true)]
public sealed class CompoundQueryCapability : CommandCapabilityTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundQueryCapability"/> class.
    /// </summary>
    public CompoundQueryCapability()
        : base(
            id: 12,
            name: "CompoundQuery",
            displayName: "Compound (Pushed-Down JOIN) Query",
            configurationFields: [],
            builderComponentType: null)
    {
    }
}
