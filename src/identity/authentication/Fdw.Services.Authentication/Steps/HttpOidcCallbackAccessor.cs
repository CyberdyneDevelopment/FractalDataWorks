using System;
using System.Linq;
using Fdw.Services.Authentication.Abstractions.Steps;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Authentication.Steps;

/// <summary>
/// Reads the authorization code and state the provider sent back.
/// </summary>
/// <remarks>
/// Query string only. A provider returning a code by any other route is a provider doing something
/// unusual, and accepting one from a header or body would widen where a code may arrive without
/// widening what checks it.
/// </remarks>
public sealed class HttpOidcCallbackAccessor : IOidcCallbackAccessor
{
    private readonly IHttpContextAccessor _context;

    /// <summary>Initializes a new instance of the <see cref="HttpOidcCallbackAccessor"/> class.</summary>
    /// <param name="context">Supplies the current request.</param>
    public HttpOidcCallbackAccessor(IHttpContextAccessor context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <inheritdoc />
    public string? Code => Query("code");

    /// <inheritdoc />
    public string? State => Query("state");

    private string? Query(string key)
        => _context.HttpContext?.Request.Query.TryGetValue(key, out var values) == true
            ? values.FirstOrDefault()
            : null;
}
