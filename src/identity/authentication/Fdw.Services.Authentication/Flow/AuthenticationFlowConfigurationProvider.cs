using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Flow;

/// <summary>Supplies the AuthenticationFlow domain configuration.</summary>
/// <remarks>
/// A flow's <c>Description</c> is not a column of <c>auth.AuthenticationFlowConfiguration</c> (FDW-6208): it is a row of the descriptions table under the description type
/// <c>AuthenticationFlow</c>, written in the unit of the flow's save and removed with the flow. The flow is read at sign-in, with no caller to state a description language,
/// so this provider never fills the text on load: the flow administration, which shows it, reads it once for its answer. A flow loaded here carries a null text, and saving
/// it again (a gate or an on/off switch changed) keeps the stored one.
/// </remarks>
public sealed class AuthenticationFlowConfigurationProvider
    : DomainConfigurationProviderBase<IAuthenticationFlowImplementationConfiguration>,
      IAuthenticationFlowConfigurationProvider,
      IConfigurationWriteObserver<IAuthenticationFlowImplementationConfiguration>,
      IReferencedConfigurationProvider<IAuthenticationFlowImplementationConfiguration>
{
    private const string DescriptionType = AuthenticationFlowDescriptionType.OptionName;
    private const string DescriptionSubType = DescriptionTextSubType.OptionName;

    private readonly IDescriptionWriter _writer;

    /// <summary>Initializes a new instance of the <see cref="AuthenticationFlowConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    /// <param name="writer">Writes each flow's description in the unit of the flow's save (FDW-6208).</param>
    public AuthenticationFlowConfigurationProvider(
        ILogger<AuthenticationFlowConfigurationProvider> logger,
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
    public Task<IGenericResult> Written(IAuthenticationFlowImplementationConfiguration record, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.DescribeWhenStated(DescriptionType, DescriptionSubType, record.Id, record.Name, record.Description, unit, cancellationToken);

    /// <inheritdoc />
    /// <remarks>A flow's descriptions name it, so they go with it, in every language, before it is retired.</remarks>
    public Task<IGenericResult> RemoveReferences(Guid memberId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.DeleteAbout(DescriptionType, memberId, unit, cancellationToken);
}
