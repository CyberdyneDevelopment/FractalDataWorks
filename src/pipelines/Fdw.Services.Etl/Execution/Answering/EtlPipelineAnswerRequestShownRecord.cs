using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Etl.Execution.Answering;

/// <summary>POCO for the <c>etl.PipelineAnswerRequestShown</c> table (FDW-6142): one thing the person is shown with a request, in order.</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class EtlPipelineAnswerRequestShownRecord
{
    /// <summary>Gets or sets the unique identifier of the row.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the request the row belongs to.</summary>
    public Guid RequestId { get; set; }

    /// <summary>Gets or sets the place of the row among the request's.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets what the row is (a title, a body, an option).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the text shown.</summary>
    public string ValueText { get; set; } = string.Empty;
}
