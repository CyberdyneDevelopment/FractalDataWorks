using Fdw.Services.Tags.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Tags;
using Fdw.Services.Tags.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>
/// The tags an annotation shows and accepts are the tags of the data set it names (FDW-3763): the annotation holds no tag of its own, and the
/// data set is found by the annotation's <c>DataSetName</c>.
/// </summary>
/// <remarks>
/// An annotation names its data set by name, so the data set is looked up by name; one that is not there fails with a not-found, never an
/// empty list. A text with no tag yet becomes a tenant tag of the caller's tenant when it is put on the data set, as the old free-text tags
/// were made by being written; a tag the request leaves out is taken off. Case is not significant, as it is not for tag names.
/// </remarks>
internal static class DataSetAnnotationTags
{
    private const string DataSetKind = "DataSet";

    /// <summary>Reads the tag names of the data set an annotation names.</summary>
    /// <param name="tags">The Tags service.</param>
    /// <param name="dataSets">Finds the data set by name.</param>
    /// <param name="dataSetName">The annotation's data set name.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The tag names; a failure when the data set is not there or the tags cannot be read.</returns>
    internal static async Task<IGenericResult<IList<string>>> Read(
        ITagService tags, IDataSetConfigurationProvider dataSets, string dataSetName, ILogger logger, CancellationToken cancellationToken)
    {
        var dataSetId = await Find(dataSets, dataSetName, logger, cancellationToken).ConfigureAwait(false);
        if (dataSetId.IsFailure)
        {
            return dataSetId.ToNewResult<IList<string>>();
        }

        var names = await tags.TagsOf(DataSetKind, dataSetId.Value, cancellationToken).ConfigureAwait(false);
        return names.IsFailure || names.Value is null
            ? names.ToNewResult<IList<string>>()
            : GenericResult<IList<string>>.Success([.. names.Value]);
    }

    /// <summary>Makes the tags of the data set an annotation names equal to a list.</summary>
    /// <param name="tags">The Tags service.</param>
    /// <param name="dataSets">Finds the data set by name.</param>
    /// <param name="authentication">The caller's authentication context: the tenant a new tag is made in.</param>
    /// <param name="dataSetName">The annotation's data set name.</param>
    /// <param name="wanted">The tag texts the request carries; none takes every tag off.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success; the first failure otherwise. The steps already made stay made, each is safe to repeat.</returns>
    internal static async Task<IGenericResult> Replace(
        ITagService tags,
        IDataSetConfigurationProvider dataSets,
        IAuthenticationContextAccessor authentication,
        string dataSetName,
        IEnumerable<string>? wanted,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (authentication.Current is not { ActiveTenantId: { } tenantId })
        {
            return GenericResult.Failure(DataSetAnnotationLog.TagsCallerHasNoTenant(logger, dataSetName));
        }

        var dataSetId = await Find(dataSets, dataSetName, logger, cancellationToken).ConfigureAwait(false);
        if (dataSetId.IsFailure)
        {
            return dataSetId;
        }

        var current = await tags.TagsOf(DataSetKind, dataSetId.Value, cancellationToken).ConfigureAwait(false);
        if (current.IsFailure || current.Value is null)
        {
            return current;
        }

        var texts = (wanted ?? []).Select(text => text.Trim()).Where(text => text.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var name in current.Value.Where(name => !texts.Contains(name, StringComparer.OrdinalIgnoreCase)))
        {
            var removed = await tags.Unlink(DataSetKind, dataSetId.Value, name, cancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return removed;
            }
        }

        foreach (var text in texts.Where(text => !current.Value.Contains(text, StringComparer.OrdinalIgnoreCase)))
        {
            var put = await Put(tags, tenantId, dataSetId.Value, text, cancellationToken).ConfigureAwait(false);
            if (put.IsFailure)
            {
                return put;
            }
        }

        return GenericResult.Success();
    }

    // Puts a tag on the data set, making it first as a tenant tag when the tenant has none of that name.
    private static async Task<IGenericResult> Put(ITagService tags, Guid tenantId, Guid dataSetId, string name, CancellationToken cancellationToken)
    {
        var linked = await tags.Link(DataSetKind, dataSetId, name, cancellationToken).ConfigureAwait(false);
        if (linked.IsSuccess || linked.Code is not TagNotFoundCode)
        {
            return linked;
        }

        var made = await tags.Create(name, TagOwnerKinds.Tenant.Name, tenantId, null, cancellationToken).ConfigureAwait(false);
        return made.IsFailure
            ? made
            : await tags.Link(DataSetKind, dataSetId, name, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IGenericResult<Guid>> Find(IDataSetConfigurationProvider dataSets, string dataSetName, ILogger logger, CancellationToken cancellationToken)
    {
        var read = await dataSets.Get(dataSetName, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure)
        {
            return read.ToNewResult<Guid>();
        }

        return read.Value is { } dataSet
            ? GenericResult<Guid>.Success(dataSet.Id)
            : GenericResult<Guid>.Failure(DataSetAnnotationLog.TagsDataSetNotFound(logger, dataSetName));
    }
}
