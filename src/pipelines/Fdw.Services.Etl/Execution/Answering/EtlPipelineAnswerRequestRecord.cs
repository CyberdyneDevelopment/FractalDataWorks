using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Etl.Execution.Answering;

/// <summary>
/// POCO for the <c>etl.PipelineAnswerRequest</c> table (FDW-6142): an answer request a run kept for the person it acts as. Property names MUST match the SQL column names exactly. OpsDb has no row-level security,
/// so every read and write filters by <see cref="TenantId"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class EtlPipelineAnswerRequestRecord
{
    /// <summary>Gets or sets the unique identifier of the request.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the tenant of the run.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the person who may answer.</summary>
    public Guid PersonId { get; set; }

    /// <summary>Gets or sets the run that asked.</summary>
    public Guid ExecutionId { get; set; }

    /// <summary>Gets or sets the first run of the chain of restarts the asking run belongs to.</summary>
    public Guid ClaimRootId { get; set; }

    /// <summary>Gets or sets the name of the component that asked.</summary>
    public string ComponentName { get; set; } = string.Empty;

    /// <summary>Gets or sets how many times the component asked in the asking run before this one.</summary>
    public int Iteration { get; set; }

    /// <summary>Gets or sets the name of the request's kind.</summary>
    public string KindName { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the answer policy the call was judged under.</summary>
    public string PolicyName { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the call.</summary>
    public string CallName { get; set; } = string.Empty;

    /// <summary>Gets or sets the pipeline the answer starts.</summary>
    public Guid ContinuationPipelineId { get; set; }

    /// <summary>Gets or sets the name of the continuation's parameter that receives the answer.</summary>
    public string AnswerParameterName { get; set; } = string.Empty;

    /// <summary>Gets or sets the stored grant the call was judged against; null when none was stated.</summary>
    public Guid? GrantId { get; set; }

    /// <summary>Gets or sets the name of the request's state.</summary>
    public string StateName { get; set; } = string.Empty;

    /// <summary>Gets or sets when the request was asked.</summary>
    public DateTimeOffset AskedAt { get; set; }

    /// <summary>Gets or sets when the request ends.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Gets or sets when the person was told; null when they were not.</summary>
    public DateTimeOffset? AnnouncedAt { get; set; }

    /// <summary>Gets or sets when the request was answered; null when it was not.</summary>
    public DateTimeOffset? AnsweredAt { get; set; }

    /// <summary>Gets or sets the answer; null until it is answered.</summary>
    public string? AnswerText { get; set; }

    /// <summary>Gets or sets the run the answer started; null until one started.</summary>
    public Guid? ContinuationExecutionId { get; set; }
}
