namespace Fdw.Web.RestEndpoints.Configuration;

/// <summary>
/// Default configuration values for endpoints.
/// </summary>
public static class EndpointDefaults
{
    /// <summary>
    /// How long a query endpoint's response is cached, in seconds. A deployment sets this before
    /// the host maps its endpoints.
    /// </summary>
    public static int DefaultCacheDurationSeconds { get; set; } = 300;
}
