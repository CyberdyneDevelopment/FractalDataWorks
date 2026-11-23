using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// POCO for the <c>etl.PipelineRunStream</c> table (FDW-6140): one piece a run gave its person. Property names MUST match the SQL column names exactly. OpsDb has no row-level security, so every read and write
/// filters by <see cref="TenantId"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class EtlPipelineRunStreamRecord
{
    /// <summary>Gets or sets the piece's sequence: the table gives it on insert.</summary>
    public long RowId { get; set; }

    /// <summary>Gets or sets the piece's own id, made by the writer so the sequence the table gave it can be read back for the push.</summary>
    public Guid PieceId { get; set; }

    /// <summary>Gets or sets the tenant the run ran in.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the first run of the chain of restarts.</summary>
    public Guid ClaimRootId { get; set; }

    /// <summary>Gets or sets the run that wrote the piece.</summary>
    public Guid ExecutionId { get; set; }

    /// <summary>Gets or sets the person the piece is addressed to.</summary>
    public Guid PersonId { get; set; }

    /// <summary>Gets or sets the kind of the piece.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the step the piece is about; null for none.</summary>
    public string? StepName { get; set; }

    /// <summary>Gets or sets the text; null for a step that started or was skipped.</summary>
    public string? Text { get; set; }

    /// <summary>Gets or sets when the piece was written.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
