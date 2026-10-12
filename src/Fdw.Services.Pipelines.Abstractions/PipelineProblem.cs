using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// One problem found in a pipeline draft by a dry-run check (<c>POST /pipelines/validate</c>).
/// </summary>
public sealed class PipelineProblem
{
    /// <summary>
    /// Gets or sets the name of the stage (transform) the problem belongs to, or <see langword="null"/> when it
    /// belongs to the pipeline as a whole or to a stage that has no name yet.
    /// </summary>
    public string? Stage { get; set; }

    /// <summary>
    /// Gets or sets the stage's place in the pipeline as names from the root stage down to the stage itself, the way a
    /// machine addresses it (<see cref="Stage"/> is its display form). One name for a stage at the top of the pipeline;
    /// <see langword="null"/> exactly when <see cref="Stage"/> is.
    /// </summary>
    public IList<string>? StagePath { get; set; }

    /// <summary>
    /// Gets or sets the request path of the field the problem belongs to (relative to its stage when
    /// <see cref="Stage"/> is set, otherwise relative to the request), or <see langword="null"/> when no single
    /// field is at fault.
    /// </summary>
    public string? Field { get; set; }

    /// <summary>Gets or sets what is wrong, in words the person who wrote the draft can act on.</summary>
    public string Reason { get; set; } = string.Empty;
}
