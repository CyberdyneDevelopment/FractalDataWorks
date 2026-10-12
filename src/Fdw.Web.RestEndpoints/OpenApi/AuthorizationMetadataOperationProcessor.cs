using Microsoft.AspNetCore.Authorization;
using NSwag.Generation.AspNetCore;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace Fdw.Web.RestEndpoints.OpenApi;

/// <summary>
/// Stamps every operation with who may call it, read from the endpoint's own authorization metadata,
/// so an API reference document can be cut down to one caller after it is generated.
/// </summary>
/// <remarks>
/// <para>
/// FastEndpoints writes only its bearer scheme into an operation's <c>security</c>, never the policy
/// the endpoint requires, so a generated document cannot say who may call what. The permission filter
/// this replaces looked for policy names in <c>security</c> and so never found one (FDW-919). The
/// endpoint metadata does carry it: <see cref="IAllowAnonymous"/> marks a public route, and each
/// <see cref="IAuthorizeData"/> names a policy, which in FDW is the permission a caller must hold
/// (<c>workspaces:query</c>) - the same reading <c>PermissionClaimsPreProcessor</c> makes per request.
/// </para>
/// <para>
/// Policies FastEndpoints builds itself for roles, claims and permissions are named
/// <c>epPolicy:</c> and are not a permission name, so they are left out, as the pre-processor leaves
/// them out.
/// </para>
/// <para>
/// Generation runs without a caller, so the stamps depend only on the build: a document generated
/// once serves every caller, and each request cuts its own copy.
/// </para>
/// </remarks>
public sealed class AuthorizationMetadataOperationProcessor : IOperationProcessor
{
    /// <summary>The operation extension that says whether the operation admits anonymous callers.</summary>
    public const string AnonymousExtension = "x-fdw-anonymous";

    /// <summary>The operation extension that lists the policies a caller must hold.</summary>
    public const string PoliciesExtension = "x-fdw-policies";

    private const string FastEndpointsPolicyPrefix = "epPolicy:";

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// The operation was not described from an ASP.NET Core endpoint, so it has no metadata to read.
    /// An unstamped operation would reach a caller unfiltered, so generation fails instead.
    /// </exception>
    public bool Process(OperationProcessorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context is not AspNetCoreOperationProcessorContext aspNetCore)
        {
            throw new InvalidOperationException(
                "Operation '" + context.OperationDescription.Path + "' was not described from an ASP.NET Core endpoint, "
                + "so who may call it cannot be read. An API reference document cannot be filtered without it.");
        }

        var metadata = aspNetCore.ApiDescription.ActionDescriptor.EndpointMetadata;
        var operation = context.OperationDescription.Operation;

        operation.ExtensionData ??= new Dictionary<string, object?>(StringComparer.Ordinal);
        operation.ExtensionData[AnonymousExtension] = metadata.OfType<IAllowAnonymous>().Any();
        operation.ExtensionData[PoliciesExtension] = metadata.OfType<IAuthorizeData>()
            .Select(data => data.Policy)
            .OfType<string>()
            .Where(policy => !policy.StartsWith(FastEndpointsPolicyPrefix, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return true;
    }
}
