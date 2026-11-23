using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Configuration;
using Fdw.Services.Settings.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Base endpoint for getting a server-level setting by name.
/// </summary>
public abstract class GetServerSettingEndpointBase : CrudGetEndpointBase<SettingNameRequest, ServerSettingDetailDto>
{
    private readonly IServerSettingConfigurationProvider _provider;
    private readonly IDescriptionReader _reader;

    /// <summary>Initializes a new instance of the <see cref="GetServerSettingEndpointBase"/> class.</summary>
    /// <param name="logger">Logger for this endpoint.</param>
    /// <param name="provider">Reads the server settings.</param>
    /// <param name="reader">Reads the description of the setting shown, once for the answer (FDW-6208).</param>
    protected GetServerSettingEndpointBase(ILogger<GetServerSettingEndpointBase> logger, IServerSettingConfigurationProvider provider, IDescriptionReader reader) : base(logger)
    {
        _provider = provider;
        _reader = reader;
    }

    /// <inheritdoc />
    protected override string ResourceName => "settings/server";

    /// <inheritdoc />
    protected override string Route => "/settings/server/{SettingName}";

    /// <inheritdoc />
    protected override string EndpointSummary => "Get server setting by name";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(SettingNameRequest request) => request.SettingName;

    /// <inheritdoc />
    protected override async Task<IGenericResult<ServerSettingDetailDto?>> FindByIdentifier(SettingNameRequest request, CancellationToken ct)
    {
        SettingsEndpointLog.GettingServerSetting(Logger, request.SettingName);

        var settingResult = await _provider.Get(request.SettingName, ct).ConfigureAwait(false);
        if (!settingResult.IsSuccess)
            return settingResult.ToNewResult<ServerSettingDetailDto?>();
        IServerSettingImplementationConfiguration? setting = settingResult.Value;

        if (setting is null)
        {
            SettingsEndpointLog.ServerSettingNotFound(Logger, request.SettingName);
            return GenericResult<ServerSettingDetailDto?>.Success(null);
        }

        var detail = new ServerSettingDetailDto
        {
            Id = setting.Id,
            SettingName = setting.Name,
            SettingValue = setting.SettingValue,
            DataType = setting.DataType,
            MinValue = setting.MinValue,
            MaxValue = setting.MaxValue,
            IsActive = setting.IsActive
        };

        // The provider never fills a description on load; one read answers this setting, and a read that fails fails the route (no text stands in).
        var filled = await _reader.Fill<ServerSettingDetailDto>(ServerSettingDescriptionType.OptionName, DescriptionTextSubType.OptionName, [detail], item => item.Id, (item, text) => item.Description = text, ct).ConfigureAwait(false);
        if (filled.IsFailure)
        {
            return filled.ToNewResult<ServerSettingDetailDto?>();
        }

        return GenericResult<ServerSettingDetailDto?>.Success(detail);
    }
}
