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
/// Base endpoint for listing all server-level settings.
/// </summary>
public abstract class ListServerSettingsEndpointBase : CrudListEndpointBase<ServerSettingSummaryDto>
{
    private readonly IServiceConfigurationProvider<ServerSettingConfiguration> _provider;

    /// <inheritdoc />
    protected ListServerSettingsEndpointBase(IServiceConfigurationProvider<ServerSettingConfiguration> provider)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "settings/server";

    /// <inheritdoc />
    protected override string EndpointSummary => "List server settings";

    /// <inheritdoc />
    protected override string EndpointDescription => "Returns all server-level setting definitions.";

    /// <inheritdoc />
    protected override async Task<IGenericResult<List<ServerSettingSummaryDto>>> LoadItems(CancellationToken ct)
    {
        SettingsEndpointLog.ListingServerSettings(Logger);

        var allResult = await _provider.Get(ct).ConfigureAwait(false);
        if (!allResult.IsSuccess)
        {
            return allResult.ToNewResult<List<ServerSettingSummaryDto>>();
        }

        var items = (allResult.Value ?? (IReadOnlyList<ServerSettingConfiguration>)[])
            .Select(s => new ServerSettingSummaryDto
            {
                Id = s.Id,
                SettingName = s.SettingName,
                SettingValue = s.SettingValue,
                DataType = s.DataType,
                IsActive = s.IsActive
            })
            .ToList();

        return GenericResult<List<ServerSettingSummaryDto>>.Success(items);
    }
}
