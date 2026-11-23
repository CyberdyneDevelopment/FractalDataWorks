using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Settings.Configuration;
using Fdw.Services.Settings.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Settings;

/// <summary>Supplies the server setting configuration.</summary>
/// <remarks>
/// A setting's <c>Description</c> is not a column of <c>settings.ServerSettingConfiguration</c> (FDW-6208): it is a row of the descriptions table under the description
/// type <c>ServerSetting</c>, written in the unit of the setting's save and removed with the setting. This provider never fills the text on load: the description language
/// is itself a server setting, so a read that filled it would read the setting it is reading. The endpoint or service that shows a text reads it once for its answer.
/// A record loaded here carries a null text, and saving it unchanged keeps the stored one.
/// </remarks>
public sealed class ServerSettingConfigurationProvider
    : DomainConfigurationProviderBase<IServerSettingImplementationConfiguration>,
      IServerSettingConfigurationProvider,
      IConfigurationWritePreparer<IServerSettingImplementationConfiguration>,
      IConfigurationWriteObserver<IServerSettingImplementationConfiguration>,
      IReferencedConfigurationProvider<IServerSettingImplementationConfiguration>
{
    private const string DescriptionType = ServerSettingDescriptionType.OptionName;
    private const string DescriptionSubType = DescriptionTextSubType.OptionName;

    private readonly Lazy<IDescriptionWriter> _writer;
    private readonly ILogger<ServerSettingConfigurationProvider> _logger;

    // The language each save in flight will write its text in, resolved before the save's unit opened and keyed by the record the save passes to both steps (FDW-7256).
    private readonly ConditionalWeakTable<IServerSettingImplementationConfiguration, string> _languages = new();

    /// <summary>Initializes a new instance of the <see cref="ServerSettingConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    /// <param name="writer">Writes each setting's description in the unit of the setting's save (FDW-6208). Lazy because the description service reads the
    /// description language through the effective settings, which read through this provider (the shape of <c>Lazy&lt;IEffectiveSettingsProvider&gt;</c>, FDW-3284).</param>
    public ServerSettingConfigurationProvider(
        ILogger<ServerSettingConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName,
        Lazy<IDescriptionWriter> writer)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
        _writer = writer;
        _logger = logger ?? NullLogger<ServerSettingConfigurationProvider>.Instance;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Resolves the description language BEFORE the unit opens, for a save that states a text or a blank (FDW-7256): the language is read from the very tables this save writes, and a
    /// read on another connection would wait for the save's own uncommitted rows. A language that is not stated fails the save here, with
    /// <c>DescriptionLanguageNotStated</c>, before any unit is open; nothing is written. A save that states no text asks nothing.
    /// </remarks>
    public Task<IGenericResult> Prepare(IServerSettingImplementationConfiguration record, CancellationToken cancellationToken = default)
    {
        if (record.Description is null)
        {
            return Task.FromResult<IGenericResult>(GenericResult.Success());
        }

        var language = _writer.Value.Language();
        if (language.IsFailure || language.Value is not { } stated)
        {
            return Task.FromResult<IGenericResult>(language);
        }

        _languages.AddOrUpdate(record, stated);
        return Task.FromResult<IGenericResult>(GenericResult.Success());
    }

    /// <inheritdoc />
    /// <remarks>
    /// A save that states no text keeps the stored one; a stated blank removes it; a text is written in the language <see cref="Prepare"/> resolved, so nothing here reads a setting.
    /// A save into a unit the caller opened was not prepared and cannot state a text: it fails (<c>SETTINGDESCWRITE-60385</c>) and writes nothing.
    /// </remarks>
    public Task<IGenericResult> Written(IServerSettingImplementationConfiguration record, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
    {
        if (record.Description is null)
        {
            return Task.FromResult<IGenericResult>(GenericResult.Success());
        }

        if (!_languages.TryGetValue(record, out var language))
        {
            return Task.FromResult<IGenericResult>(GenericResult.Failure(ServerSettingDescriptionLog.NotPrepared(_logger, record.Name)));
        }

        _languages.Remove(record);
        return _writer.Value.DescribeInWhenStated(language, DescriptionType, DescriptionSubType, record.Id, record.Name, record.Description, unit, cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>A setting's descriptions name it, so they go with it, in every language, before it is retired.</remarks>
    public Task<IGenericResult> RemoveReferences(Guid memberId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
        => _writer.Value.DeleteAbout(DescriptionType, memberId, unit, cancellationToken);
}
