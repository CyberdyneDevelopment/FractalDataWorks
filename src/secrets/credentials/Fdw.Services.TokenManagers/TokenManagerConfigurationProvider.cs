using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.TokenManagers.Abstractions;
using Fdw.Services.TokenManagers.Commands;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.TokenManagers;

/// <summary>Supplies the TokenManager configuration.</summary>
/// <remarks>
/// A token manager's <c>Description</c> is not a column of <c>auth.JwtTokenManagerConfiguration</c> (FDW-6208): it is a row of the descriptions table under the description
/// type <c>TokenManager</c>, written in the unit of the manager's save and removed with it. No route shows a token manager's text, and a manager is read when a token is
/// issued, with no caller to state a language, so nothing fills it on load: a manager loaded here carries a null text, and saving it again keeps the stored one.
/// </remarks>
public sealed class TokenManagerConfigurationProvider
    : DomainConfigurationProviderBase<ITokenManagerImplementationConfiguration>,
      ITokenManagerConfigurationProvider,
      IConfigurationWriteObserver<ITokenManagerImplementationConfiguration>,
      IReferencedConfigurationProvider<ITokenManagerImplementationConfiguration>
{
    private const string DescriptionType = TokenManagerDescriptionType.OptionName;
    private const string DescriptionSubType = DescriptionTextSubType.OptionName;

    private readonly IDescriptionWriter _writer;

    /// <summary>Initializes a new instance of the <see cref="TokenManagerConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    /// <param name="writer">Writes each token manager's description in the unit of the manager's save (FDW-6208).</param>
    public TokenManagerConfigurationProvider(
        ILogger<TokenManagerConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName,
        IDescriptionWriter writer)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
        _writer = writer;
    }

    /// <inheritdoc />
    /// <remarks>A save that states no text keeps the stored one; a stated blank removes it.</remarks>
    public Task<IGenericResult> Written(ITokenManagerImplementationConfiguration record, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.DescribeWhenStated(DescriptionType, DescriptionSubType, record.Id, record.Name, record.Description, unit, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A token manager's descriptions name it, so they go with it, in every language, before it is retired.</remarks>
    public Task<IGenericResult> RemoveReferences(Guid memberId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.DeleteAbout(DescriptionType, memberId, unit, cancellationToken);
}
