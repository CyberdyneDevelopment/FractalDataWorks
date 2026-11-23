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
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Identity.Commands;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Identity;

/// <summary>Supplies the Identity configuration.</summary>
/// <remarks>
/// An identity's <c>Description</c> is not a column of any <c>sec</c> identity table (FDW-6208): it is a row of the descriptions table under the description type <c>Identity</c>,
/// keyed by the identity's Id whichever mechanism its typed body belongs to. This domain never fills a record on load (the descriptions domain reads the settings and the caller's
/// authentication, which would make a fill loop back into it), so a record that was read carries no text. The save is the only side this provider takes: a text is written in the
/// unit of the save, a save that states none keeps the stored text, and a stated blank removes it.
/// </remarks>
public sealed class IdentityServiceConfigurationProvider
    : DomainConfigurationProviderBase<IIdentityServiceImplementationConfiguration>,
      IIdentityServiceConfigurationProvider,
      IConfigurationWriteObserver<IIdentityServiceImplementationConfiguration>,
      IReferencedConfigurationProvider<IIdentityServiceImplementationConfiguration>
{
    private const string DescriptionType = IdentityDescriptionType.OptionName;
    private const string DescriptionSubType = DescriptionTextSubType.OptionName;

    private readonly IDescriptionWriter _writer;

    /// <summary>Initializes a new instance of the <see cref="IdentityServiceConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    /// <param name="writer">Writes each identity's description in the unit of the identity's save (FDW-6208).</param>
    public IdentityServiceConfigurationProvider(
        ILogger<IdentityServiceConfigurationProvider> logger,
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
    /// <remarks>A save with no text (null) keeps the text stored; a text is written; an empty or blank text removes it. Nothing invents one.</remarks>
    public Task<IGenericResult> Written(IIdentityServiceImplementationConfiguration record, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.DescribeWhenStated(DescriptionType, DescriptionSubType, record.Id, record.Name, record.Description, unit, cancellationToken);

    /// <inheritdoc />
    /// <remarks>An identity's descriptions name it, so they go with it, in every language, before it is retired.</remarks>
    public Task<IGenericResult> RemoveReferences(Guid memberId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.DeleteAbout(DescriptionType, memberId, unit, cancellationToken);
}
