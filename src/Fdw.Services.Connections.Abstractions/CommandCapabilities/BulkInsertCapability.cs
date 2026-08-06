using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.CommandCapabilities;

/// <summary>
/// Bulk insert capability — writes a large batch of records to a target container
/// using the connection's native bulk-write mechanism (e.g., SqlBulkCopy for MsSql).
/// </summary>
/// <remarks>
/// Configuration keys:
/// <list type="bullet">
///   <item><c>TargetContainer</c> — destination table or container name (required).</item>
///   <item><c>BatchSize</c> — number of rows per bulk batch; defaults to 5000 at runtime if not set.</item>
/// </list>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CommandCapabilityTypes), "BulkInsert", RestrictToCurrentCompilation = true)]
public sealed class BulkInsertCapability : CommandCapabilityTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BulkInsertCapability"/> class.
    /// </summary>
    public BulkInsertCapability()
        : base(
            id: 4,
            name: "BulkInsert",
            displayName: "Bulk Insert",
            configurationFields:
            [
                new ConfigurationFieldDescriptor(
                    Key: "TargetContainer",
                    Label: "Target Container",
                    Placeholder: "schema.TableName",
                    InputKind: ConfigurationFieldKinds.Text,
                    IsRequired: true),
                new ConfigurationFieldDescriptor(
                    Key: "BatchSize",
                    Label: "Batch Size",
                    Placeholder: "5000",
                    InputKind: ConfigurationFieldKinds.Numeric),
            ])
    {
    }
}
