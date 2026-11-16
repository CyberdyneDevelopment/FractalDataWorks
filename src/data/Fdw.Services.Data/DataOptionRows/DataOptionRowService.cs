using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.Options;
using Fdw.Data.Options.Logging;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Abstractions.DataOptionRows;
using Fdw.Services.Data.Logging;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.DataOptionRows;

/// <summary>
/// The option rows of a data store container (FDW-5664), held in the one options table the server settings
/// <see cref="SettingDefinitions.DataOptionsDataStore"/>, <see cref="SettingDefinitions.DataOptionsPath"/> and
/// <see cref="SettingDefinitions.DataOptionsContainer"/> name, read and written through the data gateway.
/// </summary>
/// <remarks>
/// Every request first resolves the three settings for the caller's tenant; one not set is a refusal naming it, and nothing is
/// assumed. Every read and write is filtered by the request's data store and container, so a row of another container is never
/// seen, changed or removed. An add and a remove check the whole request before anything is written.
/// </remarks>
public sealed class DataOptionRowService : IDataOptionRows
{
    private readonly ILogger<DataOptionRowService> _logger;
    private readonly IDataGatewayProvider _gateways;
    private readonly IDataStoreProvider _stores;
    private readonly IEffectiveSettingsProvider _settings;
    private readonly ITenantContext _tenant;

    /// <summary>Initializes a new instance of the <see cref="DataOptionRowService"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="gateways">Supplies the gateway every read and write is sent through.</param>
    /// <param name="stores">Builds a container, whose discovered fields a row's column is checked against.</param>
    /// <param name="settings">Where the options table is named.</param>
    /// <param name="tenant">The caller's tenant, whose own settings apply when it has them.</param>
    public DataOptionRowService(ILogger<DataOptionRowService>? logger, IDataGatewayProvider gateways, IDataStoreProvider stores, IEffectiveSettingsProvider settings, ITenantContext tenant)
    {
        _logger = logger ?? NullLogger<DataOptionRowService>.Instance;
        _gateways = gateways;
        _stores = stores;
        _settings = settings;
        _tenant = tenant;
    }

    private IDataGateway Gateway => _gateways.ByName(DataGatewayNames.Main);

    /// <inheritdoc />
    public async Task<IGenericResult<DataOptionRowsPage>> List(DataOptionRowsQuery query, CancellationToken cancellationToken = default)
    {
        var table = OptionsTable();
        if (table.IsFailure || table.Value is null)
            return table.ToNewResult<DataOptionRowsPage>();

        DataOptionRowsLog.Listing(_logger, query.Target.Container, query.Target.DataStore, query.Column ?? string.Empty, query.Skip, query.Take);

        List<IFilterNode> conditions = [.. Owned(query.Target)];
        if (!string.IsNullOrEmpty(query.Column))
            conditions.Add(Equal(DataOptionRowFields.Column, query.Column));

        var read = await Read(table.Value, conditions, new PagingExpression { Skip = query.Skip, Take = query.Take + 1 }, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure || read.Value is null)
            return read.ToNewResult<DataOptionRowsPage>();

        var hasMore = read.Value.Count > query.Take;
        DataOptionRowsLog.Listed(_logger, Math.Min(read.Value.Count, query.Take), query.Target.Container, hasMore);
        return GenericResult<DataOptionRowsPage>.Success(new DataOptionRowsPage { Items = [.. read.Value.Take(query.Take)], HasMore = hasMore });
    }

    /// <inheritdoc />
    public async Task<IGenericResult<DataOptionRow?>> Get(DataOptionRowKey key, CancellationToken cancellationToken = default)
    {
        var table = OptionsTable();
        if (table.IsFailure || table.Value is null)
            return table.ToNewResult<DataOptionRow?>();

        var read = await Read(table.Value, [.. Owned(key.Target), Equal(DataOptionRowFields.Id, key.Id)], null, cancellationToken).ConfigureAwait(false);
        return read.IsFailure || read.Value is null
            ? read.ToNewResult<DataOptionRow?>()
            : GenericResult<DataOptionRow?>.Success(read.Value.FirstOrDefault());
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<DataOptionRow>>> Add(DataOptionRowsAddition addition, CancellationToken cancellationToken = default)
    {
        var table = OptionsTable();
        if (table.IsFailure || table.Value is null)
            return table.ToNewResult<IReadOnlyList<DataOptionRow>>();

        var target = addition.Target;
        if (addition.Rows.Count == 0)
            return GenericResult<IReadOnlyList<DataOptionRow>>.Failure(DataOptionRowsLog.NoRowsToAdd(_logger, target.Container));

        var fields = await DiscoveredFields(target, cancellationToken).ConfigureAwait(false);
        if (fields.IsFailure || fields.Value is null)
            return fields.ToNewResult<IReadOnlyList<DataOptionRow>>();

        var stated = Stated(addition, fields.Value);
        if (stated.IsFailure || stated.Value is null)
            return stated.ToNewResult<IReadOnlyList<DataOptionRow>>();

        var stored = await Duplicates(table.Value, target, stated.Value, cancellationToken).ConfigureAwait(false);
        if (stored is not null)
            return stored.ToNewResult<IReadOnlyList<DataOptionRow>>();

        var written = await Gateway.Execute<int>(
            new InsertCommand<Dictionary<string, object?>>(stated.Value.Select(Record).ToList()), table.Value, cancellationToken).ConfigureAwait(false);
        if (written.IsFailure)
        {
            DataOptionRowsLog.OptionsTableRefusedWrite(_logger, "add", table.Value.Container, table.Value.DataStore);
            return written.ToNewResult<IReadOnlyList<DataOptionRow>>();
        }

        DataOptionRowsLog.Added(_logger, stated.Value.Count, target.Container, target.DataStore);
        return GenericResult<IReadOnlyList<DataOptionRow>>.Success(stated.Value);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<DataOptionRow>> Change(DataOptionRowChange change, CancellationToken cancellationToken = default)
    {
        var key = change.Key;
        if (ChangeRefusal(change) is { } refused)
            return GenericResult<DataOptionRow>.Failure(refused);

        var table = OptionsTable();
        if (table.IsFailure || table.Value is null)
            return table.ToNewResult<DataOptionRow>();

        var found = await Get(key, cancellationToken).ConfigureAwait(false);
        if (found.IsFailure)
            return found.ToNewResult<DataOptionRow>();

        if (found.Value is not { } existing)
            return GenericResult<DataOptionRow>.Failure(DataOptionRowsLog.RowNotFound(_logger, key.Id, key.Target.Container));

        var changed = Changed(existing, change);
        if (changed.IsFailure || changed.Value is null)
            return changed;

        if (change.Value is not null && !string.Equals(change.Value, existing.Value, StringComparison.Ordinal)
            && await Duplicates(table.Value, key.Target, [changed.Value], cancellationToken).ConfigureAwait(false) is { } stored)
            return stored.ToNewResult<DataOptionRow>();

        return await Write(table.Value, change, changed.Value, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<int>> Remove(DataOptionRowsRemoval removal, CancellationToken cancellationToken = default)
    {
        var target = removal.Target;
        if (removal.Ids.Count == 0)
            return GenericResult<int>.Failure(DataOptionRowsLog.NoIdsToRemove(_logger, target.Container));

        var table = OptionsTable();
        if (table.IsFailure || table.Value is null)
            return table.ToNewResult<int>();

        var ids = removal.Ids.Distinct().ToList();
        var idCondition = new FilterCondition { PropertyName = DataOptionRowFields.Id, Operator = FilterOperators.ByName("In"), Value = ids.Cast<object?>().ToList() };
        var held = await Read(table.Value, [.. Owned(target), idCondition], null, cancellationToken).ConfigureAwait(false);
        if (held.IsFailure || held.Value is null)
            return held.ToNewResult<int>();

        var missing = ids.Except(held.Value.Select(row => row.Id)).ToList();
        if (missing.Count > 0)
            return GenericResult<int>.Failure(DataOptionRowsLog.IdsNotInContainer(_logger, string.Join(", ", missing), target.Container));

        var removed = await Gateway.Execute<int>(new DeleteCommand { Filter = And([.. Owned(target), idCondition]) }, table.Value, cancellationToken).ConfigureAwait(false);
        if (removed.IsFailure)
        {
            DataOptionRowsLog.OptionsTableRefusedWrite(_logger, "remove", table.Value.Container, table.Value.DataStore);
            return removed;
        }

        DataOptionRowsLog.Removed(_logger, removed.Value, target.Container);
        return removed;
    }

    private IGenericMessage? ChangeRefusal(DataOptionRowChange change)
    {
        if (change.Option is null && change.Active is null && change.Value is null)
            return DataOptionRowsLog.NothingToChange(_logger, change.Key.Id);

        return change.Value is not null && string.IsNullOrWhiteSpace(change.Value) ? DataOptionRowsLog.NewValueEmpty(_logger, change.Key.Id) : null;
    }

    // One update of the fields the change states, of that row of that container; no row updated means it is not there now.
    private async Task<IGenericResult<DataOptionRow>> Write(DataStoreTarget table, DataOptionRowChange change, DataOptionRow changed, CancellationToken cancellationToken)
    {
        var key = change.Key;
        var updated = await Gateway.Execute<int>(
            new UpdateCommand<Dictionary<string, object?>>(Stated(change, changed)) { Filter = And([.. Owned(key.Target), Equal(DataOptionRowFields.Id, key.Id)]) },
            table,
            cancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            DataOptionRowsLog.OptionsTableRefusedWrite(_logger, "change", table.Container, table.DataStore);
            return updated.ToNewResult<DataOptionRow>();
        }

        if (updated.Value == 0)
            return GenericResult<DataOptionRow>.Failure(DataOptionRowsLog.RowNotFound(_logger, key.Id, key.Target.Container));

        DataOptionRowsLog.Changed(_logger, key.Id, key.Target.Container);
        return GenericResult<DataOptionRow>.Success(changed);
    }

    // The options table the three settings name, as they resolve for the caller's tenant; the first one not set is the refusal.
    private IGenericResult<DataStoreTarget> OptionsTable()
    {
        string[] names = [SettingDefinitions.DataOptionsDataStore, SettingDefinitions.DataOptionsPath, SettingDefinitions.DataOptionsContainer];
        var values = new List<string>();
        foreach (var name in names)
        {
            if (!_settings.TryGetEffectiveValue<string>(name, out var value, _tenant.TenantId) || string.IsNullOrWhiteSpace(value))
                return GenericResult<DataStoreTarget>.Failure(DataOptionRowsLog.OptionsTableNotNamed(_logger, name));

            values.Add(value);
        }

        return GenericResult<DataStoreTarget>.Success(new DataStoreTarget(values[0], values[1], values[2]));
    }

    private async Task<IGenericResult<List<DataOptionRow>>> Read(DataStoreTarget table, IReadOnlyList<IFilterNode> conditions, PagingExpression? paging, CancellationToken cancellationToken)
    {
        var command = new QueryCommand<Dictionary<string, object?>>
        {
            Filter = And(conditions),
            Ordering = new OrderingExpression
            {
                OrderedFields =
                [
                    new OrderedField { PropertyName = DataOptionRowFields.Column, Direction = SortDirections.Ascending },
                    new OrderedField { PropertyName = DataOptionRowFields.Value, Direction = SortDirections.Ascending },
                ],
            },
            Paging = paging,
        };

        var rows = await Gateway.Execute<IEnumerable<Dictionary<string, object?>>>(command, table, cancellationToken).ConfigureAwait(false);
        if (rows.IsFailure || rows.Value is null)
        {
            DataOptionRowsLog.OptionsTableUnreadable(_logger, table.Container, table.Path ?? string.Empty, table.DataStore);
            return rows.ToNewResult<List<DataOptionRow>>();
        }

        var read = new List<DataOptionRow>();
        foreach (var row in rows.Value)
        {
            var parsed = StoredRow.Read(row, _logger);
            if (parsed.IsFailure || parsed.Value is null)
                return parsed.ToNewResult<List<DataOptionRow>>();

            read.Add(parsed.Value);
        }

        return GenericResult<List<DataOptionRow>>.Success(read);
    }

    private async Task<IGenericResult<HashSet<string>>> DiscoveredFields(DataOptionRowsContainer target, CancellationToken cancellationToken)
    {
        var container = await _stores.Get(target.DataStore, target.Path, target.Container, cancellationToken).ConfigureAwait(false);
        if (container.IsFailure || container.Value is null)
        {
            DataOptionRowsLog.ContainerUnreachable(_logger, target.Container, target.Path, target.DataStore);
            return container.ToNewResult<HashSet<string>>();
        }

        return GenericResult<HashSet<string>>.Success(new HashSet<string>(container.Value.Schema.Fields.Select(field => field.Name), StringComparer.OrdinalIgnoreCase));
    }

    // Each row as it will be stored, or the refusal of the first row that cannot be: positions count from 1, as a person reads them.
    private IGenericResult<List<DataOptionRow>> Stated(DataOptionRowsAddition addition, HashSet<string> fields)
    {
        var target = addition.Target;
        var pairs = new HashSet<(string Column, string Value)>();
        var rows = new List<DataOptionRow>();
        for (var index = 0; index < addition.Rows.Count; index++)
        {
            var row = addition.Rows[index];
            var position = index + 1;
            if (string.IsNullOrWhiteSpace(row.Value))
                return GenericResult<List<DataOptionRow>>.Failure(DataOptionRowsLog.ValueEmpty(_logger, position));

            if (row.Active is not { } active)
                return GenericResult<List<DataOptionRow>>.Failure(DataOptionRowsLog.ActiveNotStated(_logger, position));

            if (!fields.Contains(row.Column))
                return Refused<List<DataOptionRow>>(DataOptionRowsLog.ColumnNotDiscovered(_logger, row.Column, target.Container, target.DataStore), position);

            var resolved = Resolve(row.OptionType, row.Option);
            if (resolved.IsFailure)
                return Refused<List<DataOptionRow>>(resolved.Messages, position);

            var (optionType, option) = resolved.Value;

            var column = fields.First(field => string.Equals(field, row.Column, StringComparison.OrdinalIgnoreCase));
            if (!pairs.Add((column.ToUpperInvariant(), row.Value)))
                return GenericResult<List<DataOptionRow>>.Failure(DataOptionRowsLog.PairTwiceInAdd(_logger, position, column, row.Value));

            rows.Add(new DataOptionRow
            {
                Id = Guid.CreateVersion7(),
                DataStore = target.DataStore,
                Container = target.Container,
                Column = column,
                Value = row.Value,
                OptionType = optionType,
                Option = option,
                Active = active,
            });
        }

        return GenericResult<List<DataOptionRow>>.Success(rows);
    }

    // The option type and option by their registered names, so a stored row names them as DataOptions does.
    private IGenericResult<(string OptionType, string Option)> Resolve(string optionType, string option)
    {
        var type = DataOptions.All().FirstOrDefault(candidate => !ReferenceEquals(candidate, DataOptions.NotFound) && string.Equals(candidate.Name, optionType, StringComparison.Ordinal));
        if (type is null)
            return GenericResult<(string, string)>.Failure(DataOptionLog.OptionTypeNotFound(_logger, optionType));

        var named = type.Options.FirstOrDefault(candidate => string.Equals(candidate.Name, option, StringComparison.Ordinal));
        return named is null
            ? GenericResult<(string, string)>.Failure(DataOptionLog.OptionNotFound(_logger, optionType, option))
            : GenericResult<(string, string)>.Success((type.Name, named.Name));
    }

    // One read of the stored rows that could clash: the request's columns and values in this container. The first clash is the refusal.
    private async Task<IGenericResult?> Duplicates(DataStoreTarget table, DataOptionRowsContainer target, List<DataOptionRow> rows, CancellationToken cancellationToken)
    {
        IFilterNode[] conditions =
        [
            .. Owned(target),
            new FilterCondition { PropertyName = DataOptionRowFields.Column, Operator = FilterOperators.ByName("In"), Value = rows.Select(r => (object?)r.Column).Distinct().ToList() },
            new FilterCondition { PropertyName = DataOptionRowFields.Value, Operator = FilterOperators.ByName("In"), Value = rows.Select(r => (object?)r.Value).Distinct().ToList() },
        ];
        var stored = await Read(table, conditions, null, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure || stored.Value is null)
            return stored;

        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            if (stored.Value.Any(held => held.Id != row.Id && string.Equals(held.Column, row.Column, StringComparison.OrdinalIgnoreCase) && string.Equals(held.Value, row.Value, StringComparison.Ordinal)))
            {
                var clash = DataOptionRowsLog.PairAlreadyStored(_logger, row.Column, row.Value, target.Container);
                return rows.Count == 1 ? GenericResult.Failure(clash) : GenericResult.Failure(clash, DataOptionRowsLog.RowRefused(_logger, index + 1));
            }
        }

        return null;
    }

    private IGenericResult<DataOptionRow> Changed(DataOptionRow existing, DataOptionRowChange change)
    {
        var option = existing.Option;
        if (change.Option is not null)
        {
            var resolved = Resolve(existing.OptionType, change.Option);
            if (resolved.IsFailure)
                return resolved.ToNewResult<DataOptionRow>();

            option = resolved.Value.Option;
        }

        return GenericResult<DataOptionRow>.Success(new DataOptionRow
        {
            Id = existing.Id,
            DataStore = existing.DataStore,
            Container = existing.Container,
            Column = existing.Column,
            Value = change.Value ?? existing.Value,
            OptionType = existing.OptionType,
            Option = option,
            Active = change.Active ?? existing.Active,
        });
    }

    // Only the fields the change states are set; the others are not in the record, so the update leaves them as they are.
    private static Dictionary<string, object?> Stated(DataOptionRowChange change, DataOptionRow changed)
    {
        var record = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (change.Option is not null)
            record[DataOptionRowFields.Option] = changed.Option;

        if (change.Active is not null)
            record[DataOptionRowFields.Active] = changed.Active;

        if (change.Value is not null)
            record[DataOptionRowFields.Value] = changed.Value;

        return record;
    }

    private static Dictionary<string, object?> Record(DataOptionRow row) => new(StringComparer.Ordinal)
    {
        [DataOptionRowFields.Id] = row.Id,
        [DataOptionRowFields.DataStore] = row.DataStore,
        [DataOptionRowFields.Container] = row.Container,
        [DataOptionRowFields.Column] = row.Column,
        [DataOptionRowFields.Value] = row.Value,
        [DataOptionRowFields.OptionType] = row.OptionType,
        [DataOptionRowFields.Option] = row.Option,
        [DataOptionRowFields.Active] = row.Active,
    };

    private IGenericResult<T> Refused<T>(IGenericMessage reason, int position)
        => GenericResult<T>.Failure(reason, DataOptionRowsLog.RowRefused(_logger, position));

    private IGenericResult<T> Refused<T>(IEnumerable<IGenericMessage> reasons, int position)
        => GenericResult<T>.Failure(reasons.Append(DataOptionRowsLog.RowRefused(_logger, position)).ToList());

    private static IFilterNode[] Owned(DataOptionRowsContainer target) =>
    [
        Equal(DataOptionRowFields.DataStore, target.DataStore),
        Equal(DataOptionRowFields.Container, target.Container),
    ];

    private static FilterCondition Equal(string field, object value)
        => new() { PropertyName = field, Operator = FilterOperators.ByName("Equal"), Value = value };

    private static FilterExpression And(IReadOnlyList<IFilterNode> conditions)
        => new() { Root = new FilterGroup { Operator = LogicalOperator.And, Nodes = conditions } };
}
