using System;

namespace Fdw.Services.Pipelines.Hubs;

/// <summary>
/// One piece of what a run gives the person it is for (FDW-6140), pushed to that person's own hub group as it is written. <see cref="Sequence"/> is the piece's place in the stored stream: a client that holds a piece
/// with that sequence ignores a second one, and one that dropped asks the stream route for the pieces after the last sequence it holds.
/// </summary>
public sealed class PipelineRunPieceUpdate
{
    /// <summary>Gets or sets the first run of the chain the piece belongs to, the id the follow route is asked with.</summary>
    public Guid ClaimRootId { get; set; }

    /// <summary>Gets or sets the person the piece is addressed to; the hub sends it to that person's group and nobody else's.</summary>
    public Guid PersonId { get; set; }

    /// <summary>Gets or sets the piece's sequence in the stored stream.</summary>
    public long Sequence { get; set; }

    /// <summary>Gets or sets the run that wrote the piece.</summary>
    public Guid ExecutionId { get; set; }

    /// <summary>Gets or sets the kind: Text, StepStarted, StepEnded or StepSkipped.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the step the piece is about; null for none.</summary>
    public string? StepName { get; set; }

    /// <summary>Gets or sets the text, or how the step ended; null for a step that started or was skipped.</summary>
    public string? Text { get; set; }

    /// <summary>Gets or sets when the piece was written.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
