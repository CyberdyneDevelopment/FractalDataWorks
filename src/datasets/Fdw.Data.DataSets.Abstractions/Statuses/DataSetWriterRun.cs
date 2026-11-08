using Fdw.Operations.Abstractions.Execution;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>A pipeline that writes a data set, and its latest run (FDW-4488).</summary>
/// <param name="PipelineName">The pipeline's name, which its runs are tracked under.</param>
/// <param name="IsEnabled">Whether the pipeline is enabled.</param>
/// <param name="LatestRun">The pipeline's latest run, or <see langword="null"/> when it has none the caller may see.</param>
public sealed record DataSetWriterRun(string PipelineName, bool IsEnabled, IExecutionItem? LatestRun);
