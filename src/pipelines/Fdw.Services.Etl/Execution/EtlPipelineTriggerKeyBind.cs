using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Etl.Execution;

/// <summary>What a key's row is given when its run is made: the run's id.</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class EtlPipelineTriggerKeyBind
{
    /// <summary>Gets or sets the run the fire started.</summary>
    public Guid ExecutionId { get; set; }
}
