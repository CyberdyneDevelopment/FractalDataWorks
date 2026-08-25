using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Settings.Configuration;
using Fdw.Services.Settings.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Base endpoint for listing tenant-level setting overrides.
/// </summary>
public abstract class ListTenantSettingsEndpointBase : CrudListEndpointBase<TenantSettingSummaryDto>
{
    private readonly IServiceConfigurationProvider<TenantSettingConfiguration> _provider;

    /// <inheritdoc />
    protected ListTenantSettingsEndpointBase(IServiceConfigurationProvider<TenantSettingConfiguration> provider)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "settings/tenant";

    /// <inheritdoc />
    protected override string EndpointSummary => "List tenant setting overrides";

    /// <inheritdoc />
    protected override string EndpointDescription => "Returns all tenant-level setting overrides.";

    /// <inheritdoc />
    protected override async Task<IGenericResult<List<TenantSettingSummaryDto>>> LoadItems(CancellationToken ct)
    {
        SettingsEndpointLog.ListingTenantSettings(Logger, "all");

        var allResult = await _provider.Get(ct).ConfigureAwait(false);
        if (!allResult.IsSuccess)
        {
            return allResult.ToNewResult<List<TenantSettingSummaryDto>>();
        }

        var items = (allResult.Value ?? (IReadOnlyList<TenantSettingConfiguration>)[])
            .Select(s => new TenantSettingSummaryDto
            {
                Id = s.Id,
                TenantId = s.TenantId,
                SettingName = s.SettingName,
                SettingValue = s.SettingValue,
                IsActive = s.IsActive
            })
            .ToList();

        return GenericResult<List<TenantSettingSummaryDto>>.Success(items);
    }
}
