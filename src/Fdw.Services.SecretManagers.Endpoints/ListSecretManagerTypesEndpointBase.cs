using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Endpoints.ConfigurationMetadata;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.SecretManagers.Endpoints;

/// <summary>
/// Generic base endpoint for listing available secret manager types from the source-generated collection.
/// </summary>
public abstract class ListSecretManagerTypesEndpointBase : CrudListEndpointBase<SecretManagerTypeDto>
{
    /// <summary>Initializes a new instance of the <see cref="ListSecretManagerTypesEndpointBase"/> class.</summary>
    protected ListSecretManagerTypesEndpointBase(ILogger<ListSecretManagerTypesEndpointBase> logger) : base(logger)
    {
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "secret-manager-types";

    /// <summary>
    /// Gets the authorization policy for read access: the platform administrator's, as every other secret manager route asks. The permission this
    /// route asked before, <c>secret-managers:read</c>, is held by no role, so the platform administrator was refused (FDW-2890).
    /// </summary>
    protected override string ReadPolicy => AuthConstants.PlatformAdminPolicy;

    /// <summary>Gets the OpenAPI summary for this endpoint.</summary>
    protected override string EndpointSummary => "List available secret manager types";

    /// <summary>Gets the OpenAPI description for this endpoint.</summary>
    protected override string EndpointDescription =>
        "Returns all secret manager types registered via source-generated ServiceTypeCollections.";

    /// <summary>Loads all registered secret manager types as summary DTOs.</summary>
    protected override Task<IGenericResult<List<SecretManagerTypeDto>>> LoadItems(CancellationToken ct)
    {
        var items = SecretManagerTypes.All()
            .Select(kvp => MapToDto(kvp.Value))
            .ToList();
        return Task.FromResult(GenericResult<List<SecretManagerTypeDto>>.Success(items));
    }

    /// <summary>Maps a single secret manager type to a DTO.</summary>
    protected virtual SecretManagerTypeDto MapToDto(ISecretManagerType secretManagerType)
    {
        return new SecretManagerTypeDto
        {
            Name = secretManagerType.Name,
            DisplayName = secretManagerType.DisplayName,
            Description = secretManagerType.Description,
            SettingsContainer = secretManagerType.Container,
            Settings = SettingsOf(secretManagerType)
        };
    }

    // The settings the kind's configuration class declares, limited to the ones POST and PATCH accept (SecretManagerSettings.Takes), so the
    // form a client builds from this list and the reader that takes its answer cannot disagree.
    private static IReadOnlyList<ConfigurationPropertyInfoDto> SettingsOf(ISecretManagerType secretManagerType)
    {
        var takes = SecretManagerSettings.Takes(secretManagerType.ConfigurationType);
        return [.. ConfigurationPropertyExtractor.ExtractProperties(secretManagerType.ConfigurationType)
            .Where(property => takes.Contains(property.Name, StringComparer.Ordinal))];
    }
}
