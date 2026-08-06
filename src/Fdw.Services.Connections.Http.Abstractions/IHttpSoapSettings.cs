namespace Fdw.Services.Connections.Http.Abstractions;

/// <summary>
/// Interface for SOAP-specific settings.
/// </summary>
public interface IHttpSoapSettings
{
    /// <summary>
    /// Gets the default SOAP namespace for requests.
    /// </summary>
    string? DefaultNamespace { get; }

    /// <summary>
    /// Gets the SOAPAction header pattern.
    /// Supports placeholders: {operation}, {container}
    /// </summary>
    string? SoapActionPattern { get; }

    /// <summary>
    /// Gets the source identifier for service-specific headers.
    /// </summary>
    string? Source { get; }

    /// <summary>
    /// Gets the user ID for service-specific headers.
    /// </summary>
    string? UserId { get; }
}