using System;
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
/// /connect/token had no tag and no body, so there was no way to log in from Scalar. It runs at
/// generation, before any caller is known; an API reference cuts the result per caller afterwards.
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

    // Why the shared rule and not this processor's own: a document gets its groups from one place, whichever
    // processor a host attaches (FDW-1100).
    private static void NormalizeTags(OpenApiOperation operation, string path)
    {
        var tag = OperationTags.For(path, [.. operation.Tags]);
        operation.Tags.Clear();
        if (tag is not null)
            operation.Tags.Add(tag);
    }

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
