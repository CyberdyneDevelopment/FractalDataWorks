using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using Fdw.Services.SecretManagers.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.SecretManagers;

/// <summary>Supplies the SecretManager configuration.</summary>
/// <remarks>
/// A secret manager's <c>Description</c> is not a column of any <c>sec</c> secret manager table (FDW-6208): it is a row of the descriptions table under the description type
/// <c>SecretManager</c>, keyed by the secret manager's Id whichever kind its typed body belongs to. This domain never fills a record on load (the descriptions domain reads
/// through the configuration store that secret managers open), so a record that was read carries no text. A kind whose body holds a text states it by implementing
/// <see cref="IDescribedConfiguration"/>; the save is the only side this provider takes for such a body: a text is written in the unit of the save, a save that states none
/// keeps the stored text, and a stated blank removes it. A body that states no text is left alone. No secret value is read, named or copied.
/// </remarks>
public sealed class SecretManagerConfigurationProvider
    : DomainConfigurationProviderBase<ISecretManagerImplementationConfiguration>,
      ISecretManagerConfigurationProvider,
      IConfigurationWriteObserver<ISecretManagerImplementationConfiguration>,
      IReferencedConfigurationProvider<ISecretManagerImplementationConfiguration>
{
    private const string DescriptionType = SecretManagerDescriptionType.OptionName;
    private const string DescriptionSubType = DescriptionTextSubType.OptionName;

    private readonly IDescriptionWriter _writer;

    /// <summary>Initializes a new instance of the <see cref="SecretManagerConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    /// <param name="writer">Writes each secret manager's description in the unit of the secret manager's save (FDW-6208).</param>
    public SecretManagerConfigurationProvider(
        ILogger<SecretManagerConfigurationProvider> logger,
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
    /// <remarks>A body with no description member, or a save with no text (null), keeps what is stored; a text is written; an empty or blank text removes it. Nothing invents one.</remarks>
    public Task<IGenericResult> Written(ISecretManagerImplementationConfiguration record, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => record is IDescribedConfiguration described
            ? _writer.DescribeWhenStated(DescriptionType, DescriptionSubType, record.Id, record.Name, described.Description, unit, cancellationToken)
            : Task.FromResult<IGenericResult>(GenericResult.Success());

    /// <inheritdoc />
    /// <remarks>A secret manager's descriptions name it, so they go with it, in every language, before it is retired.</remarks>
    public Task<IGenericResult> RemoveReferences(Guid memberId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.DeleteAbout(DescriptionType, memberId, unit, cancellationToken);
}
