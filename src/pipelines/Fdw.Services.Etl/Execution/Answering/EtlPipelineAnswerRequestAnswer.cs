using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Etl.Execution.Answering;

/// <summary>What a request's row is given when it is answered (FDW-6142): the state, when and the answer, and nothing else of the row.</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class EtlPipelineAnswerRequestAnswer
{
    /// <summary>Gets or sets the name of the state the request moves to.</summary>
    public string StateName { get; set; } = string.Empty;

    /// <summary>Gets or sets when the request was answered.</summary>
    public DateTimeOffset AnsweredAt { get; set; }

    /// <summary>Gets or sets the answer.</summary>
    public string AnswerText { get; set; } = string.Empty;
}
