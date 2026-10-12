using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// The load definition a pipeline body carries (contract v1 to v4). Create, update and validate bodies, on the
/// API and the ETL server alike, implement it so one mapper and one validator serve them all.
/// </summary>
public interface IPipelineLoadFields
{
    /// <summary>Gets the load mode name (Append, Replace or Upsert) of the implicit load, or <see langword="null"/> when none is stated.</summary>
    string? LoadMode { get; }

    /// <summary>Gets the options of the implicit load, or <see langword="null"/> when none are stated.</summary>
    PipelineLoadOptions? LoadOptions { get; }

    /// <summary>Gets the ordered load steps, or <see langword="null"/> when none are stated.</summary>
    IList<PipelineLoadStep>? LoadSteps { get; }

    /// <summary>Gets the reject steps, which write the records the pipeline's stages rejected, or <see langword="null"/> when none are stated.</summary>
    IList<PipelineLoadStep>? RejectSteps { get; }

    /// <summary>Gets the value sources of the implicit load, or <see langword="null"/> when none are stated.</summary>
    IList<PipelineLoadColumnSource>? ValueSources { get; }

    /// <summary>Gets the declared run parameters, or <see langword="null"/> when none are stated.</summary>
    IList<PipelineParameterDeclaration>? Parameters { get; }
}
