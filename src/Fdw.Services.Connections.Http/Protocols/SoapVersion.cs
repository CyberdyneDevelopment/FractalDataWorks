using System.Xml.Linq;

namespace Fdw.Services.Connections.Http.Protocols;

/// <summary>
/// Represents a SOAP version with its associated namespaces and settings.
/// </summary>
/// <param name="Name">The version name (e.g., "Soap11", "Soap12").</param>
/// <param name="EnvelopeNamespace">The SOAP envelope namespace.</param>
/// <param name="ContentType">The content type for this SOAP version.</param>
/// <param name="MustUnderstandValue">The value for mustUnderstand attribute ("1" for 1.1, "true" for 1.2).</param>
public readonly record struct SoapVersion(
    string Name,
    XNamespace EnvelopeNamespace,
    string ContentType,
    string MustUnderstandValue)
{
    /// <summary>
    /// SOAP 1.1 version.
    /// </summary>
    public static readonly SoapVersion Soap11 = new(
        "Soap11",
        XNamespace.Get("http://schemas.xmlsoap.org/soap/envelope/"),
        "text/xml; charset=utf-8",
        "1");

    /// <summary>
    /// SOAP 1.2 version.
    /// </summary>
    public static readonly SoapVersion Soap12 = new(
        "Soap12",
        XNamespace.Get("http://www.w3.org/2003/05/soap-envelope"),
        "application/soap+xml; charset=utf-8",
        "true");
}
