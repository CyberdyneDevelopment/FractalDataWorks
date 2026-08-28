using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Web.RestEndpoints.OpenApi.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace Fdw.Web.RestEndpoints.OpenApi;

/// <summary>
/// Filters OpenAPI operations based on the current user's permissions.
/// Operations with policies the user doesn't have are removed from the document,
/// so Scalar only shows endpoints the user can actually call.
/// </summary>
/// <remarks>
/// <para>
/// FastEndpoints maps <c>Policies("fdw:connections:read")</c> to NSwag security requirements
/// on each operation. This processor inspects those security requirements and removes
/// operations whose required policies are not in the user's permission claims.
/// </para>
/// <para>
/// The processor reads the <c>permission</c> claims from the current user's JWT.
/// Admin users (role claim = "Admin") see all operations unfiltered.
/// Unauthenticated users see only the health check and auth endpoints.
/// </para>
/// <para>
/// Must call <see cref="Initialize"/> after <c>app.Build()</c> to provide the service provider.
/// </para>
/// </remarks>
public sealed class PermissionFilterDocumentProcessor : IDocumentProcessor
{
    private const string HealthCheckPath = "/healthz";

    private IServiceProvider? _serviceProvider;

    /// <summary>
    /// Sets the service provider for resolving IHttpContextAccessor and ILogger at document generation time.
    /// Must be called after app.Build().
    /// </summary>
    public void Initialize(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public void Process(DocumentProcessorContext context)
    {
        if (_serviceProvider is null)
            return;

        var httpContextAccessor = _serviceProvider.GetService<IHttpContextAccessor>();
        if (httpContextAccessor is null)
            return;

        var logger = _serviceProvider.GetService<ILogger<PermissionFilterDocumentProcessor>>()
            ?? NullLogger<PermissionFilterDocumentProcessor>.Instance;

        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            OpenApiProcessorLog.FilteredToPublicOnly(logger);
            FilterToPublicOnly(context.Document);
            return;
        }

        var systemRoleConfig = _serviceProvider.GetService<ISystemRoleConfiguration>();
        if (systemRoleConfig is not null && systemRoleConfig.IsInRole(user, systemRoleConfig.AdminRoleName))
        {
            var totalOps = CountOperations(context.Document);
            OpenApiProcessorLog.AdminUserShowAll(logger, totalOps);
            return;
        }

        var userPermissions = user.FindAll("permission")
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var removedCount = FilterByPermissions(context.Document, userPermissions, logger);

        var remainingCount = CountOperations(context.Document);
        OpenApiProcessorLog.FilteredOperations(logger, removedCount, remainingCount, userPermissions.Count);
    }


    /// <summary>
    /// Removes operations the user lacks permission for and returns the count removed.
    /// </summary>
    private static int FilterByPermissions(
        OpenApiDocument document,
        HashSet<string> userPermissions,
        ILogger logger)
    {
        var removedCount = 0;
        var pathsToRemove = new List<string>();

        foreach (var (pathKey, pathItem) in document.Paths)
        {
            if (pathKey.EndsWith(HealthCheckPath, StringComparison.OrdinalIgnoreCase))
                continue;

            removedCount += FilterPathOperations(pathItem, pathKey, userPermissions, logger);

            if (!pathItem.Any())
                pathsToRemove.Add(pathKey);
        }

        foreach (var pathKey in pathsToRemove)
            document.Paths.Remove(pathKey);

        return removedCount;
    }

    /// <summary>
    /// Removes unauthorized operations from a single path item and returns the count removed.
    /// </summary>
    private static int FilterPathOperations(
        OpenApiPathItem pathItem,
        string pathKey,
        HashSet<string> userPermissions,
        ILogger logger)
    {
        var operationsToRemove = new List<string>();

        foreach (var (method, operation) in pathItem)
        {
            var requiredPolicy = GetRequiredPolicy(operation);
            if (requiredPolicy is null)
                continue;

            if (string.Equals(requiredPolicy, "fdw:authenticated", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!HasPermission(userPermissions, requiredPolicy))
            {
                operationsToRemove.Add(method);
                OpenApiProcessorLog.RemovedOperation(logger, operation.OperationId ?? method, pathKey, requiredPolicy);
            }
        }

        foreach (var method in operationsToRemove)
            pathItem.Remove(method);

        return operationsToRemove.Count;
    }

    /// <summary>
    /// Extracts the required FDW policy name from an operation's security requirements.
    /// </summary>
    /// <remarks>
    /// FastEndpoints maps <c>Policies("fdw:connections:read")</c> to a security requirement
    /// where the scheme name is the policy name. This method finds the first FDW-prefixed
    /// security requirement.
    /// </remarks>
    private static string? GetRequiredPolicy(OpenApiOperation operation)
    {
        foreach (var securityRequirement in operation.Security)
        {
            foreach (var (schemeName, _) in securityRequirement)
            {
                if (string.Equals(schemeName, "authenticated", StringComparison.Ordinal))
                    return schemeName;

                var parts = schemeName.Split(':', 2);
                if (parts.Length == 2 && !string.IsNullOrEmpty(parts[0]) && !string.IsNullOrEmpty(parts[1]))
                    return schemeName;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks whether the user has the required permission for a given policy.
    /// </summary>
    private static bool HasPermission(HashSet<string> userPermissions, string policyName)
    {
        return userPermissions.Contains(policyName);
    }

    /// <summary>
    /// Removes all operations except health check and auth endpoints.
    /// </summary>
    private static void FilterToPublicOnly(OpenApiDocument document)
    {
        var pathsToRemove = new List<string>();

        foreach (var (pathKey, _) in document.Paths)
        {
            if (pathKey.EndsWith(HealthCheckPath, StringComparison.OrdinalIgnoreCase))
                continue;

            if (pathKey.Contains("/auth/", StringComparison.OrdinalIgnoreCase) ||
                pathKey.Contains("/login", StringComparison.OrdinalIgnoreCase) ||
                pathKey.Contains("/token", StringComparison.OrdinalIgnoreCase))
                continue;

            pathsToRemove.Add(pathKey);
        }

        foreach (var pathKey in pathsToRemove)
        {
            document.Paths.Remove(pathKey);
        }
    }

    private static int CountOperations(OpenApiDocument document)
    {
        var count = 0;
        foreach (var (_, pathItem) in document.Paths)
        {
            count += pathItem.Count;
        }

        return count;
    }
}
