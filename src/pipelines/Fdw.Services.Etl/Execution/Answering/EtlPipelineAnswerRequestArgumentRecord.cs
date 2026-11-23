using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Etl.Execution.Answering;

/// <summary>POCO for the <c>etl.PipelineAnswerRequestArgument</c> table (FDW-6142): one argument of the pipeline the answer starts, as name and text.</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class EtlPipelineAnswerRequestArgumentRecord
{
    /// <summary>Gets or sets the unique identifier of the row.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the request the row belongs to.</summary>
    public Guid RequestId { get; set; }

    /// <summary>Gets or sets the parameter's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the parameter's value as text.</summary>
    public string ValueText { get; set; } = string.Empty;
}
