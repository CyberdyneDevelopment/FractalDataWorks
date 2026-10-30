using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Etl.Execution;

/// <summary>What a run's row is given when the scheduler holds the run: the time it is held until.</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class EtlPipelineRunRequestHold
{
    /// <summary>Gets or sets when the run is due.</summary>
    public DateTimeOffset HeldUntil { get; set; }
}
