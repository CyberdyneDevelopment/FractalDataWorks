namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>What a caller states to place a hold on a key (FDW-6939): the key in the route and why it is held in the body. The person and the tenant are the caller's, never stated here.</summary>
public sealed class PlacePipelineKeyHoldRequest
{
    /// <summary>Gets or sets the key, as the pipeline's template expands it (for example a conversation's id).</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Gets or sets why the key is held; it is stated, never assumed.</summary>
    public string Reason { get; set; } = string.Empty;
}
