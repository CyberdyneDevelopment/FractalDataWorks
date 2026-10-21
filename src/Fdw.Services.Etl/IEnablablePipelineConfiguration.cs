namespace Fdw.Services.Etl;

/// <summary>
/// A pipeline implementation that can be switched off (FDW-1396): the one fact about a pipeline's state that every engine carries,
/// the linear ones and the graph alike, so a reader asks for it without asking what the engine is.
/// </summary>
public interface IEnablablePipelineConfiguration
{
    /// <summary>Gets a value indicating whether the pipeline is enabled.</summary>
    bool IsEnabled { get; }
}
