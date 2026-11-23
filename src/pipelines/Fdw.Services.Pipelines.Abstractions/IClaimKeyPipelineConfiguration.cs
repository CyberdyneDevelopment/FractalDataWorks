namespace Fdw.Services.Pipelines.Abstractions;

/// <summary>
/// A pipeline implementation that states the key its runs claim, beside the tables they write (FDW-6141): a reader asks for the fact without asking what the engine is,
/// as the layer does for the facts it owns.
/// </summary>
/// <remarks>
/// Two runs that claim the same key take turns, whatever the pipeline type. The fact is the layer's; a row that does not state it claims its tables only.
/// </remarks>
public interface IClaimKeyPipelineConfiguration
{
    /// <summary>Gets the template, over the pipeline's declared parameters, of the key a run claims; <see langword="null"/> when the pipeline claims no key of its own.</summary>
    string? ClaimKeyTemplate { get; }
}
