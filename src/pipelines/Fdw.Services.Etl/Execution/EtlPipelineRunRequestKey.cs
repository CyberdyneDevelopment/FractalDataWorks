using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// The two columns of etl.PipelineRunRequest that say which run a row is for and which host accepted it (FDW-4084). Read across every host's rows to count the rows that name
/// no host and to tell a queued run another host holds from a run with no request; the request text is never read through it.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class EtlPipelineRunRequestKey
{
    /// <summary>Gets or sets the run's id, the id of its execution record.</summary>
    public Guid ExecutionId { get; set; }

    /// <summary>Gets or sets the Id of the Host row of the host that accepted the run; null on a row written before the column existed.</summary>
    public Guid? HostId { get; set; }
}
