namespace Fdw.Web.Analytics.Clients.Models;

/// <summary>
/// Represents a request to create a new promotion between environments.
/// </summary>
public sealed class CreatePromotionPayload
{
    /// <summary>
    /// Gets or sets the name of the promotion.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source environment name.
    /// </summary>
    public string SourceEnvironment { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target environment name.
    /// </summary>
    public string TargetEnvironment { get; set; } = string.Empty;
}
