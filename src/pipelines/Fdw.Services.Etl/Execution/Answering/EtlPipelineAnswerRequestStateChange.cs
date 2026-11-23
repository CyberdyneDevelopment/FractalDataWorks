using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Etl.Execution.Answering;

/// <summary>What a request's row is given when only its state moves (FDW-6142): the state, and nothing else of the row.</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class EtlPipelineAnswerRequestStateChange
{
    /// <summary>Gets or sets the name of the state the request moves to.</summary>
    public string StateName { get; set; } = string.Empty;
}
