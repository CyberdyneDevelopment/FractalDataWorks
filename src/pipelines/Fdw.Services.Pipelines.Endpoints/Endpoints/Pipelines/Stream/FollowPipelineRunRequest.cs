using System;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>What a person asks for when they follow a run (FDW-6140): the run, the last piece they have, and how many pieces at most.</summary>
public sealed class FollowPipelineRunRequest
{
    /// <summary>Gets or sets the run: the first run of the chain, whose id the person was given when it started.</summary>
    public Guid ExecutionId { get; set; }

    /// <summary>Gets or sets the sequence of the last piece the person received; 0 for none. The pieces after it are answered, once and in order.</summary>
    public long After { get; set; }

    /// <summary>Gets or sets the most pieces to answer; none states the host's page size (the setting <c>RunStreamPageSize</c>), and a number above it is taken as the page size.</summary>
    public int? Take { get; set; }
}
