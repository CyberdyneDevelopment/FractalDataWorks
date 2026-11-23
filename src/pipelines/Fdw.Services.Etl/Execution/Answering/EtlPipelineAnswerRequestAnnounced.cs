using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Etl.Execution.Answering;

/// <summary>What a request's row is given when the person was told about it (FDW-6142): when, and nothing else of the row.</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class EtlPipelineAnswerRequestAnnounced
{
    /// <summary>Gets or sets when the person was told.</summary>
    public DateTimeOffset AnnouncedAt { get; set; }
}
