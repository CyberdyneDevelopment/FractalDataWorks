using System.Net.Http;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Http.Abstractions;

/// <summary>
/// Marker interface for an HTTP connection.
/// Exposes the base URL and the typed HTTP client primitive.
/// </summary>
/// <remarks>
/// Connectors call <see cref="HttpClient"/> directly per the §1.1 canary experiment.
/// The connection owns client lifetime; connectors own orchestration and per-call addressing.
/// </remarks>
public interface IHttpConnection : IGenericConnection
{
    /// <summary>
    /// Gets the base URL configured for this connection.
    /// </summary>
    string BaseUrl { get; }

    /// <summary>
    /// Gets the typed HTTP client primitive for making requests.
    /// Connectors use this surface directly — no DataGateway dispatch in the 1.1.1 canary.
    /// </summary>
    HttpClient HttpClient { get; }
}
