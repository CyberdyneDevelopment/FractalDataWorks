using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Etl.Execution.Answering;

/// <summary>POCO for the <c>etl.PipelineAnswerRequestRole</c> table (FDW-6142): one role the pipeline the answer starts acts with.</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class EtlPipelineAnswerRequestRoleRecord
{
    /// <summary>Gets or sets the unique identifier of the row.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the request the row belongs to.</summary>
    public Guid RequestId { get; set; }

    /// <summary>Gets or sets the role's name.</summary>
    public string RoleName { get; set; } = string.Empty;
}
