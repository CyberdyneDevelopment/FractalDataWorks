using System;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Operations.Endpoints.Tests.Lineage;

/// <summary>
/// A configured pipeline whose implementation is not an ETL one — the case the lineage projection
/// renders node-only, because there is no engine linkage to read.
/// </summary>
internal sealed class NonEtlPipelineConfiguration : IPipelineImplementationConfiguration
{
    /// <inheritdoc/>
    public Guid Id { get; set; }

    /// <inheritdoc/>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string Implementation { get; set; } = string.Empty;

    /// <inheritdoc/>
    public Guid? OrgId { get; set; }
}
