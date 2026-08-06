using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.CommandCapabilities;

/// <summary>
/// Bulk upsert capability — writes a large batch of records to a target container using
/// the connection's native merge/upsert mechanism, matching on declared key fields.
/// The pipeline builder renders the <c>BulkUpsertCommandSkin</c> composite component.
/// </summary>
/// <remarks>
/// Why BuilderComponentType null: resolved at runtime by Builder.razor via
/// the capability's name; avoids a hard dependency from this netstandard2.0 package
/// on a Blazor UI package targeting net10.0.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CommandCapabilityTypes), "BulkUpsert", RestrictToCurrentCompilation = true)]
public sealed class BulkUpsertCapability : CommandCapabilityTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BulkUpsertCapability"/> class.
    /// </summary>
    public BulkUpsertCapability()
        : base(
            id: 11,
            name: "BulkUpsert",
            displayName: "Bulk Upsert",
            configurationFields: [],
            builderComponentType: null)
    {
    }
}
