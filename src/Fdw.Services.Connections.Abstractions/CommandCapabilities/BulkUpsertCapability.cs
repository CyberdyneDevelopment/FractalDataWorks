using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.CommandCapabilities;

/// <summary>
/// Bulk upsert capability — writes a large batch of records to a target container using
/// the connection's native merge/upsert mechanism, matching on declared key fields.
/// ConfigurationFields is empty: the client edits this with its own editor.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CommandCapabilityTypes), "BulkUpsert", RestrictToCurrentCompilation = true)]
public sealed class BulkUpsertCapability : CommandCapabilityTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BulkUpsertCapability"/> class.
    /// </summary>
    public BulkUpsertCapability()
        : base(
            name: "BulkUpsert",
            displayName: "Bulk Upsert",
            configurationFields: [])
    {
    }
}
