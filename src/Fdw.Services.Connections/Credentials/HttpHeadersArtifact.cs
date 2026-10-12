using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections;

/// <summary>
/// Credential artifact containing HTTP authentication headers.
/// </summary>
/// <remarks>
/// <para>
/// This artifact is used for HTTP-based connections where authentication
/// is performed via HTTP headers (REST APIs, GraphQL, SOAP, etc.).
/// </para>
/// <para>
/// Common header types include:
/// <list type="bullet">
/// <item><description>Authorization: Bearer {token}</description></item>
/// <item><description>Authorization: Basic {base64credentials}</description></item>
/// <item><description>X-Api-Key: {key}</description></item>
/// <item><description>Custom headers for specific APIs</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Created by credential translator for Bearer auth
/// var headers = new Dictionary&lt;string, string&gt;
/// {
///     ["Authorization"] = "Bearer eyJ0eXAiOiJKV1QiLC..."
/// };
/// var artifact = new HttpHeadersArtifact(headers);
///
/// // Used by factory
/// if (credentials is HttpHeadersArtifact headersArtifact)
/// {
///     foreach (var header in headersArtifact.Headers)
///     {
///         client.DefaultRequestHeaders.Add(header.Key, header.Value);
///     }
/// }
/// </code>
/// </example>
[ExcludeFromCodeCoverage]
public sealed class HttpHeadersArtifact : CredentialArtifactBase
{
    /// <summary>
    /// The artifact type name.
    /// </summary>
    public const string TypeName = "HttpHeaders";

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpHeadersArtifact"/> class.
    /// </summary>
    /// <param name="headers">The HTTP headers to add to requests.</param>
    /// <exception cref="ArgumentNullException">Thrown when headers is null.</exception>
    public HttpHeadersArtifact(IReadOnlyDictionary<string, string> headers)
    {
        Headers = headers ?? throw new ArgumentNullException(nameof(headers));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpHeadersArtifact"/> class from a mutable dictionary.
    /// </summary>
    /// <param name="headers">The HTTP headers to add to requests (will be copied).</param>
    /// <exception cref="ArgumentNullException">Thrown when headers is null.</exception>
    public HttpHeadersArtifact(IDictionary<string, string> headers)
    {
        if (headers == null)
            throw new ArgumentNullException(nameof(headers));

        // Get a defensive copy
        Headers = new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public override string ArtifactType => TypeName;

    /// <summary>
    /// Gets the HTTP headers to add to requests.
    /// </summary>
    /// <remarks>
    /// Headers may contain sensitive information such as API keys or tokens.
    /// Do not log or expose header values.
    /// </remarks>
    public IReadOnlyDictionary<string, string> Headers { get; }

    /// <summary>
    /// Gets a value indicating whether this artifact has no headers.
    /// </summary>
    public bool IsEmpty => Headers.Count == 0;
}
