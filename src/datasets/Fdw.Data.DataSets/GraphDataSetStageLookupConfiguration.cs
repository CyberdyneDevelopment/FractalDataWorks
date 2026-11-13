using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Configuration for a single brought-across column within a Lookup transform. Cascade child of
/// <see cref="GraphDataSetStageConfiguration"/> (data.GraphDataSetStageLookup). One row per
/// output column — the lookup connection/keys are shared across the rows for the same transform.
/// </summary>
/// <remarks>
/// Why: implements <see cref="IGenericConfiguration"/>
/// so the configuration cascade-save discovers and persists this collection when the parent transform is
/// saved. Without this interface the Lookups collection would be silently skipped.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class GraphDataSetStageLookupConfiguration : IGenericConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the name for display/binding.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the lookup data set name.
    /// </summary>
    public string LookupDataSet { get; set; } = string.Empty;

    /// <summary>Gets or sets the data store the lookup reads, when it reads a container rather than a data set (FDW-1269). All three parts or none.</summary>
    /// <remarks>Maps to <c>data.GraphDataSetStageLookup.LookupDataStoreName</c>.</remarks>
    public string? LookupDataStoreName { get; set; }

    /// <summary>Gets or sets the path within the lookup data store.</summary>
    /// <remarks>Maps to <c>data.GraphDataSetStageLookup.LookupDataStorePath</c>.</remarks>
    public string? LookupDataStorePath { get; set; }

    /// <summary>Gets or sets the container within that path whose rows the lookup reads.</summary>
    /// <remarks>Maps to <c>data.GraphDataSetStageLookup.LookupContainerName</c>.</remarks>
    public string? LookupContainerName { get; set; }

    /// <summary>
    /// Gets or sets the lookup key field.
    /// </summary>
    public string LookupKeyField { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source key field to match against.
    /// </summary>
    public string SourceKeyField { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional output field prefix.
    /// </summary>
    public string? OutputFieldPrefix { get; set; }

    /// <summary>
    /// Gets or sets the field in the lookup source to bring across as this row's output column.
    /// </summary>
    public string LookupValueField { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the join type, resolved against <c>LookupJoinTypes</c>.
    /// </summary>
    public string JoinType { get; set; } = string.Empty;
}
