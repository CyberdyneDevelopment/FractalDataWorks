using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Configuration;
using Fdw.Conventions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Commands;
using Fdw.Services.Data.Logging;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>Supplies the DataSet configuration.</summary>
public sealed class DataSetConfigurationProvider
    : DomainConfigurationProviderBase<IDataSetImplementationConfiguration, DataSetDomainConfiguration>,
      IDataSetConfigurationProvider,
      IDataSetRecordProvider,
      IPlacedConfigurationProvider<IDataSetImplementationConfiguration>,
      IConfigurationWriteObserver<IDataSetImplementationConfiguration>,
      IConfigurationReadObserver<IDataSetImplementationConfiguration>,
      IConfigurationChildReadObserver<IDataSetImplementationConfiguration>,
      IReferencedConfigurationProvider<IDataSetImplementationConfiguration>
{
    private const string DataSetType = "DataSet";
    private const string FieldType = "DataSetField";
    private const string AggregateType = DataSetAggregateDescriptionType.OptionName;
    private const string JoinType = DataSetJoinDescriptionType.OptionName;
    private const string SampleType = DataSetSampleDescriptionType.OptionName;
    private const string DescriptionSubType = DescriptionTextSubType.OptionName;

    private readonly IDescriptionWriter _descriptions;
    private readonly IDescriptionReader _reader;

    /// <summary>Initializes a new instance of the <see cref="DataSetConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    /// <param name="placement">Places each data set this provider makes, in the unit of work that makes it (FDW-3692).</param>
    /// <param name="descriptions">Keeps the descriptions table in step with the <c>Description</c> of each data set and field, in the unit of work of every write (FDW-6121).</param>
    /// <param name="reader">Reads the descriptions of the data sets and fields a read has just composed, once for the set, to fill their <c>Description</c> (FDW-6208).</param>
    public DataSetConfigurationProvider(
        ILogger<DataSetConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName,
        IConfigurationPlacement placement,
        IDescriptionWriter descriptions,
        IDescriptionReader reader)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
        Placement = placement;
        _descriptions = descriptions;
        _reader = reader;
    }

    /// <inheritdoc />
    public IConfigurationPlacement Placement { get; }

    /// <inheritdoc />
    /// <remarks>
    /// The page of <see cref="DataSetDomainConfiguration"/> is the same rows, so a caller of the plain interface gets them as the plain record.
    /// Why this exists: the map, the lookup endpoints and the variation provider read data sets as <c>IDomainRecordProvider&lt;DomainConfiguration&gt;</c>, and one
    /// of them casts to it with <c>as</c>. A provider that implemented only <see cref="IDataSetRecordProvider"/> would make that cast fail, and the caller would
    /// see a silent null where it expects a provider.
    /// </remarks>
    async Task<IGenericResult<ConfigurationPage<DomainConfiguration>>> IDomainRecordProvider<DomainConfiguration>.Get(DomainQuery query, CancellationToken ct)
    {
        var page = await Get(query, ct).ConfigureAwait(false);
        return page.IsFailure || page.Value is not { } read
            ? page.ToNewResult<ConfigurationPage<DomainConfiguration>>()
            : GenericResult<ConfigurationPage<DomainConfiguration>>.Success(
                new ConfigurationPage<DomainConfiguration>(read.Skip, read.Take, read.HasMore, report => Plain(read, report, ct), read.Total));
    }

    /// <inheritdoc />
    async Task<IGenericResult<IReadOnlyList<DomainConfiguration>>> IDomainRecordProvider<DomainConfiguration>.Get(IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        var records = await Get(ids, ct).ConfigureAwait(false);
        return records.IsFailure || records.Value is not { } found
            ? records.ToNewResult<IReadOnlyList<DomainConfiguration>>()
            : GenericResult<IReadOnlyList<DomainConfiguration>>.Success(found);
    }

    // The rows of the page, as the plain record they are; a failure the page meets while it is read is reported on the plain page too.
    private static async IAsyncEnumerable<DomainConfiguration> Plain(
        ConfigurationPage<DataSetDomainConfiguration> read, Action<IGenericResult> report, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var record in read.Items.WithCancellation(ct).ConfigureAwait(false))
        {
            yield return record;
        }

        if (read.Outcome.IsFailure)
        {
            report(read.Outcome);
        }
    }

    /// <inheritdoc />
    public string PlacementKind => "DataSet";

    /// <inheritdoc />
    /// <remarks>
    /// A variation is a data set whose own row names its owner (a workspace for a scoped variation, a user for a data view), and the workspace read
    /// already lists the variations a workspace owns, so a placement row would list it twice or give a data view a workspace it does not have.
    /// Every other data set is placed.
    /// </remarks>
    public bool Needs(IDataSetImplementationConfiguration record) => record is not IDataSetVariationImplementationConfiguration;

    /// <inheritdoc />
    /// <remarks>
    /// The description of the data set and of each field, in the language the setting states, is written in the unit of the write of the data set, so the
    /// table and the column are never apart (FDW-6121). Text that is missing writes nothing and removes what the thing had; a field the saved list no longer
    /// holds loses its description. A field is described by the Id it was saved under, and its owner is the data set. The fields are kept in step with one
    /// <see cref="IDescriptionWriter.DescribeOwned"/> (FDW-6589): one read of what the data set holds, and a write only for a field whose text or name changed.
    /// </remarks>
    public async Task<IGenericResult> Written(IDataSetImplementationConfiguration record, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
    {
        var language = _descriptions.Language();
        if (language.IsFailure || language.Value is not { } tag) return language;

        var described = await Describe(
            new DescriptionKey(DataSetType, DescriptionSubType, record.Id, tag), record.Name, null, record.Description, unit, cancellationToken).ConfigureAwait(false);
        if (described.IsFailure) return described;

        // Why the owner and not only the field's Id: the descriptions of a data set's fields are found, replaced and removed by the data set's Id, so a field the
        // saved list no longer holds loses its description in the same unit. Since FDW-6200 a field keeps its Id across an update, so a field whose text did not
        // change is not written again.
        var owner = new DescriptionOwner(record.Id, record.Name);
        var fields = await _descriptions.DescribeOwned(
            FieldType, DescriptionSubType, owner,
            [.. record.Fields.Select(field => new OwnedDescription(field.Id, field.Name, field.Description))], unit, cancellationToken).ConfigureAwait(false);
        if (fields.IsFailure) return fields;

        // The aggregates and joins of the data set are its children in the same way (FDW-6208): each is described by the Id the save gave it, owned by the data set, so a
        // save that holds fewer of them removes the descriptions of the ones it dropped. A kind that has none writes nothing and removes what an earlier kind left.
        var aggregates = await _descriptions.DescribeOwned(
            AggregateType, DescriptionSubType, owner,
            [.. record.AggregatesOrNone().Select(aggregate => new OwnedDescription(aggregate.Id, aggregate.AggregateColumnName, aggregate.Description))], unit, cancellationToken).ConfigureAwait(false);
        if (aggregates.IsFailure) return aggregates;

        return await _descriptions.DescribeOwned(
            JoinType, DescriptionSubType, owner,
            [.. record.JoinsOrNone().Select(join => new OwnedDescription(join.Id, JoinName(join), join.Description))], unit, cancellationToken).ConfigureAwait(false);
    }

    // A join has no name of its own, so the thing a description names is the join itself: what it matches on which sides, how.
    private static string JoinName(DataSetJoinConfiguration join)
        => $"{join.LeftSourceName}.{join.LeftFieldName} {join.JoinType} {join.RightSourceName}.{join.RightFieldName}";

    /// <inheritdoc />
    /// <remarks>
    /// The <c>Description</c> of each data set and field is not stored in the data set's own tables: it is read from the descriptions table, once for the whole
    /// set read (FDW-6208), in the language the setting states. A thing with no description row keeps an empty description; nothing stands in for it. A kind that keeps
    /// its fields apart (a Graph data set) is edited through <see cref="IKeepsItsOwnFields"/>, because its shared field list is a copy.
    /// The texts of the fields are read only for the data sets whose records hold fields: a record-only read, as a list page makes, carries none, and a read of
    /// field texts for it would fetch rows that nothing receives (FDW-6124).
    /// </remarks>
    public async Task<IGenericResult> Read(IReadOnlyList<IDataSetImplementationConfiguration> records, DateTimeOffset? asOf, CancellationToken cancellationToken = default)
    {
        var dataSets = await _reader.ReadMany(DataSetType, DescriptionSubType, [.. records.Select(record => record.Id)], cancellationToken).ConfigureAwait(false);
        if (dataSets.IsFailure || dataSets.Value is not { } dataSetTexts) return dataSets;

        List<(IDataSetImplementationConfiguration Record, IKeepsItsOwnFields? Keeper)> holders = [.. records.Select(record => (record, KeeperOf(record)))];
        var fieldTexts = await FieldTexts([.. holders.Where(holder => HeldFields(holder.Record, holder.Keeper).Count > 0).Select(holder => holder.Record.Id)], cancellationToken).ConfigureAwait(false);
        if (fieldTexts.IsFailure || fieldTexts.Value is not { } fieldsRead) return fieldTexts;

        foreach ((IDataSetImplementationConfiguration record, IKeepsItsOwnFields? keeper) in holders)
        {
            if (dataSetTexts.TryGetValue(record.Id, out var own)) record.Description = own.Text;

            var held = HeldFields(record, keeper);
            foreach (var field in held)
            {
                if (fieldsRead.TryGetValue(field.Id, out var text)) field.Description = text.Text;
            }

            keeper?.KeepFields(record, [.. held]);
        }

        var aggregates = await FillOwned(AggregateType, records, record => record.AggregatesOrNone(), aggregate => aggregate.Id, (aggregate, text) => aggregate.Description = text, cancellationToken).ConfigureAwait(false);
        return aggregates.IsFailure
            ? aggregates
            : await FillOwned(JoinType, records, record => record.JoinsOrNone(), join => join.Id, (join, text) => join.Description = text, cancellationToken).ConfigureAwait(false);
    }

    // The texts of one kind of child of the data sets read, in one read of the descriptions the data sets own; no read at all when none of them holds one (FDW-6124's rule).
    private async Task<IGenericResult> FillOwned<TChild>(
        string type, IReadOnlyList<IDataSetImplementationConfiguration> records, Func<IDataSetImplementationConfiguration, IReadOnlyList<TChild>> childrenOf,
        Func<TChild, Guid> idOf, Action<TChild, string> fill, CancellationToken cancellationToken)
    {
        var owners = records.Where(record => childrenOf(record).Count > 0).Select(record => record.Id).ToList();
        if (owners.Count == 0) return GenericResult.Success();

        var texts = await _reader.ReadOwnedBy(type, DescriptionSubType, owners, cancellationToken).ConfigureAwait(false);
        if (texts.IsFailure || texts.Value is not { } held) return texts;

        foreach (var child in records.SelectMany(childrenOf))
        {
            if (held.TryGetValue(idOf(child), out var text)) fill(child, text.Text);
        }

        return GenericResult.Success();
    }

    // The kind that keeps its fields apart, when the record's kind is one.
    private static IKeepsItsOwnFields? KeeperOf(IDataSetImplementationConfiguration record) => DataSetTypes.ByName(record.Implementation) as IKeepsItsOwnFields;

    // The fields a record holds: its own list, or for a kind that keeps its fields apart, the list that kind keeps.
    private static IReadOnlyList<DataSetFieldConfiguration> HeldFields(IDataSetImplementationConfiguration record, IKeepsItsOwnFields? keeper)
        => keeper is null ? [.. record.Fields] : keeper.FieldsOf(record);

    // The texts of the fields of the data sets named, in one read; no read at all when none of them holds a field.
    private async Task<IGenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>> FieldTexts(IReadOnlyCollection<Guid> ownerIds, CancellationToken cancellationToken)
        => ownerIds.Count == 0
            ? GenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>.Success(new Dictionary<Guid, DescriptionEntry>())
            : await _reader.ReadOwnedBy(FieldType, DescriptionSubType, ownerIds, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    /// <remarks>
    /// The field list reads the shared field table as a child read (<c>Get&lt;DataSetFieldConfiguration&gt;</c>), which composes no data set and so reaches no
    /// <see cref="IConfigurationReadObserver{TImplementationConfiguration}"/> (FDW-6777). Its fields are filled here with the
    /// same read, the descriptions the data set owns, by each field's Id; an aggregate or join read the same way is filled the same way (FDW-6208). Children of any other type are left as read.
    /// </remarks>
    public async Task<IGenericResult> ReadChildren<TChild>(Guid memberId, IReadOnlyList<TChild> children, CancellationToken cancellationToken = default)
    {
        var fields = await FillChildren(FieldType, memberId, [.. children.OfType<DataSetFieldConfiguration>()], field => field.Id, (field, text) => field.Description = text, cancellationToken).ConfigureAwait(false);
        if (fields.IsFailure) return fields;

        var aggregates = await FillChildren(AggregateType, memberId, [.. children.OfType<DataSetAggregateConfiguration>()], aggregate => aggregate.Id, (aggregate, text) => aggregate.Description = text, cancellationToken).ConfigureAwait(false);
        return aggregates.IsFailure
            ? aggregates
            : await FillChildren(JoinType, memberId, [.. children.OfType<DataSetJoinConfiguration>()], join => join.Id, (join, text) => join.Description = text, cancellationToken).ConfigureAwait(false);
    }

    // The children of one type that a child read returned, filled with one read of the descriptions the data set owns; nothing is read when it returned none.
    private async Task<IGenericResult> FillChildren<TChild>(
        string type, Guid memberId, IReadOnlyList<TChild> children, Func<TChild, Guid> idOf, Action<TChild, string> fill, CancellationToken cancellationToken)
    {
        if (children.Count == 0) return GenericResult.Success();

        var texts = await _reader.ReadOwnedBy(type, DescriptionSubType, [memberId], cancellationToken).ConfigureAwait(false);
        if (texts.IsFailure || texts.Value is not { } held) return texts;

        foreach (var child in children)
        {
            if (held.TryGetValue(idOf(child), out var text)) fill(child, text.Text);
        }

        return GenericResult.Success();
    }

    /// <summary>Keeps the description of one sample of a data set in step with the sample the sample route has just written (FDW-6208).</summary>
    /// <remarks>
    /// A sample is replaced as a whole under a new Id each time it is written, so the description of the sample it replaced is removed and the new text is saved, in one unit.
    /// The sample's rows are written before this, by the sample writer, which has no unit of its own: a failure here leaves the sample without its text and is reported, so the
    /// caller writes the sample again. A sample saved with no text keeps none.
    /// </remarks>
    /// <param name="dataSetId">The logical Id of the data set that owns the sample.</param>
    /// <param name="dataSetName">The name of the data set.</param>
    /// <param name="sampleId">The Id the sample was just written under.</param>
    /// <param name="sampleName">The sample's name.</param>
    /// <param name="text">The text the request carries; empty or blank saves none.</param>
    /// <param name="replacedSampleId">The Id of the sample this one replaced, whose description goes; null when it replaced none.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success, or the reason the language was not stated, a part of the key was refused or the write failed.</returns>
    public async Task<IGenericResult> DescribeSample(
        Guid dataSetId, string dataSetName, Guid sampleId, string sampleName, string? text, Guid? replacedSampleId, CancellationToken cancellationToken = default)
    {
        var language = _descriptions.Language();
        if (language.IsFailure || language.Value is not { } tag) return language;

        var begun = await BeginWriteScope(cancellationToken).ConfigureAwait(false);
        if (begun.IsFailure || begun.Value is not { } unit) return begun;

        await using (unit.ConfigureAwait(false))
        {
            var described = await DescribeSampleIn(dataSetId, dataSetName, sampleId, sampleName, text, replacedSampleId, tag, unit, cancellationToken).ConfigureAwait(false);
            if (described.IsFailure)
            {
                await unit.Rollback(cancellationToken).ConfigureAwait(false);
                return described;
            }

            return await unit.Commit(cancellationToken).ConfigureAwait(false);
        }
    }

    // The replaced sample's description goes, then the new sample's text is saved; both in the one unit. Internal so a test passes the writer a unit it never touches.
    internal async Task<IGenericResult> DescribeSampleIn(
        Guid dataSetId, string dataSetName, Guid sampleId, string sampleName, string? text, Guid? replacedSampleId, string language, ConfigurationWriteScope unit, CancellationToken cancellationToken)
    {
        if (replacedSampleId is { } replaced)
        {
            var removed = await _descriptions.Delete(new DescriptionKey(SampleType, DescriptionSubType, replaced, language), unit, cancellationToken).ConfigureAwait(false);
            if (removed.IsFailure) return removed;
        }

        return await Describe(
            new DescriptionKey(SampleType, DescriptionSubType, sampleId, language), sampleName, new DescriptionOwner(dataSetId, dataSetName), text, unit, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reads the descriptions of the samples of one data set, in the language of today, for the route that lists them (FDW-6208).</summary>
    /// <param name="dataSetId">The logical Id of the data set.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The text of each sample that has one, by the sample's Id; a sample with none has no entry. Or the reason the read failed.</returns>
    public async Task<IGenericResult<IReadOnlyDictionary<Guid, string>>> SampleDescriptions(Guid dataSetId, CancellationToken cancellationToken = default)
    {
        var read = await _reader.ReadOwnedBy(SampleType, DescriptionSubType, [dataSetId], cancellationToken).ConfigureAwait(false);
        return read.IsFailure || read.Value is not { } entries
            ? read.ToNewResult<IReadOnlyDictionary<Guid, string>>()
            : GenericResult<IReadOnlyDictionary<Guid, string>>.Success(entries.ToDictionary(entry => entry.Key, entry => entry.Value.Text));
    }

    /// <summary>Reads every stored version of the description of a data set field, oldest first, for the field's history.</summary>
    /// <param name="fieldId">The logical Id of the field.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The versions, or the reason the read failed.</returns>
    public Task<IGenericResult<IReadOnlyList<DescriptionVersion>>> FieldDescriptionVersions(Guid fieldId, CancellationToken cancellationToken = default)
        => _reader.Versions(FieldType, DescriptionSubType, fieldId, cancellationToken);

    /// <inheritdoc />
    /// <remarks>The descriptions of a data set and of its fields, aggregates, joins and samples name it, so they go with it, in every language, before it is retired (FDW-5222, FDW-6121, FDW-6208).</remarks>
    public async Task<IGenericResult> RemoveReferences(Guid memberId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
    {
        var about = await _descriptions.DeleteAbout(DataSetType, memberId, unit, cancellationToken).ConfigureAwait(false);
        if (about.IsFailure) return about;

        var fields = await _descriptions.DeleteOwnedBy(FieldType, memberId, unit, cancellationToken).ConfigureAwait(false);
        if (fields.IsFailure) return fields;

        var aggregates = await _descriptions.DeleteOwnedBy(AggregateType, memberId, unit, cancellationToken).ConfigureAwait(false);
        if (aggregates.IsFailure) return aggregates;

        var joins = await _descriptions.DeleteOwnedBy(JoinType, memberId, unit, cancellationToken).ConfigureAwait(false);
        return joins.IsFailure
            ? joins
            : await _descriptions.DeleteOwnedBy(DataSetSampleDescriptionType.OptionName, memberId, unit, cancellationToken).ConfigureAwait(false);
    }

    // A text is saved, and no text removes the description the thing had: no description is invented and none is left behind.
    private Task<IGenericResult> Describe(
        DescriptionKey key, string thingName, DescriptionOwner? owner, string? text, ConfigurationWriteScope unit, CancellationToken cancellationToken)
        => string.IsNullOrWhiteSpace(text)
            ? _descriptions.Delete(key, unit, cancellationToken)
            : _descriptions.Save(key, thingName, owner, text, unit, cancellationToken);
}
