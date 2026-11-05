using Fdw.Conventions;

namespace Fdw.Services.Connections.Http.Protocols;

/// <summary>
/// The texts the Http protocols' failure details show where the server sent no text of its own: no reason phrase, no
/// GraphQL error message, no SOAP fault code or fault string. None of them is ever a value the server sent.
/// </summary>
/// <remarks>
/// Why declared here: FDW055 accepts a placeholder only as a [PlaceholderText] constant, so the texts are named once,
/// with the exact wording the details have always used (FDW-3580).
/// </remarks>
[PlaceholderText]
public static class HttpPlaceholders
{
    /// <summary>The response carried no reason phrase (HTTP/2 and later never send one).</summary>
    public const string NoReasonPhrase = "Unknown";

    /// <summary>A GraphQL error arrived without the message the GraphQL specification requires.</summary>
    public const string NoGraphQLErrorMessage = "Unknown error";

    /// <summary>A SOAP fault arrived without its fault code.</summary>
    public const string NoFaultCode = "Unknown";

    /// <summary>A SOAP fault arrived without its fault string or reason text.</summary>
    public const string NoFaultString = "Unknown SOAP fault";
}
