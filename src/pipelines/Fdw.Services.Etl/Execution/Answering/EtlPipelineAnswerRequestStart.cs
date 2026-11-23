using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Etl.Execution.Answering;

/// <summary>What a request's row is given when its continuation started (FDW-6142): the state and the run, and nothing else of the row.</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class EtlPipelineAnswerRequestStart
{
    /// <summary>Gets or sets the name of the state the request moves to.</summary>
    public string StateName { get; set; } = string.Empty;

    /// <summary>Gets or sets the run the answer started.</summary>
    public Guid ContinuationExecutionId { get; set; }
}
