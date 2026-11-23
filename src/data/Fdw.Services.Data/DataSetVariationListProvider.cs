using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>Reads the list of data set variations from the <c>data.DataSetVariationList</c> view (FDW-3638).</summary>
/// <remarks>
/// The view joins a variation's data set row to its own row, so a lookup by owner, by the data set it varies, by its parent or by its name is
/// one domain-record query. The view is read only: its members are saved, read whole and retired through the data set provider, which the
/// variation provider calls. A variation's <c>Description</c> is the one of its data set, kept in the descriptions table, so the view does not carry it: the page
/// is filled from the descriptions in one read (FDW-6208).
/// </remarks>
public sealed class DataSetVariationListProvider
    : DomainConfigurationProviderBase<IDataSetVariationImplementationConfiguration, DataSetVariationDomainConfiguration>,
      IConfigurationDomainReadObserver<DataSetVariationDomainConfiguration>
{
    private const string DataSetType = "DataSet";

    private readonly IDescriptionReader _descriptions;

    /// <summary>Initializes a new instance of the <see cref="DataSetVariationListProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from.</param>
    /// <param name="pathName">The schema the view lives in -- passed by its registration.</param>
    /// <param name="containerName">The view -- passed by its registration.</param>
    /// <param name="descriptions">Reads the descriptions of the variations' data sets.</param>
    public DataSetVariationListProvider(
        ILogger<DataSetVariationListProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName,
        IDescriptionReader descriptions)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
        _descriptions = descriptions;
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Read(IReadOnlyList<DataSetVariationDomainConfiguration> records, CancellationToken cancellationToken = default)
    {
        var read = await _descriptions.ReadMany(DataSetType, DescriptionTextSubType.OptionName, [.. records.Select(record => record.Id)], cancellationToken).ConfigureAwait(false);
        if (read.IsFailure || read.Value is not { } texts) return read;

        foreach (var record in records)
        {
            if (texts.TryGetValue(record.Id, out var text)) record.Description = text.Text;
        }

        return GenericResult.Success();
    }
}
