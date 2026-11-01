using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Etl.Execution;

/// <summary>What a run's row is given when the run is accepted a second time, as a run approved after it was held for approval is: only the request text.</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class EtlPipelineRunRequestText
{
    /// <summary>Gets or sets the request as text.</summary>
    public string Request { get; set; } = string.Empty;
}
