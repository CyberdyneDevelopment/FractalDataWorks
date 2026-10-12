using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Workspaces.Logging;
using Fdw.Services.Workspaces.Results;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>The domain providers a map reads through, and the paging that streams a domain's members out of them.</summary>
/// <remarks>
/// Every read goes through the owning domain's provider: the rows come back as the domain's own current members, read
/// as the caller, so row-level security narrows the map to what the caller may see. A page composes one member at a
/// time as it is enumerated, so a response holds one composed member, not the estate.
/// </remarks>
internal sealed class MapSources
{
    /// <summary>How many members one page of a domain read holds.</summary>
    public const int PageSize = 200;

    /// <summary>Initializes the providers.</summary>
    /// <param name="dataSets">The data set domain.</param>
    /// <param name="dataStores">The data store domain.</param>
    /// <param name="pipelines">The pipeline domain.</param>
    /// <param name="variations">The data set variation domain.</param>
    public MapSources(
        IDataSetConfigurationProvider dataSets,
        DataStoreConfigurationProvider dataStores,
        IPipelineConfigurationProvider pipelines,
        IDataSetVariationConfigurationProvider variations)
    {
        DataSets = dataSets;
        DataStores = dataStores;
        Pipelines = pipelines;
        Variations = variations;
    }

    /// <summary>Gets the data set domain.</summary>
    public IDataSetConfigurationProvider DataSets { get; }

    /// <summary>Gets the data store domain.</summary>
    public DataStoreConfigurationProvider DataStores { get; }

    /// <summary>Gets the pipeline domain.</summary>
    public IPipelineConfigurationProvider Pipelines { get; }

    /// <summary>Gets the data set variation domain.</summary>
    public IDataSetVariationConfigurationProvider Variations { get; }

    /// <summary>Gets the data set domain table as a list: names and ids read from the domain row alone, no data set composed.</summary>
    /// <param name="logger">Records the refusal when the registered provider cannot list its domain table.</param>
    /// <returns>The domain list, or <see cref="MapSearchUnavailableCode"/> when the registered provider is not one that lists.</returns>
    public IGenericResult<IDomainRecordProvider<DomainConfiguration>> DataSetRecords(ILogger logger)
        => Records(DataSets as IDomainRecordProvider<DomainConfiguration>, MapKinds.DataSet, logger);

    /// <summary>Gets the pipeline domain table as a list: names and ids read from the domain row alone, no pipeline composed.</summary>
    /// <param name="logger">Records the refusal when the registered provider cannot list its domain table.</param>
    /// <returns>The domain list, or <see cref="MapSearchUnavailableCode"/> when the registered provider is not one that lists.</returns>
    public IGenericResult<IDomainRecordProvider<DomainConfiguration>> PipelineRecords(ILogger logger)
        => Records(Pipelines as IDomainRecordProvider<DomainConfiguration>, MapKinds.Pipeline, logger);

    /// <summary>Takes the member out of a read of one named or identified member.</summary>
    /// <typeparam name="T">The domain's implementation contract.</typeparam>
    /// <param name="read">The result of the read.</param>
    /// <param name="kind">The kind of the object that was asked for.</param>
    /// <param name="reference">The name or id it was asked for by.</param>
    /// <param name="namedBy">What named it.</param>
    /// <param name="state">Receives a failed read, or counts a reference to nothing.</param>
    /// <param name="logger">Records the reference that named nothing.</param>
    /// <returns>The member, or nothing when the read failed (recorded on <paramref name="state"/> as the outcome) or the object is not there (counted as unresolved).</returns>
    /// <remarks>
    /// A domain answers a read of a member that does not exist with success and no value: a real answer, not an error.
    /// Something that names an object no longer there, such as a pipeline naming a deleted data set, is left off the map and
    /// counted, not guessed at and not allowed to fail the whole map.
    /// </remarks>
    public static T? Resolve<T>(
        IGenericResult<T> read, string kind, string reference, string namedBy, MapState state, ILogger logger)
        where T : class
    {
        if (read.IsFailure)
        {
            state.Fail(read);
            return null;
        }

        if (read.Value is { } member) return member;

        state.NoteUnresolved();
        WorkspaceMapLog.ReferenceUnresolved(logger, kind, reference, namedBy);
        return null;
    }

    /// <summary>Takes the domain list out of a provider, or refuses when the registered provider does not list.</summary>
    /// <param name="records">The registered provider as a lister, or nothing when it is not one.</param>
    /// <param name="kind">The kind of the domain.</param>
    /// <param name="logger">Records the refusal.</param>
    /// <returns>The domain list, or <see cref="MapSearchUnavailableCode"/>.</returns>
    public static IGenericResult<IDomainRecordProvider<DomainConfiguration>> Records(
        IDomainRecordProvider<DomainConfiguration>? records, string kind, ILogger logger)
        => records is null
            ? GenericResult<IDomainRecordProvider<DomainConfiguration>>.Failure(
                WorkspacesResultCodes.ByName("MapSearchUnavailable"), logger, ResultDetails.Create("kind", kind))
            : GenericResult<IDomainRecordProvider<DomainConfiguration>>.Success(records);

    /// <summary>Streams every member of a domain, a page at a time.</summary>
    /// <typeparam name="T">The domain's implementation contract.</typeparam>
    /// <param name="read">Reads the page at a skip and take.</param>
    /// <param name="state">Receives the failure when a page cannot be read or a member cannot be composed.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <remarks>A failure ends the stream and is recorded on <paramref name="state"/>: what was sent before it stands, and the response says it ended early.</remarks>
    public static async IAsyncEnumerable<T> Pages<T>(
        Func<int, int, CancellationToken, Task<IGenericResult<ConfigurationPage<T>>>> read,
        MapState state,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var skip = 0;
        while (true)
        {
            var page = await read(skip, PageSize, ct).ConfigureAwait(false);
            if (page.IsFailure)
            {
                state.Fail(page);
                yield break;
            }

            await foreach (var member in page.Value!.Items.WithCancellation(ct).ConfigureAwait(false))
                yield return member;

            if (page.Value.Outcome.IsFailure)
            {
                state.Fail(page.Value.Outcome);
                yield break;
            }

            if (!page.Value.HasMore) yield break;
            skip += PageSize;
        }
    }
}
