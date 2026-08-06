using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Settings.Configuration;
using Fdw.Services.Settings.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Base endpoint for getting a server-level setting by name.
/// </summary>
public abstract class GetServerSettingEndpointBase : CrudGetEndpoint<SettingNameRequest, ServerSettingDetailDto>
{
    // Why: SettingsConfigurationProvider replaces IOptionsMonitor<List<ServerSettingConfiguration>>
    // with dual-source (ctrl + cfg) provider that provides server/tenant/role settings.
    private readonly SettingsConfigurationProvider _provider;

    /// <inheritdoc />
    protected GetServerSettingEndpointBase(SettingsConfigurationProvider provider)
    {
        _provider = provider;
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

        var settingResult = await _provider.GetServerSetting(request.SettingName, ct).ConfigureAwait(false);
        var setting = settingResult.IsSuccess ? settingResult.Value : null;

        if (setting is null)
        {
            SettingsEndpointLog.ServerSettingNotFound(Logger, request.SettingName);
            return GenericResult<ServerSettingDetailDto?>.Success(null);
        }

        var detail = new ServerSettingDetailDto
        {
            Id = setting.Id,
            SettingName = setting.SettingName,
            SettingValue = setting.SettingValue,
            DataType = setting.DataType,
            Description = setting.Description,
            MinValue = setting.MinValue,
            MaxValue = setting.MaxValue,
            IsActive = setting.IsActive
        };

        return GenericResult<ServerSettingDetailDto?>.Success(detail);
    }
}
