using System;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// A pipeline that writes a data set (FDW-4488).
/// </summary>
/// <param name="PipelineId">The pipeline's logical id.</param>
/// <param name="PipelineName">The pipeline's name, which its runs are tracked under.</param>
/// <param name="IsEnabled">Whether the pipeline is enabled.</param>
public sealed record DataSetWriter(Guid PipelineId, string PipelineName, bool IsEnabled);
