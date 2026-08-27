using System;
using System.Linq;
using Fdw.Services.Authentication.Abstractions.Steps;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Authentication.Steps;

/// <summary>
/// Reads the externally-issued token out of the current request.
/// </summary>
/// <remarks>
/// Looks in the token-exchange form field first, then the Authorization header. The form field is
/// where RFC 8693 puts it, and the header is where a caller who has not read the spec will put it —
/// accepting both costs nothing and turns a confusing failure into a working request.
/// </remarks>
public sealed class HttpForeignTokenAccessor : IForeignTokenAccessor
{
    private readonly IHttpContextAccessor _context;

    /// <summary>Initializes a new instance of the <see cref="HttpForeignTokenAccessor"/> class.</summary>
    /// <param name="context">Supplies the current request.</param>
    public HttpForeignTokenAccessor(IHttpContextAccessor context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <inheritdoc />
    public string? Token
    {
        get
        {
            var request = _context.HttpContext?.Request;
            if (request is null)
                return null;

            if (request.HasFormContentType
                && request.Form.TryGetValue("subject_token", out var exchanged)
                && exchanged.FirstOrDefault() is { Length: > 0 } fromForm)
            {
                return fromForm;
            }

            var header = request.Headers.Authorization.FirstOrDefault();

            return header?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true
                ? header["Bearer ".Length..].Trim()
                : null;
        }
    }
}
