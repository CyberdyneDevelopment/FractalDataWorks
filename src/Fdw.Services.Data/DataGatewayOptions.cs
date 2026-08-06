namespace Fdw.Services.Data;

/// <summary>
/// Configuration options for the DataGateway cache behaviour.
/// Bound from the <c>"DataGateway"</c> configuration section via
/// <see cref="Microsoft.Extensions.Options.IOptions{T}"/>.
/// </summary>
/// <remarks>
/// <c>EnableCache = true</c> is the documented default knob (not a ?? fallback):
/// api leaves it true; etl/scheduler set it false in their appsettings so they
/// always get a cacheless path with no cross-process staleness risk.
/// </remarks>
public sealed class DataGatewayOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether gateway-level result caching is active for this process.
    /// When <see langword="false"/> the gateway neither reads from nor writes to the result cache — every
    /// <c>Execute&lt;T&gt;</c> call reaches the data source directly.
    /// </summary>
    public bool EnableCache { get; set; } = true;
}
