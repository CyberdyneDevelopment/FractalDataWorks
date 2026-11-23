using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// POCO for the <c>etl.PipelineRunOutput</c> table (FDW-6136): one output a run gave back. Property names MUST match the SQL column names exactly. OpsDb has no row-level security, so every read and write
/// filters by <see cref="TenantId"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class EtlPipelineRunOutputRecord
{
    /// <summary>Gets or sets the unique identifier of the row.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the run that gave the output back.</summary>
    public Guid ExecutionId { get; set; }

    /// <summary>Gets or sets the tenant the run ran in.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the output's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the <c>DataTypes</c> option name of the value.</summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>Gets or sets the value as invariant text; null when the variable held no value.</summary>
    public string? ValueText { get; set; }
}
