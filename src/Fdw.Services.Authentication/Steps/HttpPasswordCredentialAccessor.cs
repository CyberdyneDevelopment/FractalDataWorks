using System;
using System.Linq;
using Fdw.Services.Authentication.Abstractions.Steps;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Authentication.Steps;

/// <summary>
/// Reads the username and password out of the current request.
/// </summary>
/// <remarks>
/// Accepts a JSON body or a form, because both are how a login arrives in practice and supporting
/// each costs nothing. A credential is read on demand and never held: this type keeps no field for
/// one, so nothing that inspects it later can find a password in it.
/// </remarks>
public sealed class HttpPasswordCredentialAccessor : IPasswordCredentialAccessor
{
    private readonly IHttpContextAccessor _context;

    /// <summary>Initializes a new instance of the <see cref="HttpPasswordCredentialAccessor"/> class.</summary>
    /// <param name="context">Supplies the current request.</param>
    public HttpPasswordCredentialAccessor(IHttpContextAccessor context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <inheritdoc />
    public string? Username => Read("username");

    /// <inheritdoc />
    public string? Password => Read("password");

    private string? Read(string field)
    {
        var request = _context.HttpContext?.Request;
        if (request is null)
            return null;

        if (request.HasFormContentType
            && request.Form.TryGetValue(field, out var fromForm)
            && fromForm.FirstOrDefault() is { Length: > 0 } formValue)
        {
            return formValue;
        }

        // The model binder has already read a JSON body by the time a step runs, and the stream is
        // not rewindable, so the bound values are put here by the endpoint rather than re-parsed.
        return request.HttpContext.Items.TryGetValue(ItemKey(field), out var bound)
            ? bound as string
            : null;
    }

    /// <summary>The key an endpoint stores a bound credential field under.</summary>
    /// <param name="field">The field name — <c>username</c> or <c>password</c>.</param>
    /// <remarks>
    /// Prefixed so it cannot collide with anything else a host puts in the request's item bag.
    /// </remarks>
    public static string ItemKey(string field) => "Fdw.Authentication.Credential." + field;
}
