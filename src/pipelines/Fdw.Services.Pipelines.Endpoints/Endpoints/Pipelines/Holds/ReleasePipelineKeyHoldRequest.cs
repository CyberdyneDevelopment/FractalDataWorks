namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>What a caller states to release the hold on a key (FDW-6939): the key in the route. The tenant is the caller's.</summary>
public sealed class ReleasePipelineKeyHoldRequest
{
    /// <summary>Gets or sets the key, as the pipeline's template expands it.</summary>
    public string Key { get; set; } = string.Empty;
}
