using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Reports how much of a dataverse is backed by real data.</summary>
/// <remarks>
/// Every number here is counted from stored field bindings. Nothing is estimated and nothing is
/// defaulted: a data set the provider cannot return is reported as blocking rather than silently
/// counted as complete or skipped.
/// </remarks>
public abstract class GetDataverseReadinessEndpointBase : CrudGetEndpointBase<DataverseNameRequest, DataverseReadinessResponse>
{
    private readonly IDataverseConfigurationProvider _dataverses;
    private readonly IDataSetConfigurationProvider _dataSets;

    /// <inheritdoc />
    protected GetDataverseReadinessEndpointBase(
        ILogger<GetDataverseReadinessEndpointBase> logger,
        IDataverseConfigurationProvider dataverses,
        IDataSetConfigurationProvider dataSets) : base(logger)
    {
        _dataverses = dataverses;
        _dataSets = dataSets;
    }

    /// <summary>Gets the resource name used for policy generation.</summary>
    protected override string ResourceName => "dataverses";

    /// <inheritdoc />
    protected override string Route => "/dataverses/{Name}/readiness";

    /// <inheritdoc />
    protected override string EndpointSummary => "Get a dataverse's readiness";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Counts the dataverse's data sets by how much of each is bound to real data.";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(DataverseNameRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataverseReadinessResponse?>> FindByIdentifier(
        DataverseNameRequest request, CancellationToken ct)
    {
        var dataverse = await _dataverses.Get(request.Name, ct).ConfigureAwait(false);
        if (dataverse.IsFailure) return dataverse.ToNewResult<DataverseReadinessResponse?>();
        if (dataverse.Value is null) return GenericResult<DataverseReadinessResponse?>.Success(null);

        var response = new DataverseReadinessResponse();
        var blocked = new List<string>();
        var anyFederated = false;

        foreach (var attachment in dataverse.Value.Resources.Where(IsDataSet))
        {
            var dataSet = await _dataSets.Get(attachment.ResourceId, ct).ConfigureAwait(false);

            // Why a resource we cannot read counts as blocking rather than being skipped: skipping
            // it would raise readiness by removing the unknown from the denominator, which reports
            // a more complete dataverse than anybody can justify.
            if (dataSet.IsFailure || dataSet.Value is null)
            {
                blocked.Add(attachment.Name);
                response.Proposed++;
                continue;
            }

            anyFederated |= string.Equals(dataSet.Value.ServiceOptionType, "Federated", StringComparison.Ordinal);

            // A calculated field has no source BY DESIGN -- it is not missing a binding, it does not
            // have one. Counting it here would leave every data set containing a calculation
            // permanently short of Bound.
            var bindable = dataSet.Value.Fields.Where(f => !f.IsCalculated).ToList();
            var bound = bindable.Count(f => f.BoundContainerFieldId.HasValue);

            response.FieldsDefined += bindable.Count;
            response.FieldsBound += bound;

            switch (Classify(bindable, bound))
            {
                case "Bound": response.Bound++; break;
                case "Partial": response.Partial++; blocked.Add(dataSet.Value.Name); break;
                case "Sketched": response.Sketched++; blocked.Add(dataSet.Value.Name); break;
                default: response.Proposed++; blocked.Add(dataSet.Value.Name); break;
            }
        }

        response.BlockedBy = blocked;

        // Stand-ins are what make a sketched set answerable, so a dataverse with data sets can be
        // queried before any of them is bound. Materialization is the stricter question: it needs
        // every set actually backed, and a federated set can never be materialized at all.
        response.Queryable = response.Bound + response.Partial + response.Sketched + response.Proposed > 0;
        response.Materializable = blocked.Count == 0 && !anyFederated && response.Bound > 0;

        return GenericResult<DataverseReadinessResponse?>.Success(response);
    }

    private static bool IsDataSet(DataverseResourceConfiguration resource) =>
        string.Equals(resource.ResourceType, "DataSet", StringComparison.Ordinal);

    /// <summary>Places one data set on the binding scale.</summary>
    /// <param name="bindable">The fields that could be bound — calculated fields already excluded.</param>
    /// <param name="bound">How many of them are.</param>
    private static string Classify(List<DataSetFieldConfiguration> bindable, int bound)
    {
        // No bindable field carrying a type means the set has been named and nothing more. TypeName
        // is checked with IsNullOrEmpty rather than for null because the configuration type still
        // defaults it to string.Empty even though the column is nullable -- see FDW-730.
        if (bindable.Count == 0 || bindable.All(f => string.IsNullOrEmpty(f.TypeName)))
            return "Proposed";

        if (bound == 0) return "Sketched";
        return bound == bindable.Count ? "Bound" : "Partial";
    }
}
