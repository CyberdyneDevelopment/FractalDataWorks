using System.Collections.Generic;
using Fdw.Services.Etl.Abstractions.Execution;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>The pieces of a run after the sequence a person asked from (FDW-6140).</summary>
public sealed class FollowPipelineRunResponse
{
    /// <summary>Gets or sets the pieces, in order.</summary>
    public IReadOnlyList<RunStreamPiece> Pieces { get; set; } = [];

    /// <summary>Gets or sets the sequence to ask again from: the last piece's, or the one asked from when there were none.</summary>
    public long Last { get; set; }
}
