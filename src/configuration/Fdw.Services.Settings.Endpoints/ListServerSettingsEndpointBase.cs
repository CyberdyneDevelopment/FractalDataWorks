using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Configuration;
using Fdw.Services.Settings.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Base endpoint for listing all server-level settings.
/// </summary>
public abstract class ListServerSettingsEndpointBase : CrudListEndpointBase<ServerSettingSummaryDto>
{
    private readonly IServerSettingConfigurationProvider _provider;
    private readonly IDescriptionReader _reader;

    /// <summary>Initializes a new instance of the <see cref="ListServerSettingsEndpointBase"/> class.</summary>
    /// <param name="logger">Logger for this endpoint.</param>
    /// <param name="provider">Reads the server settings.</param>
    /// <param name="reader">Reads the descriptions of the settings listed, once for the answer (FDW-6208).</param>
    protected ListServerSettingsEndpointBase(ILogger<ListServerSettingsEndpointBase> logger, IServerSettingConfigurationProvider provider, IDescriptionReader reader) : base(logger)
    {
        _provider = provider;
        _reader = reader;
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

        var items = allResult.Value!
            .Select(s => new ServerSettingSummaryDto
            {
                Id = s.Id,
                SettingName = s.Name,
                SettingValue = s.SettingValue,
                DataType = s.DataType,
                IsActive = s.IsActive
            })
            .ToList();

        // The provider never fills a description on load; one read of the descriptions answers the whole list, and a read that fails fails the route (no text stands in).
        var filled = await _reader.Fill(ServerSettingDescriptionType.OptionName, DescriptionTextSubType.OptionName, items, item => item.Id, (item, text) => item.Description = text, ct).ConfigureAwait(false);
        if (filled.IsFailure)
        {
            return filled.ToNewResult<List<ServerSettingSummaryDto>>();
        }

        return GenericResult<List<ServerSettingSummaryDto>>.Success(items);
    }
}
