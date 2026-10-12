using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Settings.Configuration;
using Fdw.Services.Settings.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Base endpoint for creating a role-level setting override.
/// </summary>
public abstract class CreateRoleSettingEndpointBase : CrudCreateEndpointBase<CreateRoleSettingRequest, RoleSettingSummaryDto>
{
    private readonly IRoleSettingConfigurationProvider _provider;

    /// <inheritdoc />
    protected CreateRoleSettingEndpointBase(ILogger<CreateRoleSettingEndpointBase> logger, IRoleSettingConfigurationProvider provider) : base(logger)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "settings/role";

    /// <inheritdoc />
    protected override string EndpointSummary => "Create a role setting override";

    /// <inheritdoc />
    protected override string GetResourceName(CreateRoleSettingRequest request) => request.SettingName;

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExists(CreateRoleSettingRequest request, CancellationToken ct)
    {
        var roleSettingsResult = await _provider.Get(ct).ConfigureAwait(false);
        var roleSettings = roleSettingsResult.IsSuccess ? roleSettingsResult.Value! : (IReadOnlyList<RoleSettingImplementationConfiguration>)[];
        var existing = roleSettings
            .FirstOrDefault(s => s.TenantId == request.TenantId
                                 && string.Equals(s.RoleName, request.RoleName, StringComparison.OrdinalIgnoreCase)
                                 && string.Equals(s.SettingName, request.SettingName, StringComparison.OrdinalIgnoreCase));
        return GenericResult<bool>.Success(existing is not null);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult<RoleSettingSummaryDto>> Create(CreateRoleSettingRequest request, CancellationToken ct)
    {
        var config = new RoleSettingImplementationConfiguration
        {
            // Why: Save()'s name param is the domain-row lookup key -- an existing row carrying
            // it is the row a save hangs from, and only its absence mints a new one. Left unset
            // (defaults to ""), every create resolved the SAME single domain row regardless of
            // tenant, role or setting name, so a second create silently overwrote the first.
            // TenantId+RoleName+SettingName together identify a member, the same composite
            // UpdateRoleSettingEndpointBase.GetResourceIdentifier uses.
            Name = $"{request.TenantId}/{request.RoleName}/{request.SettingName}",
            TenantId = request.TenantId,
            RoleName = request.RoleName,
            SettingName = request.SettingName,
            SettingValue = request.SettingValue,
            IsActive = true
        };

        var saveResult = await _provider.Save(config, "RoleSetting", "RoleSetting", config.Name, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            return saveResult.ToNewResult<RoleSettingSummaryDto>();
        }

        SettingsEndpointLog.CreatedRoleSetting(Logger, request.SettingName, request.RoleName, request.TenantId.ToString());

        var dto = new RoleSettingSummaryDto
        {
            Id = config.Id,
            TenantId = config.TenantId,
            RoleName = config.RoleName,
            SettingName = config.SettingName,
            SettingValue = config.SettingValue,
            IsActive = config.IsActive
        };

        return GenericResult<RoleSettingSummaryDto>.Success(dto);
    }

    /// <inheritdoc />
    protected override Task SendCreatedResponse(RoleSettingSummaryDto detail, CancellationToken ct)
    {
        return Send.ResponseAsync(detail, 201, ct);
    }
}
