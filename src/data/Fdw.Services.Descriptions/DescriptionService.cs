using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Configuration;
using Fdw.Services.Configuration.Logging;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Descriptions.Configuration;
using Fdw.Services.Descriptions.Logging;
using Fdw.Services.Descriptions.Results;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Descriptions;

/// <summary>
/// The descriptions service. A description is one or more parts: each a domain row (<c>catalog.Description</c>) and its text (<c>catalog.DescriptionConfiguration</c>),
/// written together through the description provider, so version-on-write and cache invalidation are the provider's. A text within the part length is one part; a longer
/// one is split (<see cref="DescriptionParts"/>) and every read joins the parts back, so no caller sees one. Nothing here is defaulted: a missing type, sub-type, thing,
/// language or text is a failed result that names it, and a read of a language that has no row is not found.
/// </summary>
public sealed class DescriptionService : IDescriptionService, IDescriptionWriter, IDescriptionReader
{
    // The members of one thing are a handful (a sub-type in a language each, and each in a few parts), so a read of them takes the whole page rather than a limit
    // that would cut a thing's descriptions short; the same marker TagSelection uses for every row.
    private const int EveryRow = int.MaxValue - 1;

    private const string DomainName = "Description";
    private const string CatalogPath = "catalog";

    private readonly IDescriptionConfigurationProvider _descriptions;
    private readonly IEffectiveSettingsProvider _settings;
    private readonly IAuthenticationContextAccessor _authentication;
    private readonly IConfigurationGatewayProvider _gateways;
    private readonly int _partLength;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="DescriptionService"/> class.</summary>
    /// <param name="descriptions">Reads and writes the descriptions.</param>
    /// <param name="settings">Where the language of today's descriptions is read.</param>
    /// <param name="gateways">The configuration gateway, whose key chunk size says how many keys one read names.</param>
    /// <param name="authentication">The calling principal, whose tenant's own language applies when it has one. A singleton accessor, so the service is one too and a provider that holds it is not captive.</param>
    /// <param name="partLength">The most characters one row holds: <see cref="DescriptionServiceTypes.PartLength"/>, which registration refuses below 1. A longer text is saved as parts.</param>
    /// <param name="logger">The logger.</param>
    public DescriptionService(
        IDescriptionConfigurationProvider descriptions,
        IEffectiveSettingsProvider settings,
        IConfigurationGatewayProvider gateways,
        IAuthenticationContextAccessor authentication,
        int partLength,
        ILogger<DescriptionService>? logger = null)
    {
        if (partLength < 1)
            throw new ArgumentOutOfRangeException(nameof(partLength), partLength, "A part holds at least one character");

        _descriptions = descriptions;
        _settings = settings;
        _gateways = gateways;
        _authentication = authentication;
        _partLength = partLength;
        _logger = logger ?? NullLogger<DescriptionService>.Instance;
    }

    /// <inheritdoc />
    public IGenericResult<string> Language()
        => _settings.TryGetEffectiveValue<string>(SettingDefinitions.DescriptionLanguage, out var language, _authentication.Current?.ActiveTenantId) && !string.IsNullOrWhiteSpace(language)
            ? GenericResult<string>.Success(language)
            : GenericResult<string>.Failure(
                DescriptionsResultCodes.ByName("DescriptionLanguageNotStated"), _logger, ResultDetails.Create("setting", SettingDefinitions.DescriptionLanguage));

    /// <inheritdoc />
    public async Task<IGenericResult> Save(
        DescriptionKey key, string thingName, DescriptionOwner? owner, string text, CancellationToken cancellationToken = default)
    {
        var prepared = Prepare(key, thingName, owner, text);
        if (prepared.IsFailure || prepared.Value is not { } parts) return prepared;

        // All the parts land together or none do: a text of three parts is never half written.
        return await _descriptions.InUnit(unit => SaveParts(key, parts, unit, cancellationToken), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Save(
        DescriptionKey key, string thingName, DescriptionOwner? owner, string text, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
    {
        var prepared = Prepare(key, thingName, owner, text);
        if (prepared.IsFailure || prepared.Value is not { } parts) return prepared;

        return await SaveParts(key, parts, unit, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Delete(DescriptionKey key, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
    {
        var valid = Check(key);
        if (valid.IsFailure) return valid;

        return await RemoveRows(PartsOf(key), keepThingIds: [], unit, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Describe(
        string type, string subType, Guid thingId, string thingName, string? text, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
    {
        var language = Language();
        if (language.IsFailure || language.Value is not { } tag) return language;

        var key = new DescriptionKey(type, subType, thingId, tag);
        return string.IsNullOrWhiteSpace(text)
            ? await Delete(key, unit, cancellationToken).ConfigureAwait(false)
            : await Save(key, thingName, null, text, unit, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> DeleteAbout(string type, Guid thingId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
    {
        var valid = CheckThing(type, thingId);
        if (valid.IsFailure) return valid;

        return await RemoveRows(
            [Equal("DescriptionType", type), Equal("ThingId", thingId)], keepThingIds: [], unit, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> DeleteOwnedBy(string type, Guid ownerId, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
    {
        var valid = CheckThing(type, ownerId);
        if (valid.IsFailure) return valid;

        return await RemoveRows(
            [Equal("DescriptionType", type), Equal("OwnerId", ownerId)], keepThingIds: [], unit, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> DeleteOwnedExcept(
        string type, string subType, string language, Guid ownerId, IReadOnlyCollection<Guid> keepThingIds, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
    {
        var valid = Check(new DescriptionKey(type, subType, ownerId, language));
        if (valid.IsFailure) return valid;

        return await RemoveRows(
            [Equal("DescriptionType", type), Equal("DescriptionSubType", subType), Equal("Language", language), Equal("OwnerId", ownerId)],
            keepThingIds, unit, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> DescribeOwned(
        string type, string subType, DescriptionOwner owner, IReadOnlyList<OwnedDescription> things, ConfigurationWriteScope unit, CancellationToken cancellationToken = default)
    {
        var language = Language();
        if (language.IsFailure || language.Value is not { } tag) return language;

        var valid = Check(new DescriptionKey(type, subType, owner.Id, tag));
        if (valid.IsFailure) return valid;

        // One read of every part of every description the owner holds of this type, on the unit; what is written and what is removed is decided from it.
        var read = await _descriptions.ReadCurrent(
            Current([
                Equal(nameof(DescriptionDomainConfiguration.DescriptionType), type),
                Equal(nameof(DescriptionDomainConfiguration.DescriptionSubType), subType),
                Equal(nameof(DescriptionDomainConfiguration.Language), tag),
                Equal(nameof(DescriptionDomainConfiguration.OwnerId), owner.Id),
            ]),
            unit,
            cancellationToken).ConfigureAwait(false);
        if (read.IsFailure || read.Value is not { } rows) return read;

        var kept = new HashSet<Guid>();
        var written = await WriteOwned(new DescriptionKey(type, subType, owner.Id, tag), owner, things, rows, kept, unit, cancellationToken).ConfigureAwait(false);
        if (written.IsFailure) return written;

        var removed = await RemoveNotKept(rows, kept, unit, cancellationToken).ConfigureAwait(false);
        if (removed.IsFailure) return removed;

        DescriptionsLog.DescribedOwned(_logger, type, owner.Id, things.Count, written.Value.Written, written.Value.Removed + removed.Value);
        return GenericResult.Success();
    }

    // Writes each held thing whose text, name or owner name changed, from the rows already read, and records in kept every thing that still has a text. The key
    // (the owner's) names the type, subtype and language; each thing is written under its own Id. Answers how many things it wrote and how many surplus parts it removed.
    private async Task<IGenericResult<(int Written, int Removed)>> WriteOwned(
        DescriptionKey owned, DescriptionOwner owner, IReadOnlyList<OwnedDescription> things, IReadOnlyList<DescriptionDomainConfiguration> rows, HashSet<Guid> kept,
        ConfigurationWriteScope unit, CancellationToken cancellationToken)
    {
        var held = rows.GroupBy(row => row.ThingId).ToDictionary(group => group.Key, group => group.ToList());
        var written = 0;
        var removed = 0;
        foreach (var thing in things)
        {
            if (string.IsNullOrWhiteSpace(thing.Text) || !kept.Add(thing.ThingId)) continue;

            List<DescriptionDomainConfiguration> parts = held.TryGetValue(thing.ThingId, out var found) ? found : [];
            if (parts.Count > 0 && Unchanged(parts, thing, owner)) continue;

            var prepared = Prepare(new DescriptionKey(owned.Type, owned.SubType, thing.ThingId, owned.Language), thing.ThingName, owner, thing.Text);
            if (prepared.IsFailure || prepared.Value is not { } toSave) return prepared.ToNewResult<(int, int)>();

            var saved = await SaveOwnedParts(toSave, parts, unit, cancellationToken).ConfigureAwait(false);
            if (saved.IsFailure) return saved.ToNewResult<(int, int)>();

            written++;
            removed += saved.Value;
        }

        return GenericResult<(int Written, int Removed)>.Success((written, removed));
    }

    // Removes every row read for a thing the owner no longer holds or no longer describes. Answers how many it removed.
    private async Task<IGenericResult<int>> RemoveNotKept(
        IReadOnlyList<DescriptionDomainConfiguration> rows, HashSet<Guid> kept, ConfigurationWriteScope unit, CancellationToken cancellationToken)
    {
        var removed = 0;
        foreach (var row in rows.Where(row => !kept.Contains(row.ThingId)))
        {
            var deleted = await _descriptions.Delete(row.Id, unit, cancellationToken).ConfigureAwait(false);
            if (deleted.IsFailure) return deleted.ToNewResult<int>();

            removed++;
            DescriptionsLog.Deleted(_logger, row.Name);
        }

        return GenericResult<int>.Success(removed);
    }

    // A thing whose stored parts already say its text, under its name and its owner's name, is left as it is: nothing is written for it.
    private static bool Unchanged(List<DescriptionDomainConfiguration> parts, OwnedDescription thing, DescriptionOwner owner)
        => DescriptionParts.AreContiguous(parts.Select(part => part.Ordinal))
           && string.Equals(DescriptionParts.Join(parts.Select(part => (part.Ordinal, part.Text))), thing.Text, StringComparison.Ordinal)
           && string.Equals(parts[0].ThingName, thing.ThingName, StringComparison.Ordinal)
           && string.Equals(parts[0].OwnerName, owner.Name, StringComparison.Ordinal);

    // Writes the parts of one owned description and removes the parts it held above them, from the parts already read: no second read for each thing. Answers how
    // many parts it removed.
    private async Task<IGenericResult<int>> SaveOwnedParts(
        IReadOnlyList<IDescriptionImplementationConfiguration> parts, List<DescriptionDomainConfiguration> held, ConfigurationWriteScope unit, CancellationToken cancellationToken)
    {
        foreach (var part in parts)
        {
            var saved = await _descriptions.Save<IDescriptionImplementationConfiguration>(part, DomainName, DomainName, part.Name, unit, cancellationToken).ConfigureAwait(false);
            if (saved.IsFailure) return saved.ToNewResult<int>();
        }

        var removed = 0;
        foreach (var stale in held.Where(row => row.Ordinal > parts.Count))
        {
            var deleted = await _descriptions.Delete(stale.Id, unit, cancellationToken).ConfigureAwait(false);
            if (deleted.IsFailure) return deleted.ToNewResult<int>();

            removed++;
        }

        DescriptionsLog.Saved(_logger, parts[0].Name);
        return GenericResult<int>.Success(removed);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Delete(DescriptionKey key, CancellationToken cancellationToken = default)
    {
        var valid = Check(key);
        if (valid.IsFailure) return valid;

        var deleted = await _descriptions.InUnit(unit => DeleteWhole(key, unit, cancellationToken), cancellationToken).ConfigureAwait(false);
        if (deleted.IsSuccess) DescriptionsLog.Deleted(_logger, key.AsName());
        return deleted;
    }

    // The public delete names a description that must be there: one that holds no part is refused with the provider's own coded message, as on the 1.5 line, where the delete of a
    // row that is not there fails. Only a delete a caller asks for is refused so; the removal inside a save (the parts above the last it wrote) and the unit overload of Delete
    // (Describe with no text) have nothing to remove in their normal case and pass.
    private async Task<IGenericResult> DeleteWhole(DescriptionKey key, ConfigurationWriteScope unit, CancellationToken cancellationToken)
    {
        var removed = await RemoveRows(PartsOf(key), keepThingIds: [], unit, cancellationToken).ConfigureAwait(false);
        if (removed.IsFailure) return removed;

        return removed.Value == 0
            ? GenericResult.Failure(DefaultConfigurationProviderLog.DeleteTargetNotFound(_logger, nameof(IDescriptionImplementationConfiguration), key.AsName()))
            : GenericResult.Success();
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>> ReadMany(
        string type, string subType, IReadOnlyCollection<Guid> thingIds, CancellationToken cancellationToken = default)
        => ReadSet(type, subType, thingIds, nameof(DescriptionDomainConfiguration.ThingId), cancellationToken);

    /// <inheritdoc />
    public async Task<IGenericResult> Fill<T>(
        string type, string subType, IReadOnlyList<T> things, Func<T, Guid> idOf, Action<T, string> fill, CancellationToken cancellationToken = default)
    {
        var read = await ReadMany(type, subType, [.. things.Select(idOf)], cancellationToken).ConfigureAwait(false);
        if (read.IsFailure || read.Value is not { } texts) return read;

        foreach (var thing in things)
        {
            if (texts.TryGetValue(idOf(thing), out var entry)) fill(thing, entry.Text);
        }

        return GenericResult.Success();
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>> ReadOwnedBy(
        string type, string subType, IReadOnlyCollection<Guid> ownerIds, CancellationToken cancellationToken = default)
        => ReadSet(type, subType, ownerIds, nameof(DescriptionDomainConfiguration.OwnerId), cancellationToken);

    // The descriptions of a set, in the language of today: one query for each chunk of keys, answered from the domain rows alone, because the text is a column
    // of the domain row (FDW-6123). A query returns EVERY part of EVERY description asked for, and the parts are joined here, so a page of 580 data sets is still one
    // query (the chunks of the gateway's key chunk size are the only reason there is ever more than one), never one for a description or for a part.
    private async Task<IGenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>> ReadSet(
        string type, string subType, IReadOnlyCollection<Guid> keys, string keyColumn, CancellationToken cancellationToken)
    {
        var typeFound = CheckType(type);
        if (typeFound.IsFailure) return typeFound.ToNewResult<IReadOnlyDictionary<Guid, DescriptionEntry>>();

        if (string.IsNullOrWhiteSpace(subType) || ReferenceEquals(DescriptionSubTypes.ByName(subType), DescriptionSubTypes.NotFound))
            return GenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>.Failure(
                DescriptionsResultCodes.ByName("DescriptionSubTypeUnknown"), _logger, ResultDetails.Create("subType", subType));

        var language = Language();
        if (language.IsFailure) return language.ToNewResult<IReadOnlyDictionary<Guid, DescriptionEntry>>();

        var found = new Dictionary<Guid, DescriptionEntry>();
        if (keys.Count == 0) return GenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>.Success(found);

        DescriptionsLog.ReadingMany(_logger, type, keys.Count);
        foreach (var chunk in keys.Distinct().Chunk(_gateways.KeyChunkSize))
        {
            var rows = await RowsOf(new DomainQuery(ManyOf(type, subType, language.Value!, keyColumn, chunk), null, 0, EveryRow, false), cancellationToken).ConfigureAwait(false);
            if (rows.IsFailure || rows.Value is not { } read) return rows.ToNewResult<IReadOnlyDictionary<Guid, DescriptionEntry>>();

            foreach (var description in read.GroupBy(row => row.ThingId))
            {
                var entry = EntryOf([.. description]);
                if (entry.IsFailure || entry.Value is null) return entry.ToNewResult<IReadOnlyDictionary<Guid, DescriptionEntry>>();

                found[description.Key] = entry.Value;
            }
        }

        return GenericResult<IReadOnlyDictionary<Guid, DescriptionEntry>>.Success(found);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyCollection<Guid>>> Find(string type, string subType, string text, CancellationToken cancellationToken = default)
    {
        var typeFound = CheckType(type);
        if (typeFound.IsFailure) return typeFound.ToNewResult<IReadOnlyCollection<Guid>>();

        if (string.IsNullOrWhiteSpace(subType) || ReferenceEquals(DescriptionSubTypes.ByName(subType), DescriptionSubTypes.NotFound))
            return GenericResult<IReadOnlyCollection<Guid>>.Failure(
                DescriptionsResultCodes.ByName("DescriptionSubTypeUnknown"), _logger, ResultDetails.Create("subType", subType));

        var language = Language();
        if (language.IsFailure) return language.ToNewResult<IReadOnlyCollection<Guid>>();

        if (string.IsNullOrWhiteSpace(text)) return GenericResult<IReadOnlyCollection<Guid>>.Success(new List<Guid>());

        // Any part may hold the text, so a thing is named once whatever number of its parts do. A phrase that crosses the end of one part and the start of the next is in no part: it is not found.
        var rows = await RowsOf(new DomainQuery(Containing(type, subType, language.Value!, text), null, 0, EveryRow, false), cancellationToken).ConfigureAwait(false);
        if (rows.IsFailure || rows.Value is not { } read) return rows.ToNewResult<IReadOnlyCollection<Guid>>();

        return GenericResult<IReadOnlyCollection<Guid>>.Success([.. read.Select(row => row.ThingId).Distinct()]);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<DescriptionVersion>>> Versions(string type, string subType, Guid thingId, CancellationToken cancellationToken = default)
    {
        var typeFound = CheckType(type);
        if (typeFound.IsFailure) return typeFound.ToNewResult<IReadOnlyList<DescriptionVersion>>();

        if (string.IsNullOrWhiteSpace(subType) || ReferenceEquals(DescriptionSubTypes.ByName(subType), DescriptionSubTypes.NotFound))
            return GenericResult<IReadOnlyList<DescriptionVersion>>.Failure(
                DescriptionsResultCodes.ByName("DescriptionSubTypeUnknown"), _logger, ResultDetails.Create("subType", subType));

        var language = Language();
        if (language.IsFailure) return language.ToNewResult<IReadOnlyList<DescriptionVersion>>();

        // Every version of every part of the description, retired and deleted ones too: the one read that does not filter to the current rows.
        var rows = await new ConfigurationRowReader(_gateways, DescriptionServiceTypes.ConfigurationConnection, CatalogPath, DomainName, _logger)
            .Page(Of(type, subType, thingId, language.Value!), ["RowId"], 0, EveryRow, false, cancellationToken)
            .ConfigureAwait(false);
        if (rows.IsFailure) return rows.ToNewResult<IReadOnlyList<DescriptionVersion>>();

        try
        {
            return GenericResult<IReadOnlyList<DescriptionVersion>>.Success(DescriptionParts.Versions(rows.Value!
                .Select(row => (
                    Ordinal: (int)(row.Number(nameof(DescriptionDomainConfiguration.Ordinal)) ?? throw new InvalidOperationException($"A version of {DomainName} has no value in its column 'Ordinal'")),
                    Text: row.Text("Text") ?? string.Empty,
                    At: row.Moment("ModifyDate") ?? throw new InvalidOperationException($"A version of {DomainName} has no value in its column 'ModifyDate'"),
                    Deleted: row.Flag("IsDeleted") ?? throw new InvalidOperationException($"A version of {DomainName} has no value in its column 'IsDeleted'")))
                .ToList()));
        }
        catch (InvalidOperationException exception)
        {
            return GenericResult<IReadOnlyList<DescriptionVersion>>.Failure(Fdw.Services.Configuration.Logging.ConfigurationRowReaderLog.ColumnMissing(_logger, DomainName, exception.Message));
        }
    }

    // Writes the parts, then removes the parts the description had above them: fewer parts than before leaves no stale higher Ordinal behind.
    private async Task<IGenericResult> SaveParts(
        DescriptionKey key, IReadOnlyList<IDescriptionImplementationConfiguration> parts, ConfigurationWriteScope unit, CancellationToken cancellationToken)
    {
        foreach (var part in parts)
        {
            var saved = await _descriptions.Save<IDescriptionImplementationConfiguration>(part, DomainName, DomainName, part.Name, unit, cancellationToken).ConfigureAwait(false);
            if (saved.IsFailure) return saved;
        }

        var removed = await RemoveRows([.. PartsOf(key), Above("Ordinal", parts.Count)], keepThingIds: [], unit, cancellationToken).ConfigureAwait(false);
        if (removed.IsFailure) return removed;

        DescriptionsLog.SavedAsParts(_logger, key.AsName(), parts.Count, removed.Value);
        DescriptionsLog.Saved(_logger, key.AsName());
        return GenericResult.Success();
    }

    // The rows of a query, whole, or the failure that stopped the read.
    private async Task<IGenericResult<List<DescriptionDomainConfiguration>>> RowsOf(DomainQuery query, CancellationToken cancellationToken)
    {
        var page = await _descriptions.Get(query, cancellationToken).ConfigureAwait(false);
        if (page.IsFailure || page.Value is null) return page.ToNewResult<List<DescriptionDomainConfiguration>>();

        var rows = new List<DescriptionDomainConfiguration>();
        await foreach (var row in page.Value.Items.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            rows.Add(row);
        }

        return page.Value.Outcome.IsFailure
            ? page.Value.Outcome.ToNewResult<List<DescriptionDomainConfiguration>>()
            : GenericResult<List<DescriptionDomainConfiguration>>.Success(rows);
    }

    // The parts of ONE description, joined in Ordinal order into the text that was written; the parts must be 1 to n with no gap, or the text is not the one written.
    private IGenericResult<DescriptionEntry> EntryOf(IReadOnlyList<DescriptionDomainConfiguration> parts)
    {
        var first = parts.OrderBy(part => part.Ordinal).First();
        if (!DescriptionParts.AreContiguous(parts.Select(part => part.Ordinal)))
        {
            return GenericResult<DescriptionEntry>.Failure(
                DescriptionsResultCodes.ByName("DescriptionPartsBroken"), _logger,
                ResultDetails.Create("type", first.DescriptionType, "thingId", first.ThingId.ToString("D"), "language", first.Language)
                    .With("ordinals", string.Join(",", parts.Select(part => part.Ordinal).OrderBy(ordinal => ordinal))));
        }

        return GenericResult<DescriptionEntry>.Success(new DescriptionEntry(
            new DescriptionKey(first.DescriptionType, first.DescriptionSubType, first.ThingId, first.Language),
            first.ThingName,
            OwnerOf(first.OwnerId, first.OwnerName),
            DescriptionParts.Join(parts.Select(part => (part.Ordinal, part.Text)))));
    }

    private static FilterExpression Containing(string type, string subType, string language, string text)
        => new()
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.And,
                Nodes =
                [
                    Equal(nameof(DescriptionDomainConfiguration.DescriptionType), type),
                    Equal(nameof(DescriptionDomainConfiguration.DescriptionSubType), subType),
                    Equal(nameof(DescriptionDomainConfiguration.Language), language),
                    new FilterCondition { PropertyName = nameof(DescriptionDomainConfiguration.Text), Operator = FilterOperators.ByName("Contains"), Value = text },
                ]
            }
        };

    private static FilterExpression ManyOf(string type, string subType, string language, string keyColumn, Guid[] keys)
        => new()
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.And,
                Nodes =
                [
                    Equal(nameof(DescriptionDomainConfiguration.DescriptionType), type),
                    Equal(nameof(DescriptionDomainConfiguration.DescriptionSubType), subType),
                    Equal(nameof(DescriptionDomainConfiguration.Language), language),
                    new FilterCondition { PropertyName = keyColumn, Operator = FilterOperators.ByName("In"), Value = keys.Cast<object?>().ToList() },
                ]
            }
        };

    // The text, the names and the key written as the implementation records the provider saves (one for each part), or the refusal that stops it before anything is written.
    private IGenericResult<IReadOnlyList<IDescriptionImplementationConfiguration>> Prepare(DescriptionKey key, string thingName, DescriptionOwner? owner, string text)
    {
        var valid = Check(key);
        if (valid.IsFailure) return valid.ToNewResult<IReadOnlyList<IDescriptionImplementationConfiguration>>();

        var named = CheckNames(key, thingName, owner);
        if (named.IsFailure) return named.ToNewResult<IReadOnlyList<IDescriptionImplementationConfiguration>>();

        if (string.IsNullOrWhiteSpace(text))
        {
            return GenericResult<IReadOnlyList<IDescriptionImplementationConfiguration>>.Failure(
                DescriptionsResultCodes.ByName("DescriptionTextMissing"), _logger,
                ResultDetails.Create("type", key.Type, "thingId", key.ThingId.ToString("D"), "language", key.Language));
        }

        DescriptionsLog.Saving(_logger, key.AsName());
        var texts = DescriptionParts.Split(text, _partLength);
        var parts = new List<IDescriptionImplementationConfiguration>(texts.Count);
        for (var i = 0; i < texts.Count; i++)
        {
            parts.Add(new DescriptionImplementationConfiguration
            {
                Name = key.AsName(i + 1),
                DescriptionType = key.Type,
                DescriptionSubType = key.SubType,
                ThingId = key.ThingId,
                Language = key.Language,
                Ordinal = i + 1,
                ThingName = thingName,
                OwnerName = owner?.Name,
                OwnerId = owner?.Id,
                Text = texts[i],
            });
        }

        return GenericResult<IReadOnlyList<IDescriptionImplementationConfiguration>>.Success(parts);
    }

    // The current descriptions a filter selects are read on the unit, and each is deleted in it, except the things named. Answers how many rows it removed.
    private async Task<IGenericResult<int>> RemoveRows(
        IReadOnlyList<FilterCondition> conditions, IReadOnlyCollection<Guid> keepThingIds, ConfigurationWriteScope unit, CancellationToken cancellationToken)
    {
        var read = await _descriptions.ReadCurrent(Current(conditions), unit, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure || read.Value is not { } rows) return read.ToNewResult<int>();

        var removed = 0;
        foreach (var row in rows)
        {
            if (keepThingIds.Contains(row.ThingId)) continue;

            var deleted = await _descriptions.Delete(row.Id, unit, cancellationToken).ConfigureAwait(false);
            if (deleted.IsFailure) return deleted.ToNewResult<int>();

            removed++;
            DescriptionsLog.Deleted(_logger, row.Name);
        }

        return GenericResult<int>.Success(removed);
    }

    private static FilterCondition Equal(string property, object value)
        => new() { PropertyName = property, Operator = FilterOperators.ByName("Equal"), Value = value };

    private static FilterCondition Above(string property, int value)
        => new() { PropertyName = property, Operator = FilterOperators.ByName("GreaterThan"), Value = value };

    // The four parts of a key: every part of one description.
    private static List<FilterCondition> PartsOf(DescriptionKey key)
        =>
        [
            Equal(nameof(DescriptionDomainConfiguration.DescriptionType), key.Type),
            Equal(nameof(DescriptionDomainConfiguration.DescriptionSubType), key.SubType),
            Equal(nameof(DescriptionDomainConfiguration.ThingId), key.ThingId),
            Equal(nameof(DescriptionDomainConfiguration.Language), key.Language),
        ];

    private static FilterGroup Of(string type, string subType, Guid thingId, string language)
        => new()
        {
            Operator = LogicalOperator.And,
            Nodes =
            [
                Equal(nameof(DescriptionDomainConfiguration.DescriptionType), type),
                Equal(nameof(DescriptionDomainConfiguration.DescriptionSubType), subType),
                Equal(nameof(DescriptionDomainConfiguration.ThingId), thingId),
                Equal(nameof(DescriptionDomainConfiguration.Language), language),
            ],
        };

    private static FilterExpression Current(IReadOnlyList<FilterCondition> conditions)
        => new()
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.And,
                Nodes =
                [
                    .. conditions,
                    Equal("IsCurrent", true),
                    Equal("IsDeleted", false),
                ]
            }
        };

    // The four parts of a key, each refused by name before anything is read or written.
    private IGenericResult Check(DescriptionKey key)
    {
        var thing = CheckThing(key.Type, key.ThingId);
        if (thing.IsFailure) return thing;

        if (string.IsNullOrWhiteSpace(key.SubType) || ReferenceEquals(DescriptionSubTypes.ByName(key.SubType), DescriptionSubTypes.NotFound))
            return GenericResult.Failure(DescriptionsResultCodes.ByName("DescriptionSubTypeUnknown"), _logger, ResultDetails.Create("subType", key.SubType));

        return string.IsNullOrWhiteSpace(key.Language)
            ? GenericResult.Failure(
                DescriptionsResultCodes.ByName("DescriptionLanguageMissing"), _logger, ResultDetails.Create("type", key.Type, "thingId", key.ThingId.ToString("D")))
            : GenericResult.Success();
    }

    // The names a search shows: the thing's always, and the owner's exactly when the type has one.
    private IGenericResult CheckNames(DescriptionKey key, string thingName, DescriptionOwner? owner)
    {
        if (string.IsNullOrWhiteSpace(thingName))
            return GenericResult.Failure(
                DescriptionsResultCodes.ByName("DescriptionThingNameMissing"), _logger,
                ResultDetails.Create("type", key.Type, "thingId", key.ThingId.ToString("D")));

        var hasOwner = DescriptionTypes.ByName(key.Type).HasOwner;
        if (hasOwner && (owner is null || owner.Id == Guid.Empty || string.IsNullOrWhiteSpace(owner.Name)))
            return GenericResult.Failure(
                DescriptionsResultCodes.ByName("DescriptionOwnerMissing"), _logger,
                ResultDetails.Create("type", key.Type, "thingId", key.ThingId.ToString("D")));

        return !hasOwner && owner is not null
            ? GenericResult.Failure(
                DescriptionsResultCodes.ByName("DescriptionOwnerNotExpected"), _logger,
                ResultDetails.Create("type", key.Type, "thingId", key.ThingId.ToString("D"), "ownerName", owner.Name))
            : GenericResult.Success();
    }

    // A row that names an owner has both its Id and its name; one that names neither has no owner.
    private static DescriptionOwner? OwnerOf(Guid? ownerId, string? ownerName)
        => ownerId is { } id && ownerName is not null ? new DescriptionOwner(id, ownerName) : null;

    private IGenericResult CheckType(string type)
        => string.IsNullOrWhiteSpace(type) || ReferenceEquals(DescriptionTypes.ByName(type), DescriptionTypes.NotFound)
            ? GenericResult.Failure(DescriptionsResultCodes.ByName("DescriptionTypeUnknown"), _logger, ResultDetails.Create("type", type))
            : GenericResult.Success();

    private IGenericResult CheckThing(string type, Guid thingId)
    {
        var known = CheckType(type);
        if (known.IsFailure) return known;

        return thingId == Guid.Empty
            ? GenericResult.Failure(DescriptionsResultCodes.ByName("DescriptionThingMissing"), _logger, ResultDetails.Create("type", type))
            : GenericResult.Success();
    }
}
