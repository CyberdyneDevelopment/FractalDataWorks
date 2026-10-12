using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections.Http.Abstractions;

namespace Fdw.Services.Connections.Http;

/// <summary>
/// SOAP-specific settings for HTTP connections.
/// </summary>
/// <remarks>
/// Maps to a child table: conn.HttpConnectionSoap
/// </remarks>
[ExcludeFromCodeCoverage]
public class HttpSoapSettings : IHttpSoapSettings
{
    /// <summary>
    /// Gets or sets the default SOAP namespace for requests.
    /// </summary>
    public string? DefaultNamespace { get; set; }

    /// <summary>
    /// Gets or sets the SOAPAction header pattern.
    /// </summary>
    /// <remarks>
    /// Supports placeholders:
    /// <list type="bullet">
    /// <item><description>{operation} - Replaced with command type</description></item>
    /// <item><description>{container} - Replaced with container name</description></item>
    /// </list>
    /// </remarks>
    public string? SoapActionPattern { get; set; }

    /// <summary>
    /// Gets or sets the source identifier for service-specific headers.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the user ID for service-specific headers.
    /// </summary>
    public string? UserId { get; set; }
}