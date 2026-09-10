using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Dataverses.Abstractions;
using Fdw.Services.Dataverses.Commands;
using Fdw.Services.Dataverses.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Dataverses;

/// <summary>
/// Reads and writes <see cref="DataverseConfiguration"/> and its children.
/// </summary>
/// <remarks>
/// There are no Get overrides here. The base composes the aggregate — members, resources and
/// relationships come back populated — because those are direct children of the dataverse row.
/// <c>DataSetConfigurationProvider</c> overrides Get only to reach a grandchild, which a dataverse
/// does not have.
/// </remarks>
public class DataverseConfigurationProvider
    : ImplementationConfigurationProviderBase<IDataverseImplementationConfiguration>,
      IDataverseConfigurationProvider
{

    /// <summary>
    /// Registers the provider and the interfaces callers resolve it through.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static void RegisterDomainConfiguration(IServiceCollection services)
    {
        services.TryAddSingleton<DataverseConfigurationProvider>(sp =>
            new DataverseConfigurationProvider(
                sp.GetService<ILogger<DataverseConfigurationProvider>>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                DataStoreTypes.ConfigurationConnection,
                "dataverse"));

        services.TryAddSingleton<ImplementationConfigurationProviderBase<IDataverseImplementationConfiguration>>(
            sp => sp.GetRequiredService<DataverseConfigurationProvider>());

        services.TryAddSingleton<IDataverseConfigurationProvider>(
            sp => sp.GetRequiredService<DataverseConfigurationProvider>());

        // Scoped, unlike the providers above: it reads the calling user off the ambient
        // authentication context, so it answers per request rather than once per process.
        services.TryAddScoped<IDataverseAccessPolicy>(sp =>
            new DataverseAccessPolicy(
                sp.GetRequiredService<IAuthenticationContextAccessor>(),
                sp.GetRequiredService<RoleConfigurationProvider>(),
                sp.GetService<ILogger<DataverseAccessPolicy>>()));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataverseConfigurationProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger, or null for a functional provider without logging.</param>
    /// <param name="gatewayProvider">The configuration gateway provider.</param>
    /// <param name="dataStoreName">The data store holding the configuration.</param>
    /// <param name="pathName">The schema the dataverse tables live in.</param>
    public DataverseConfigurationProvider(
        ILogger<DataverseConfigurationProvider>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "dataverse")
        : base(logger ?? NullLogger<DataverseConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName,
               pathName, "Dataverse")
    {
        _logger = logger ?? NullLogger<DataverseConfigurationProvider>.Instance;
    }

    private readonly ILogger _logger;







    /// <summary>Loads the aggregate and picks one child out of it.</summary>
    /// <remarks>
    /// Reading the aggregate to locate a child is fine — it is the WRITE that must stay narrow.
    /// The id selector is passed rather than reflected: this layer stays reflection-free, the same
    /// reason the cascade sets child FKs through a generated mapper instead of a property lookup.
    /// </remarks>
    private async Task<IGenericResult<TChild>> FindChild<TChild>(
        Guid dataverseId,
        Func<DataverseConfiguration, System.Collections.Generic.IList<TChild>> select,
        Func<TChild, Guid> id,
        Guid childId,
        string childKind,
        CancellationToken cancellationToken)
        where TChild : class
    {
        var dataverse = await Get(dataverseId, cancellationToken).ConfigureAwait(false);
        if (dataverse.IsFailure) return dataverse.ToNewResult<TChild>();
        if (dataverse.Value is null)
        {
            return GenericResult<TChild>.Failure(
                DataversesResultCodes.ByName("DataverseLoadReturnedNoValue"), _logger,
                ResultDetails.Create("name", dataverseId.ToString()));
        }

        var match = select(dataverse.Value).FirstOrDefault(c => id(c) == childId);

        return match is null
            ? GenericResult<TChild>.Failure(
                DataversesResultCodes.ByName("DataverseChildNotFound"), _logger,
                ResultDetails.Create("name", dataverseId.ToString(), "kind", childKind, "id", childId.ToString()))
            : GenericResult<TChild>.Success(match);
    }
}
