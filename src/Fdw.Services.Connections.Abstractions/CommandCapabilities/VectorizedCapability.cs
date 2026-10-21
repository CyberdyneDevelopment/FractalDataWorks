using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.CommandCapabilities;

/// <summary>
/// The connection's store holds vectors, and the connection answers the vectorized commands: the rows nearest a vector
/// (FDW-1354). A connection type lists it only when its connection implements <see cref="IVectorizedDataConnection"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CommandCapabilityTypes), "Vectorized", RestrictToCurrentCompilation = true)]
public sealed class VectorizedCapability : CommandCapabilityTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VectorizedCapability"/> class.
    /// </summary>
    public VectorizedCapability()
        : base(
            name: "Vectorized",
            displayName: "Vectors (nearest rows by distance)",
            configurationFields: [])
    {
    }
}
