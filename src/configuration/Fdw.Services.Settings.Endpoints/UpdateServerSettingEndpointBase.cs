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
/// Base endpoint for updating an existing server-level setting.
/// </summary>
public abstract class UpdateServerSettingEndpointBase : CrudUpdateEndpointBase<UpdateServerSettingRequest, ServerSettingDetailDto>
{
    private readonly IServerSettingConfigurationProvider _provider;
    private readonly IDescriptionReader _reader;

    /// <summary>Initializes a new instance of the <see cref="UpdateServerSettingEndpointBase"/> class.</summary>
    /// <param name="logger">Logger for this endpoint.</param>
    /// <param name="provider">Reads and saves the server settings.</param>
    /// <param name="reader">Reads the description of the setting before the update, once for the answer (FDW-6208).</param>
    protected UpdateServerSettingEndpointBase(ILogger<UpdateServerSettingEndpointBase> logger, IServerSettingConfigurationProvider provider, IDescriptionReader reader) : base(logger)
    {
        _provider = provider;
        _reader = reader;
    }

    /// <inheritdoc />
    protected override string ResourceName => "settings/server";

    /// <inheritdoc />
    protected override string Route => "/settings/server/{SettingName}";

    /// <inheritdoc />
    protected override string EndpointSummary => "Update a server setting";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(UpdateServerSettingRequest request) => request.SettingName;

    /// <inheritdoc />
    protected override async Task<IGenericResult<ServerSettingDetailDto?>> FindForUpdate(UpdateServerSettingRequest request, CancellationToken ct)
    {
        var settingResult = await _provider.Get(request.SettingName, ct).ConfigureAwait(false);
        if (!settingResult.IsSuccess)
            return settingResult.ToNewResult<ServerSettingDetailDto?>();
        IServerSettingImplementationConfiguration? setting = settingResult.Value;

        if (setting is null)
        {
            if (SettingDefinitions.TryGet(request.SettingName, out var definition))
            {
                return GenericResult<ServerSettingDetailDto?>.Success(new ServerSettingDetailDto
                {
                    Id = Guid.Empty,
                    SettingName = request.SettingName,
                    SettingValue = string.Empty,
                    DataType = definition.DataType,
                    Description = definition.Description,
                    IsActive = true
                });
            }

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

    /// <inheritdoc />
    protected override async Task<IGenericResult<ServerSettingDetailDto>> Update(
        UpdateServerSettingRequest request,
        ServerSettingDetailDto existing,
        CancellationToken ct)
    {
        var settingResult = await _provider.Get(request.SettingName, ct).ConfigureAwait(false);
        if (!settingResult.IsSuccess)
            return settingResult.ToNewResult<ServerSettingDetailDto>();
        IServerSettingImplementationConfiguration? setting = settingResult.Value;

        if (setting is null)
        {
            if (!SettingDefinitions.TryGet(request.SettingName, out var definition))
            {
                SettingsEndpointLog.ServerSettingNotFound(Logger, request.SettingName);
                return GenericResult<ServerSettingDetailDto>.Failure(
                    SettingsEndpointLog.ServerSettingNotFound(Logger, request.SettingName));
            }

            setting = new ServerSettingImplementationConfiguration
            {
                Name = request.SettingName,
                DataType = definition.DataType,
                IsActive = true
            };
        }

        if (request.SettingValue is not null) setting.SettingValue = request.SettingValue;
        if (request.Description is not null) setting.Description = request.Description;
        if (request.MinValue is not null) setting.MinValue = request.MinValue;
        if (request.MaxValue is not null) setting.MaxValue = request.MaxValue;
        if (request.IsActive.HasValue) setting.IsActive = request.IsActive.Value;

        var saveResult = await _provider.Save(setting, "ServerSetting", "ServerSetting", setting.Name, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            return saveResult.ToNewResult<ServerSettingDetailDto>();
        }

        SettingsEndpointLog.UpdatedServerSetting(Logger, request.SettingName);

        // A text the caller stated is what is stored now (a blank removed it); a request that stated none left the stored text as the read before the update showed it.
        setting.Description = request.Description is null ? existing.Description : (string.IsNullOrWhiteSpace(request.Description) ? null : request.Description);

        var detail = new ServerSettingDetailDto
        {
            Id = setting.Id,
            SettingName = setting.Name,
            SettingValue = setting.SettingValue,
            DataType = setting.DataType,
            Description = setting.Description,
            MinValue = setting.MinValue,
            MaxValue = setting.MaxValue,
            IsActive = setting.IsActive
        };

        return GenericResult<ServerSettingDetailDto>.Success(detail);
    }
}
