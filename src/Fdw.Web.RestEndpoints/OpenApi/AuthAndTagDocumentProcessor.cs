using System;
using System.Collections.Generic;
using System.Linq;
using NJsonSchema;
using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace Fdw.Web.RestEndpoints.OpenApi;

/// <summary>
/// NSwag document processor that (1) normalizes OpenAPI tags so Scalar groups every endpoint
/// consistently, and (2) augments the OpenIddict <c>/connect/token</c> endpoint — which is
/// registered by middleware and therefore carries no FastEndpoints metadata — with a tag and a
/// form request body so Scalar renders a usable login form.
/// </summary>
/// <remarks>
/// Why: tags were inconsistent ("Auth" vs "Authentication", double-tagging, and the generic
/// security base classes emit a null EndpointTag → untagged operations that Scalar buries).
/// /connect/token had no tag and no body, so there was no way to log in from Scalar. This runs
/// after PermissionFilterDocumentProcessor, so it only touches operations that survived the
/// per-user permission filter.
/// </remarks>
public sealed class AuthAndTagDocumentProcessor : IDocumentProcessor
{
    private readonly string _clientId;
    private readonly string _scope;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthAndTagDocumentProcessor"/> class.
    /// </summary>
    /// <param name="clientId">The OAuth client id prefilled on the token form.</param>
    /// <param name="scope">The scopes prefilled on the token form.</param>
    /// <remarks>
    /// These are the only parts of this processor that belong to a deployment rather than to the
    /// framework — everything else it does (collapsing the auth tags, giving the token endpoint a
    /// usable form) follows from using OpenIddict at all.
    /// </remarks>
    public AuthAndTagDocumentProcessor(string clientId, string scope)
    {
        _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
    }

    private const string AuthenticationTag = "Authentication";

    // Why: OAuth2 password-grant fields. The three fixed values are prefilled as schema defaults
    // so Scalar populates them; username/password are left empty for the caller to type.
    private (string Name, string? Default)[] TokenFormFields =>
    [
        ("grant_type", "password"),
        ("client_id", _clientId),
        ("scope", _scope),
        ("username", null),
        ("password", null),
    ];

    /// <inheritdoc />
    public void Process(DocumentProcessorContext context)
    {
        foreach (var (path, pathItem) in context.Document.Paths)
        {
            foreach (var operation in pathItem.Values)
            {
                NormalizeTags(operation, path);

                if (path.EndsWith("/connect/token", StringComparison.OrdinalIgnoreCase))
                {
                    AugmentTokenEndpoint(operation);
                }
            }
        }
    }

    // Why: collapse the "Auth"/"Authentication" split, drop empty/null tags, dedupe, and give any
    // untagged operation a tag derived from its route so nothing renders ungrouped in Scalar.
    private static void NormalizeTags(OpenApiOperation operation, string path)
    {
        var normalized = operation.Tags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => string.Equals(t, "Auth", StringComparison.OrdinalIgnoreCase)
                ? AuthenticationTag
                : t)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (normalized.Count == 0)
        {
            normalized.Add(DeriveTag(path));
        }

        operation.Tags.Clear();
        foreach (var tag in normalized)
        {
            operation.Tags.Add(tag);
        }
    }

    // Why: /connect/* (OpenIddict) → Authentication; everything else derives a Title-cased tag
    // from the first non-version route segment (e.g. /api/v1/connections → "Connections").
    private static string DeriveTag(string path)
    {
        if (path.Contains("/connect/", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticationTag;
        }

        var segment = path
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(s =>
                !string.Equals(s, "api", StringComparison.OrdinalIgnoreCase)
                && !(s.Length == 2 && (s[0] == 'v' || s[0] == 'V') && char.IsDigit(s[1])));

        if (string.IsNullOrEmpty(segment))
        {
            return "General";
        }

        return char.ToUpperInvariant(segment[0]) + segment[1..];
    }

    // Why: the token endpoint is OpenIddict middleware with no request schema, so Scalar shows no
    // fields to fill. Add a urlencoded form body with the password-grant parameter names (no
    // default values) so the login form is usable directly from the doc.
    private void AugmentTokenEndpoint(OpenApiOperation operation)
    {
        if (operation.RequestBody is not null && operation.RequestBody.Content.Count > 0)
        {
            return;
        }

        var schema = new JsonSchema { Type = JsonObjectType.Object };
        foreach (var (name, defaultValue) in TokenFormFields)
        {
            var property = new JsonSchemaProperty { Type = JsonObjectType.String };
            // Why: prefill the fixed OAuth2 values (grant_type/client_id/scope) via schema default
            // + example so Scalar populates them; username/password stay empty.
            if (defaultValue is not null)
            {
                property.Default = defaultValue;
                property.Example = defaultValue;
            }
            schema.Properties[name] = property;
        }

        var body = new OpenApiRequestBody { IsRequired = true };
        body.Content["application/x-www-form-urlencoded"] = new OpenApiMediaType { Schema = schema };
        operation.RequestBody = body;
    }
}
