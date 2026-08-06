using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Command for executing HTTP requests that have been translated from data commands.
/// </summary>
/// <remarks>
/// <para>
/// Used by REST translators to represent HTTP operations:
/// <list type="bullet">
/// <item>GET - Query operations with OData parameters</item>
/// <item>POST - Insert operations with JSON body</item>
/// <item>PUT/PATCH - Update operations with JSON body</item>
/// <item>DELETE - Delete operations</item>
/// </list>
/// </para>
/// </remarks>
public sealed class HttpConnectionCommand : IConnectionCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpConnectionCommand"/> class.
    /// </summary>
    /// <param name="method">The HTTP method (GET, POST, PUT, PATCH, DELETE).</param>
    /// <param name="relativePath">The relative path/endpoint (e.g., "/api/customers").</param>
    /// <param name="queryParameters">Optional query string parameters (for GET requests).</param>
    /// <param name="body">Optional request body content (for POST/PUT/PATCH).</param>
    /// <param name="headers">Optional HTTP headers.</param>
    public HttpConnectionCommand(
        HttpMethod method,
        string relativePath,
        IReadOnlyDictionary<string, string>? queryParameters = null,
        string? body = null,
        IReadOnlyDictionary<string, string>? headers = null)
    {
        Method = method ?? throw new ArgumentNullException(nameof(method));
        RelativePath = relativePath ?? throw new ArgumentNullException(nameof(relativePath));
        QueryParameters = queryParameters ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Body = body;
        Headers = headers ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        CommandId = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the HTTP method (GET, POST, PUT, PATCH, DELETE).
    /// </summary>
    public HttpMethod Method { get; }

    /// <summary>
    /// Gets the relative path/endpoint (e.g., "/api/customers").
    /// Will be combined with base URL from connection configuration.
    /// </summary>
    public string RelativePath { get; }

    /// <summary>
    /// Gets the query string parameters (for GET requests, OData parameters).
    /// </summary>
    public IReadOnlyDictionary<string, string> QueryParameters { get; }

    /// <summary>
    /// Gets the request body content (for POST/PUT/PATCH requests, typically JSON).
    /// </summary>
    public string? Body { get; }

    /// <summary>
    /// Gets additional HTTP headers to include in the request.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; }

    /// <inheritdoc/>
    public Guid CommandId { get; }

    /// <inheritdoc/>
    public DateTime CreatedAt { get; }

    /// <inheritdoc/>
    public string CommandType => "HttpConnection";

    /// <inheritdoc/>
    public string Category => "Connection";
}
