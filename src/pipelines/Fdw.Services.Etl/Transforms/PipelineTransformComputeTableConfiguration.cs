using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Etl.Transforms;

/// <summary>One table input of the Built transformation a Compute stage applies and where its rows come from (FDW-1231).</summary>
/// <remarks>A cascade child of the stage, in <c>pipe.PipelineTransformComputeTable</c>.</remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline")]
public sealed partial class PipelineTransformComputeTableConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the row's id.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the table input of the transformation this row binds.</summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>Gets or sets where the rows come from: Stage (the rows arriving at the stage), DataSet (the named data set) or Output (a named output an earlier stage wrote).</summary>
    public string FromKind { get; set; } = string.Empty;

    /// <summary>Gets or sets the data set or output name for FromKind DataSet or Output; null for Stage.</summary>
    public string? FromName { get; set; }
}
