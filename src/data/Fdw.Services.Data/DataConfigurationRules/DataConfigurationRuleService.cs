using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.Options;
using Fdw.Messages;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Abstractions.DataConfigurationRules;
using Fdw.Services.Data.Logging;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Settings;
using Fdw.Services.Workspaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.DataConfigurationRules;

/// <summary>
/// The data configuration rules and their values (FDW-6345), read and written in the tables the settings name, through the configuration
/// gateway of the store that holds them. A change keeps the old version (a save that replaces the current row; a remove marks the row deleted),
/// and every write runs in one unit of work (a <see cref="ConfigurationWriteScope"/>) on that store: write, record in the administrative audit, then
/// commit, which also drops the cached reads of the tables written, so a write that cannot be recorded does not take effect. A concurrent change is last writer wins.
/// </summary>
/// <remarks>
/// Not in this service: who may change a rule (FDW-6346), that an owner, a custodian or a column exists, and the longest text a field takes.
/// The path and method are stored as one text, <c>DataConfigurationRules:Comparison:In.To</c>.
/// </remarks>
public sealed class DataConfigurationRuleService : IDataConfigurationRules
{
    private const string ItemKind = "DataConfigurationRule";
    private const string RuleDescriptionType = DataConfigurationRuleDescriptionType.OptionName;
    private const string ValueDescriptionType = DataConfigurationRuleValueDescriptionType.OptionName;
    private const string DescriptionSubType = DescriptionTextSubType.OptionName;

    private readonly ILogger<DataConfigurationRuleService> _logger;
    private readonly IConfigurationGatewayProvider _gateways;
    private readonly IEffectiveSettingsProvider _settings;
    private readonly ITenantContext _tenant;
    private readonly IAdministrativeAudit _audit;
    private readonly IAuthenticationContextAccessor _authContext;
    private readonly IPermissionHolders _holders;
    private readonly IWorkspaceConfigurationProvider _workspaces;
    private readonly IWorkspaceAccessPolicy _workspaceAccess;
    private readonly IDataStoreProvider _stores;
    private readonly IDescriptionWriter _writer;
    private readonly IDescriptionReader _reader;

    /// <summary>Initializes a new instance of the <see cref="DataConfigurationRuleService"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="gateways">Supplies the configuration gateway of the store that holds the rule tables.</param>
    /// <param name="settings">Where the rule tables are named.</param>
    /// <param name="tenant">The caller's tenant, whose own settings apply when it has them.</param>
    /// <param name="audit">Where every change is recorded.</param>
    /// <param name="authContext">Who the caller is and the permissions they hold.</param>
    /// <param name="holders">Who holds <c>datastores:write</c>, the people who may be a custodian.</param>
    /// <param name="workspaces">Answers a workspace owner by id.</param>
    /// <param name="workspaceAccess">Whether the caller may view a workspace owner.</param>
    /// <param name="stores">Answers a container with the fields it declares, for the limits.</param>
    /// <param name="writer">Writes the description of a rule and of each of its values in the unit of the write (FDW-6208).</param>
    /// <param name="reader">Reads the descriptions of the rules and values a read has just found, once for the page (FDW-6208).</param>
    public DataConfigurationRuleService(
        ILogger<DataConfigurationRuleService>? logger,
        IConfigurationGatewayProvider gateways,
        IEffectiveSettingsProvider settings,
        ITenantContext tenant,
        IAdministrativeAudit audit,
        IAuthenticationContextAccessor authContext,
        IPermissionHolders holders,
        IWorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy workspaceAccess,
        IDataStoreProvider stores,
        IDescriptionWriter writer,
        IDescriptionReader reader)
    {
        _logger = logger ?? NullLogger<DataConfigurationRuleService>.Instance;
        _gateways = gateways;
        _settings = settings;
        _tenant = tenant;
        _audit = audit;
        _authContext = authContext;
        _holders = holders;
        _workspaces = workspaces;
        _workspaceAccess = workspaceAccess;
        _stores = stores;
        _writer = writer;
        _reader = reader;
    }

    private static readonly IReadOnlyList<string> RuleClearable = DataConfigurationRuleCatalog.Clearable(DataConfigurationRuleCatalog.RuleOwner);

    private static readonly IReadOnlyList<string> ValueClearable = DataConfigurationRuleCatalog.Clearable(DataConfigurationRuleCatalog.ValueOwner);

    private sealed record Caller(Guid UserId, IReadOnlyList<string> Permissions);

    private sealed record Opened(Tables Tables, Caller Caller, DataConfigurationRule Rule);

    private sealed record Tables(DataStoreTarget Rules, DataStoreTarget Values, IConfigurationGateway Gateway);

    /// <inheritdoc />
    public async Task<IGenericResult<DataConfigurationRulesPage>> List(DataConfigurationRulesQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Removed is not { } removed)
            return GenericResult<DataConfigurationRulesPage>.Failure(DataOptionRulesLog.RemovedMissing(_logger, query.Target.Container));

        var tables = Resolve();
        if (!tables.IsSuccess || tables.Value is null)
            return tables.ToNewResult<DataConfigurationRulesPage>();

        var caller = Identify("list");
        if (!caller.IsSuccess || caller.Value is null)
            return caller.ToNewResult<DataConfigurationRulesPage>();

        List<IFilterNode> conditions = [.. Scope(query.Target), Equal("IsCurrent", true), Equal("IsDeleted", removed)];
        if (query.Column is not null)
            conditions.Add(Equal("Column", query.Column));
        if (query.OwnerKind is not null)
            conditions.Add(Equal("OwnerKind", query.OwnerKind));
        if (query.OwnerId is { } ownerId)
            conditions.Add(Equal("OwnerId", ownerId));

        // The rules a container holds are few, so one read of all the matching rule rows gives the total the contract carries and the page is cut from it; only the page's rules
        // are given their values, changer and custodian state.
        var rows = await Read(tables.Value, tables.Value.Rules, conditions, ["Column", "Name"], null, cancellationToken).ConfigureAwait(false);
        if (!rows.IsSuccess || rows.Value is null)
            return rows.ToNewResult<DataConfigurationRulesPage>();

        var page = await Rules(tables.Value, caller.Value, [.. rows.Value.Skip(query.Skip).Take(query.Take)], cancellationToken).ConfigureAwait(false);
        return page.IsSuccess && page.Value is { } items
            ? GenericResult<DataConfigurationRulesPage>.Success(new DataConfigurationRulesPage { Items = items, TotalCount = rows.Value.Count })
            : page.ToNewResult<DataConfigurationRulesPage>();
    }

    /// <inheritdoc />
    public async Task<IGenericResult<DataConfigurationRuleLimits>> Limits(CancellationToken cancellationToken = default)
    {
        var tables = Resolve();
        if (!tables.IsSuccess || tables.Value is null)
            return tables.ToNewResult<DataConfigurationRuleLimits>();

        var caller = Identify("limits");
        if (!caller.IsSuccess)
            return caller.ToNewResult<DataConfigurationRuleLimits>();

        var rule = await Declared(tables.Value.Rules, DataConfigurationRuleCatalog.RuleOwner, cancellationToken).ConfigureAwait(false);
        if (!rule.IsSuccess || rule.Value is null)
            return rule.ToNewResult<DataConfigurationRuleLimits>();

        var value = await Declared(tables.Value.Values, DataConfigurationRuleCatalog.ValueOwner, cancellationToken).ConfigureAwait(false);
        return value.IsSuccess && value.Value is { } values
            ? GenericResult<DataConfigurationRuleLimits>.Success(new DataConfigurationRuleLimits { Rule = rule.Value, Value = values })
            : value.ToNewResult<DataConfigurationRuleLimits>();
    }

    // The maximum length each member of a rule or a value that has a column of its own declares there (the member's name with a capital first letter): a column that declares none is left out.
    private async Task<IGenericResult<Dictionary<string, int>>> Declared(DataStoreTarget table, string owner, CancellationToken cancellationToken)
    {
        var container = await _stores.Get(table.DataStore, table.Path!, table.Container, cancellationToken).ConfigureAwait(false);
        if (!container.IsSuccess || container.Value is null)
            return container.ToNewResult<Dictionary<string, int>>();

        var lengths = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var member in DataConfigurationRuleCatalog.Members.Where(candidate => string.Equals(candidate.Of, owner, StringComparison.Ordinal)))
        {
            var column = char.ToUpperInvariant(member.Name[0]) + member.Name[1..];
            var field = container.Value.Schema.Fields.FirstOrDefault(candidate => string.Equals(candidate.Name, column, StringComparison.OrdinalIgnoreCase));
            if (field?.Metadata is not { } metadata || !metadata.TryGetValue("MaxLength", out var declared))
                continue;

            if (declared is not (int or long) || Convert.ToInt64(declared, CultureInfo.InvariantCulture) is < 0 or > int.MaxValue)
                return GenericResult<Dictionary<string, int>>.Failure(DataOptionRulesLog.LimitNotAWholeNumber(_logger, column, table.Container));

            lengths[member.Name] = Convert.ToInt32(declared, CultureInfo.InvariantCulture);
        }

        return GenericResult<Dictionary<string, int>>.Success(lengths);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<DataConfigurationRuleHistoryPage>> History(DataConfigurationRuleHistoryQuery query, CancellationToken cancellationToken = default)
    {
        var tables = Resolve();
        if (!tables.IsSuccess || tables.Value is null)
            return tables.ToNewResult<DataConfigurationRuleHistoryPage>();

        var caller = Identify("history");
        if (!caller.IsSuccess)
            return caller.ToNewResult<DataConfigurationRuleHistoryPage>();

        var names = await RuleNames(tables.Value, query, cancellationToken).ConfigureAwait(false);
        if (!names.IsSuccess || names.Value is null)
            return names.ToNewResult<DataConfigurationRuleHistoryPage>();

        if (names.Value.Count == 0)
            return GenericResult<DataConfigurationRuleHistoryPage>.Success(new DataConfigurationRuleHistoryPage { Items = [], TotalCount = 0 });

        var filter = new AdministrativeChangeFilter(query.Page, query.PageSize, null, null, null, null, null)
        {
            ItemKind = ItemKind,
            ItemIds = [.. names.Value.Keys.Select(id => id.ToString())],
        };
        var read = await _audit.Read(filter, cancellationToken).ConfigureAwait(false);
        if (!read.IsSuccess || read.Value is null)
            return read.ToNewResult<DataConfigurationRuleHistoryPage>();

        var entries = HistoryEntries(read.Value.Items, names.Value, tables.Value);
        return entries.IsSuccess && entries.Value is { } items
            ? GenericResult<DataConfigurationRuleHistoryPage>.Success(new DataConfigurationRuleHistoryPage { Items = items, TotalCount = checked((int)read.Value.TotalCount) })
            : entries.ToNewResult<DataConfigurationRuleHistoryPage>();
    }

    // Each recorded change named through the catalog and the rule's name: a kind the catalog does not know, or a rule the container does not hold, fails the page.
    private IGenericResult<List<DataConfigurationRuleHistoryEntry>> HistoryEntries(IReadOnlyList<AdministrativeChangeRecord> changes, Dictionary<Guid, string> names, Tables tables)
    {
        var items = new List<DataConfigurationRuleHistoryEntry>();
        foreach (var change in changes)
        {
            if (DataConfigurationRuleCatalog.ChangeKind(change.ChangeKind) is not { } kind)
                return GenericResult<List<DataConfigurationRuleHistoryEntry>>.Failure(DataOptionRulesLog.ChangeKindUnknown(_logger, change.ChangeKind, change.ItemId));

            if (!Guid.TryParse(change.ItemId, out var ruleId) || !names.TryGetValue(ruleId, out var ruleName))
                return GenericResult<List<DataConfigurationRuleHistoryEntry>>.Failure(DataOptionRulesLog.StoredRuleUnreadable(_logger, "ItemId", tables.Rules.Container));

            items.Add(new DataConfigurationRuleHistoryEntry
            {
                At = change.OccurredAt,
                ByUserId = change.ActorUserId,
                ByName = change.ActorName,
                RuleId = ruleId,
                RuleName = ruleName,
                Kind = kind,
                OldValue = change.OldValue,
                NewValue = change.NewValue,
            });
        }

        return GenericResult<List<DataConfigurationRuleHistoryEntry>>.Success(items);
    }

    // The name of each rule the history may show, removed or not: the container's current rule rows, narrowed to the column or the one rule the query names.
    private async Task<IGenericResult<Dictionary<Guid, string>>> RuleNames(Tables tables, DataConfigurationRuleHistoryQuery query, CancellationToken cancellationToken)
    {
        List<IFilterNode> conditions = [.. Scope(query.Target), Equal("IsCurrent", true)];
        if (query.Column is not null)
            conditions.Add(Equal("Column", query.Column));
        if (query.RuleId is { } ruleId)
            conditions.Add(Equal("Id", ruleId));

        var rows = await Read(tables, tables.Rules, conditions, ["Name"], null, cancellationToken).ConfigureAwait(false);
        if (!rows.IsSuccess || rows.Value is null)
            return rows.ToNewResult<Dictionary<Guid, string>>();

        var names = new Dictionary<Guid, string>();
        foreach (var row in rows.Value)
        {
            if (!DataConfigurationRuleRows.IdOf(row, out var id))
                return GenericResult<Dictionary<Guid, string>>.Failure(DataOptionRulesLog.StoredRuleUnreadable(_logger, "Id", tables.Rules.Container));
            if (!DataConfigurationRuleRows.NameOf(row, out var name))
                return GenericResult<Dictionary<Guid, string>>.Failure(DataOptionRulesLog.StoredRuleUnreadable(_logger, "Name", tables.Rules.Container));

            names[id] = name;
        }

        return GenericResult<Dictionary<Guid, string>>.Success(names);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<DataConfigurationRule>> Get(DataConfigurationRuleKey key, CancellationToken cancellationToken = default)
    {
        var tables = Resolve();
        if (!tables.IsSuccess || tables.Value is null)
            return tables.ToNewResult<DataConfigurationRule>();

        var caller = Identify("get");
        if (!caller.IsSuccess || caller.Value is null)
            return caller.ToNewResult<DataConfigurationRule>();

        return await Load(tables.Value, caller.Value, key, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<DataConfigurationRule>> Add(NewDataConfigurationRule rule, CancellationToken cancellationToken = default)
    {
        var tables = Resolve();
        if (!tables.IsSuccess || tables.Value is null)
            return tables.ToNewResult<DataConfigurationRule>();

        var caller = Identify("add");
        if (!caller.IsSuccess || caller.Value is null)
            return caller.ToNewResult<DataConfigurationRule>();

        var named = rule.Name ?? string.Empty;
        var problems = RequiredOfNew(rule, named);
        var kind = DataConfigurationRuleChecks.Kind(_logger, named, rule.Path, rule.Method);
        IRuleKind? resolved = null;
        if (kind.IsSuccess && kind.Value is { } found)
            resolved = found;
        else
            problems.AddRange(kind.Messages);

        problems.AddRange(DataConfigurationRuleChecks.Values(_logger, named, Joined(rule), resolved, rule.Values));
        if (rule.RequiredPermission is { } needed && !PermissionCoverage.Covers(caller.Value.Permissions, needed))
            problems.Add(DataOptionRulesLog.PermissionNotHeld(_logger, caller.Value.UserId.ToString("D"), needed));

        if (problems.Count > 0)
            return GenericResult<DataConfigurationRule>.Failure(problems);

        if (await CustodianRefusals(rule.CustodianUserId!.Value, cancellationToken).ConfigureAwait(false) is { } notCustodian)
            return GenericResult<DataConfigurationRule>.Failure(notCustodian);

        if (await OwnerRefusals(rule.OwnerKind!, rule.OwnerId!.Value, caller.Value, cancellationToken).ConfigureAwait(false) is { } notOwner)
            return GenericResult<DataConfigurationRule>.Failure(notOwner);

        var taken = await Taken(tables.Value, rule.Target, rule.Column!, rule.Name!, rule.Abbreviation!, null, cancellationToken).ConfigureAwait(false);
        if (taken is not null)
            return GenericResult<DataConfigurationRule>.Failure(taken);

        var id = Guid.CreateVersion7();
        var record = new DataConfigurationRuleRecord
        {
            Id = id,
            Name = rule.Name!,
            Abbreviation = rule.Abbreviation!,
            Description = rule.Description,
            DataStore = rule.Target.DataStore,
            DataPath = rule.Target.DataPath,
            Container = rule.Target.Container,
            Column = rule.Column!,
            Path = DataConfigurationRuleRows.Joined(rule.Path!, rule.Method!),
            OwnerKind = rule.OwnerKind!,
            OwnerId = rule.OwnerId!.Value,
            CustodianUserId = rule.CustodianUserId!.Value,
            RequiredPermission = rule.RequiredPermission,
            Active = rule.Active!.Value,
        };
        var values = rule.Values!.Select(value => ValueRecord(id, Guid.CreateVersion7(), value)).ToList();
        List<AdministrativeChangeEntry> entries = [Entry("RuleAdded", record, null, record.Name), .. values.Select(value => Entry("ValueAdded", record, null, ValueText(value)))];

        var written = await Write(
            tables.Value,
            "add",
            unit => SaveNew(unit, tables.Value, record, values, cancellationToken),
            entries,
            cancellationToken).ConfigureAwait(false);
        if (written.IsFailure)
            return written.ToNewResult<DataConfigurationRule>();

        DataOptionRulesLog.Written(_logger, "added", record.Name, record.Container);
        return await Load(tables.Value, caller.Value, new DataConfigurationRuleKey { Target = rule.Target, RuleId = id }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<DataConfigurationRule>> Change(DataConfigurationRuleChange change, CancellationToken cancellationToken = default)
    {
        var opened = await Open("change", change.Key, cancellationToken).ConfigureAwait(false);
        if (!opened.IsSuccess || opened.Value is null)
            return opened.ToNewResult<DataConfigurationRule>();

        var (tables, caller, rule) = opened.Value;
        var refusals = ChangeRefusals(change, rule, caller);
        if (refusals.Count > 0)
            return GenericResult<DataConfigurationRule>.Failure(refusals);

        if (change.CustodianUserId is { } custodian
            && custodian != rule.CustodianUserId
            && await CustodianRefusals(custodian, cancellationToken).ConfigureAwait(false) is { } notCustodian)
            return GenericResult<DataConfigurationRule>.Failure(notCustodian);

        if (await OwnerChangeRefusals(change, rule, caller, cancellationToken).ConfigureAwait(false) is { } notOwner)
            return GenericResult<DataConfigurationRule>.Failure(notOwner);

        var changed = Changed(rule, change);
        var entries = Differences(rule, changed);
        if (entries.Count == 0)
            return GenericResult<DataConfigurationRule>.Success(rule);

        if (IsRenamed(rule, changed)
            && await Taken(tables, rule.Target, changed.Column, changed.Name, changed.Abbreviation, rule.Id, cancellationToken).ConfigureAwait(false) is { } taken)
            return GenericResult<DataConfigurationRule>.Failure(taken);

        return await Replace(tables, caller, change.Key, "changed", changed, entries, true, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<DataConfigurationRule>> Switch(DataConfigurationRuleKey key, bool active, CancellationToken cancellationToken = default)
    {
        var tables = Resolve();
        if (!tables.IsSuccess || tables.Value is null)
            return tables.ToNewResult<DataConfigurationRule>();

        var caller = Identify("switch");
        if (!caller.IsSuccess || caller.Value is null)
            return caller.ToNewResult<DataConfigurationRule>();

        var current = await Load(tables.Value, caller.Value, key, cancellationToken).ConfigureAwait(false);
        if (!current.IsSuccess || current.Value is null)
            return current;

        if (Forbidden(current.Value, caller.Value) is { } forbidden)
            return GenericResult<DataConfigurationRule>.Failure(forbidden);

        if (current.Value.Removed)
            return GenericResult<DataConfigurationRule>.Failure(DataOptionRulesLog.RuleRemoved(_logger, current.Value.Name));

        if (current.Value.Active == active)
            return current;

        var record = RecordOf(current.Value, active);
        return await Replace(tables.Value, caller.Value, key, active ? "switched on" : "switched off", record,
            [Entry("ActiveChanged", record, current.Value.Active.ToString(), active.ToString())], false, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<DataConfigurationRule>> Remove(DataConfigurationRuleKey key, CancellationToken cancellationToken = default)
    {
        var tables = Resolve();
        if (!tables.IsSuccess || tables.Value is null)
            return tables.ToNewResult<DataConfigurationRule>();

        var caller = Identify("remove");
        if (!caller.IsSuccess || caller.Value is null)
            return caller.ToNewResult<DataConfigurationRule>();

        var current = await Load(tables.Value, caller.Value, key, cancellationToken).ConfigureAwait(false);
        if (!current.IsSuccess || current.Value is null)
            return current;

        if (Forbidden(current.Value, caller.Value) is { } forbidden)
            return GenericResult<DataConfigurationRule>.Failure(forbidden);

        if (current.Value.Removed)
            return GenericResult<DataConfigurationRule>.Failure(DataOptionRulesLog.RuleRemoved(_logger, current.Value.Name));

        var record = RecordOf(current.Value, current.Value.Active);
        var written = await Write(
            tables.Value,
            "removed",
            unit => Retire(unit, key.RuleId, tables.Value.Rules, cancellationToken),
            [Entry("RuleRemoved", record, record.Name, null)],
            cancellationToken).ConfigureAwait(false);
        if (written.IsFailure)
            return written.ToNewResult<DataConfigurationRule>();

        DataOptionRulesLog.Written(_logger, "removed", record.Name, record.Container);
        return await Load(tables.Value, caller.Value, key, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<DataConfigurationRule>> Restore(DataConfigurationRuleKey key, CancellationToken cancellationToken = default)
    {
        var tables = Resolve();
        if (!tables.IsSuccess || tables.Value is null)
            return tables.ToNewResult<DataConfigurationRule>();

        var caller = Identify("restore");
        if (!caller.IsSuccess || caller.Value is null)
            return caller.ToNewResult<DataConfigurationRule>();

        var current = await Load(tables.Value, caller.Value, key, cancellationToken).ConfigureAwait(false);
        if (!current.IsSuccess || current.Value is null)
            return current;

        if (Forbidden(current.Value, caller.Value) is { } forbidden)
            return GenericResult<DataConfigurationRule>.Failure(forbidden);

        if (!current.Value.Removed)
            return GenericResult<DataConfigurationRule>.Failure(DataOptionRulesLog.RuleNotRemoved(_logger, current.Value.Name));

        var taken = await Taken(tables.Value, current.Value.Target, current.Value.Column, current.Value.Name, current.Value.Abbreviation, current.Value.Id, cancellationToken).ConfigureAwait(false);
        if (taken is not null)
            return GenericResult<DataConfigurationRule>.Failure(taken);

        var record = RecordOf(current.Value, false);
        return await Replace(tables.Value, caller.Value, key, "restored", record, [Entry("RuleRestored", record, null, record.Name)], false, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<DataConfigurationRule>> AddValues(
        DataConfigurationRuleKey key, IReadOnlyList<NewDataConfigurationRuleValue> values, CancellationToken cancellationToken = default)
    {
        var opened = await Open("add values", key, cancellationToken).ConfigureAwait(false);
        if (!opened.IsSuccess || opened.Value is null)
            return opened.ToNewResult<DataConfigurationRule>();

        var (tables, caller, rule) = opened.Value;
        var problems = AddedProblems(rule, values);
        if (problems.Count > 0)
            return GenericResult<DataConfigurationRule>.Failure(problems);

        var record = RecordOf(rule, rule.Active);
        var added = values.Select(value => ValueRecord(rule.Id, Guid.CreateVersion7(), value)).ToList();
        var written = await Write(
            tables,
            "values added",
            unit => SaveValues(unit, tables, record, added, cancellationToken),
            [.. added.Select(value => Entry("ValueAdded", record, null, ValueText(value)))],
            cancellationToken).ConfigureAwait(false);
        if (written.IsFailure)
            return written.ToNewResult<DataConfigurationRule>();

        DataOptionRulesLog.Written(_logger, "values added", record.Name, record.Container);
        return await Load(tables, caller, key, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<DataConfigurationRule>> ChangeValue(DataConfigurationRuleValueChange change, CancellationToken cancellationToken = default)
    {
        var opened = await Open("change value", change.Key, cancellationToken).ConfigureAwait(false);
        if (!opened.IsSuccess || opened.Value is null)
            return opened.ToNewResult<DataConfigurationRule>();

        var (tables, caller, rule) = opened.Value;
        var existing = rule.Values.FirstOrDefault(value => value.Id == change.ValueId);
        if (existing is null)
            return GenericResult<DataConfigurationRule>.Failure(DataOptionRulesLog.ValueNotFound(_logger, change.ValueId.ToString(), rule.Name));

        if (NotClearable(change.Clear, ValueClearable) is { } notClearable)
            return GenericResult<DataConfigurationRule>.Failure(notClearable);

        var merged = Merged(existing, change);
        var problems = DataConfigurationRuleChecks.One(_logger, rule.Name, merged.Ordinal ?? existing.Ordinal, merged);
        if (problems.Count > 0)
            return GenericResult<DataConfigurationRule>.Failure(problems);

        var record = RecordOf(rule, rule.Active);
        var replaced = ValueRecord(rule.Id, existing.Id, merged);
        var written = await Write(
            tables,
            "value changed",
            async unit =>
            {
                var saved = await Save(unit, replaced, true, tables.Values, cancellationToken).ConfigureAwait(false);
                return saved.IsSuccess
                    ? await DescribeValue(unit, record, replaced, cancellationToken).ConfigureAwait(false)
                    : saved;
            },
            [Entry("ValueChanged", record, ValueText(existing), ValueText(replaced))],
            cancellationToken).ConfigureAwait(false);
        if (written.IsFailure)
            return written.ToNewResult<DataConfigurationRule>();

        DataOptionRulesLog.Written(_logger, "value changed", record.Name, record.Container);
        return await Load(tables, caller, change.Key, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<DataConfigurationRule>> RemoveValues(
        DataConfigurationRuleKey key, IReadOnlyList<Guid> valueIds, CancellationToken cancellationToken = default)
    {
        var tables = Resolve();
        if (!tables.IsSuccess || tables.Value is null)
            return tables.ToNewResult<DataConfigurationRule>();

        var caller = Identify("remove values");
        if (!caller.IsSuccess || caller.Value is null)
            return caller.ToNewResult<DataConfigurationRule>();

        var current = await Load(tables.Value, caller.Value, key, cancellationToken).ConfigureAwait(false);
        if (!current.IsSuccess || current.Value is null)
            return current;

        if (Forbidden(current.Value, caller.Value) is { } forbidden)
            return GenericResult<DataConfigurationRule>.Failure(forbidden);

        var rule = current.Value;
        if (rule.Removed)
            return GenericResult<DataConfigurationRule>.Failure(DataOptionRulesLog.RuleRemoved(_logger, rule.Name));

        var removing = new List<DataConfigurationRuleValue>();
        foreach (var id in valueIds)
        {
            var found = rule.Values.FirstOrDefault(value => value.Id == id);
            if (found is null)
                return GenericResult<DataConfigurationRule>.Failure(DataOptionRulesLog.ValueNotFound(_logger, id.ToString(), rule.Name));

            removing.Add(found);
        }

        var remaining = rule.Values.Count - removing.Count;
        var kind = DataConfigurationRuleChecks.Kind(_logger, rule.Name, rule.Path, rule.Method);
        if (kind.IsSuccess && kind.Value is { } resolved && remaining < resolved.MinValues)
            return GenericResult<DataConfigurationRule>.Failure(DataOptionRulesLog.ValueCountNotTaken(
                _logger, rule.Name, DataConfigurationRuleRows.Joined(rule.Path, rule.Method), DataConfigurationRuleChecks.Takes(resolved), remaining));

        var record = RecordOf(rule, rule.Active);
        var written = await Write(
            tables.Value,
            "values removed",
            async unit =>
            {
                foreach (var value in removing)
                {
                    var retired = await Retire(unit, value.Id, tables.Value.Values, cancellationToken).ConfigureAwait(false);
                    if (!retired.IsSuccess)
                        return retired;
                }

                return GenericResult.Success();
            },
            [.. removing.Select(value => Entry("ValueRemoved", record, ValueText(value), null))],
            cancellationToken).ConfigureAwait(false);
        if (written.IsFailure)
            return written.ToNewResult<DataConfigurationRule>();

        DataOptionRulesLog.Written(_logger, "values removed", record.Name, record.Container);
        return await Load(tables.Value, caller.Value, key, cancellationToken).ConfigureAwait(false);
    }

    // The person asking: a signed-in user with the permissions the token carries. Reading a rule needs it too, because every rule states whether this caller may change it.
    private IGenericResult<Caller> Identify(string operation)
    {
        if (_authContext.Current is not { IsAuthenticated: true } context || !Guid.TryParse(context.UserId, out var userId))
            return GenericResult<Caller>.Failure(DataOptionRulesLog.CallerNotIdentified(_logger, operation));

        return GenericResult<Caller>.Success(new Caller(userId, [.. context.Permissions]));
    }

    // The custodian controls who may change a rule: the custodian, or a holder of the permission the rule names.
    private static bool MayChange(Caller caller, Guid custodian, string? requiredPermission) =>
        caller.UserId == custodian || (requiredPermission is { } permission && PermissionCoverage.Covers(caller.Permissions, permission));

    private IGenericMessage? Forbidden(DataConfigurationRule rule, Caller caller) =>
        rule.CanChange ? null : DataOptionRulesLog.RuleChangeForbidden(_logger, caller.UserId.ToString("D"), rule.Name, rule.RequiredPermission ?? string.Empty);

    // What every write that changes a rule starts with: the tables, who is asking, the rule as stored, that the caller may change it, and that it is not removed.
    private async Task<IGenericResult<Opened>> Open(string operation, DataConfigurationRuleKey key, CancellationToken cancellationToken)
    {
        var tables = Resolve();
        if (!tables.IsSuccess || tables.Value is null)
            return tables.ToNewResult<Opened>();

        var caller = Identify(operation);
        if (!caller.IsSuccess || caller.Value is null)
            return caller.ToNewResult<Opened>();

        var loaded = await Load(tables.Value, caller.Value, key, cancellationToken).ConfigureAwait(false);
        if (!loaded.IsSuccess || loaded.Value is null)
            return loaded.ToNewResult<Opened>();

        if (Forbidden(loaded.Value, caller.Value) is { } forbidden)
            return GenericResult<Opened>.Failure(forbidden);

        return loaded.Value.Removed
            ? GenericResult<Opened>.Failure(DataOptionRulesLog.RuleRemoved(_logger, loaded.Value.Name))
            : GenericResult<Opened>.Success(new Opened(tables.Value, caller.Value, loaded.Value));
    }

    // A custodian is a holder of datastores:write. Null when the person is one; why not when the person is not, or the holders could not be read.
    private async Task<IReadOnlyList<IGenericMessage>?> CustodianRefusals(Guid custodian, CancellationToken cancellationToken)
    {
        var holders = await DataOptionCustodianHolders.Get(_holders, _tenant, cancellationToken).ConfigureAwait(false);
        if (!holders.IsSuccess || holders.Value is null)
            return holders.Messages.ToList();

        if (holders.Value.Contains(custodian))
            return null;

        return [DataOptionRulesLog.CustodianNotFound(_logger, custodian.ToString("D"))];
    }

    // A change that names the owner the rule already has is not checked again; one that names a different owner is checked as an add is.
    private async Task<IReadOnlyList<IGenericMessage>?> OwnerChangeRefusals(DataConfigurationRuleChange change, DataConfigurationRule rule, Caller caller, CancellationToken cancellationToken)
    {
        var ownerKind = change.OwnerKind ?? rule.OwnerKind;
        var ownerId = change.OwnerId ?? rule.OwnerId;
        if (string.Equals(ownerKind, rule.OwnerKind, StringComparison.Ordinal) && ownerId == rule.OwnerId)
            return null;

        return await OwnerRefusals(ownerKind, ownerId, caller, cancellationToken).ConfigureAwait(false);
    }

    // A workspace owner is one the caller may view: it must exist (OwnerNotFound) and the workspace access policy must let the caller view it (OwnerNotVisible). A policy that
    // cannot tell fails with its own message, never as not visible, which would hide an outage. An organization owner is not checked here: no access policy names who may
    // view an organization, so the rule layer does not invent one. Null when the owner passes; why not otherwise.
    private async Task<IReadOnlyList<IGenericMessage>?> OwnerRefusals(string ownerKind, Guid ownerId, Caller caller, CancellationToken cancellationToken)
    {
        if (!string.Equals(ownerKind, "Workspace", StringComparison.Ordinal))
            return null;

        var found = await _workspaces.Get(ownerId, cancellationToken).ConfigureAwait(false);
        if (!found.IsSuccess || found.Value is null)
            return [DataOptionRulesLog.OwnerNotFound(_logger, ownerKind, ownerId.ToString("D"))];

        var viewable = await _workspaceAccess.MayView(found.Value, cancellationToken).ConfigureAwait(false);
        if (viewable.IsSuccess)
            return null;

        return WorkspaceAccessRefusals.IsUndetermined(viewable)
            ? viewable.Messages.ToList()
            : [DataOptionRulesLog.OwnerNotVisible(_logger, caller.UserId.ToString("D"), ownerKind, ownerId.ToString("D"))];
    }

    // Why a change is refused before anything is written: a field that cannot be cleared, a path or method the option layer does not know or whose value count the rule's values do not fit, an owner kind that is not one, and a permission the caller does not hold.
    private List<IGenericMessage> ChangeRefusals(DataConfigurationRuleChange change, DataConfigurationRule rule, Caller caller)
    {
        var problems = new List<IGenericMessage>();
        if (NotClearable(change.Clear, RuleClearable) is { } notClearable)
            problems.Add(notClearable);

        problems.AddRange(PathProblems(change, rule));
        if (change.OwnerKind is not null && DataConfigurationRuleChecks.Owner(_logger, change.OwnerKind) is { } ownerProblem)
            problems.Add(ownerProblem);

        if (change.RequiredPermission is { } requested && !PermissionCoverage.Covers(caller.Permissions, requested))
            problems.Add(DataOptionRulesLog.PermissionNotHeld(_logger, caller.UserId.ToString("D"), requested));

        return problems;
    }

    // The first field a clear list names that the record cannot clear.
    private IGenericMessage? NotClearable(IReadOnlyList<string>? clear, IReadOnlyList<string> allowed)
    {
        foreach (var field in clear ?? [])
        {
            if (!allowed.Contains(field, StringComparer.Ordinal))
                return DataOptionRulesLog.FieldNotClearable(_logger, field);
        }

        return null;
    }

    private List<IGenericMessage> PathProblems(DataConfigurationRuleChange change, DataConfigurationRule rule)
    {
        var problems = new List<IGenericMessage>();
        if (change.Path is null && change.Method is null)
            return problems;

        var path = change.Path ?? rule.Path;
        var method = change.Method ?? rule.Method;
        var kind = DataConfigurationRuleChecks.Kind(_logger, rule.Name, path, method);
        if (!kind.IsSuccess || kind.Value is not { } resolved)
            problems.AddRange(kind.Messages);
        else if (CountProblem(rule.Name, path, method, resolved, rule.Values.Count) is { } count)
            problems.Add(count);

        return problems;
    }

    // The values a rule is given are checked on their own and then against what the rule's kind takes in all.
    private List<IGenericMessage> AddedProblems(DataConfigurationRule rule, IReadOnlyList<NewDataConfigurationRuleValue> values)
    {
        var problems = new List<IGenericMessage>();
        for (var index = 0; index < values.Count; index++)
            problems.AddRange(DataConfigurationRuleChecks.One(_logger, rule.Name, index + 1, values[index]));

        var kind = DataConfigurationRuleChecks.Kind(_logger, rule.Name, rule.Path, rule.Method);
        if (kind.IsSuccess && kind.Value is { } resolved && CountProblem(rule.Name, rule.Path, rule.Method, resolved, rule.Values.Count + values.Count) is { } count)
            problems.Add(count);

        return problems;
    }

    private IGenericMessage? CountProblem(string rule, IReadOnlyList<string> path, string method, IRuleKind kind, int count) =>
        count < kind.MinValues || (kind.MaxValues is { } most && count > most)
            ? DataOptionRulesLog.ValueCountNotTaken(_logger, rule, DataConfigurationRuleRows.Joined(path, method), DataConfigurationRuleChecks.Takes(kind), count)
            : null;

    private static bool IsRenamed(DataConfigurationRule rule, DataConfigurationRuleRecord changed) =>
        !string.Equals(changed.Name, rule.Name, StringComparison.Ordinal)
        || !string.Equals(changed.Abbreviation, rule.Abbreviation, StringComparison.Ordinal)
        || !string.Equals(changed.Column, rule.Column, StringComparison.Ordinal);

    // The rule as it will be stored: what the change states over what is stored, three groups of fields each in its own method.
    private static DataConfigurationRuleRecord Changed(DataConfigurationRule rule, DataConfigurationRuleChange change)
    {
        var record = RecordOf(rule, rule.Active);
        var clear = change.Clear ?? [];
        Labelled(record, rule, change, clear);
        Placed(record, rule, change);
        Governed(record, rule, change, clear);
        return record;
    }

    private static void Labelled(DataConfigurationRuleRecord record, DataConfigurationRule rule, DataConfigurationRuleChange change, IReadOnlyList<string> clear)
    {
        record.Name = change.Name ?? rule.Name;
        record.Abbreviation = change.Abbreviation ?? rule.Abbreviation;
        record.Description = clear.Contains("description", StringComparer.Ordinal) ? null : change.Description ?? rule.Description;
    }

    private static void Placed(DataConfigurationRuleRecord record, DataConfigurationRule rule, DataConfigurationRuleChange change)
    {
        record.Column = change.Column ?? rule.Column;
        record.Path = DataConfigurationRuleRows.Joined(change.Path ?? rule.Path, change.Method ?? rule.Method);
    }

    private static void Governed(DataConfigurationRuleRecord record, DataConfigurationRule rule, DataConfigurationRuleChange change, IReadOnlyList<string> clear)
    {
        record.OwnerKind = change.OwnerKind ?? rule.OwnerKind;
        record.OwnerId = change.OwnerId ?? rule.OwnerId;
        record.CustodianUserId = change.CustodianUserId ?? rule.CustodianUserId;
        record.RequiredPermission = clear.Contains("requiredPermission", StringComparer.Ordinal) ? null : change.RequiredPermission ?? rule.RequiredPermission;
    }

    // A value as it will be stored: what the change states over what is stored, its content and its labels each in their own method.
    private static NewDataConfigurationRuleValue Merged(DataConfigurationRuleValue existing, DataConfigurationRuleValueChange change)
    {
        var content = Content(existing, change);
        var labels = Labels(existing, change);
        return new NewDataConfigurationRuleValue
        {
            Kind = content.Kind,
            Value = content.Value,
            Parameter = content.Parameter,
            Name = labels.Name,
            Abbreviation = labels.Abbreviation,
            Description = labels.Description,
            Ordinal = change.Ordinal ?? existing.Ordinal,
        };
    }

    // Changing the kind to say a parameter, or to say a literal, drops the other member.
    private static (string Kind, string? Value, string? Parameter) Content(DataConfigurationRuleValue existing, DataConfigurationRuleValueChange change) =>
        (change.Kind ?? existing.Kind,
         change.Kind is not null && change.Value is null && change.Parameter is not null ? null : change.Value ?? existing.Value,
         change.Kind is not null && change.Parameter is null && change.Value is not null ? null : change.Parameter ?? existing.Parameter);

    private static (string? Name, string? Abbreviation, string? Description) Labels(DataConfigurationRuleValue existing, DataConfigurationRuleValueChange change)
    {
        var clear = change.Clear ?? [];
        return (
            clear.Contains("name", StringComparer.Ordinal) ? null : change.Name ?? existing.Name,
            clear.Contains("abbreviation", StringComparer.Ordinal) ? null : change.Abbreviation ?? existing.Abbreviation,
            clear.Contains("description", StringComparer.Ordinal) ? null : change.Description ?? existing.Description);
    }

    // The tables the four rule settings name (never the option row service's), and the configuration gateway of their store; the first setting not set is the refusal, never a default.
    private IGenericResult<Tables> Resolve()
    {
        string[] names =
        [
            SettingDefinitions.DataConfigurationRuleDataStore,
            SettingDefinitions.DataConfigurationRulePath,
            SettingDefinitions.DataConfigurationRuleContainer,
            SettingDefinitions.DataConfigurationRuleValueContainer,
        ];
        var named = new List<string>();
        foreach (var name in names)
        {
            if (!_settings.TryGetEffectiveValue<string>(name, out var value, _tenant.TenantId) || string.IsNullOrWhiteSpace(value))
                return GenericResult<Tables>.Failure(DataOptionRulesLog.RuleSettingNotSet(_logger, name));

            named.Add(value);
        }

        var gateway = _gateways.Get(named[0]);
        if (!gateway.IsSuccess || gateway.Value is null)
            return gateway.ToNewResult<Tables>();

        return GenericResult<Tables>.Success(new Tables(
            new DataStoreTarget(named[0], named[1], named[2]), new DataStoreTarget(named[0], named[1], named[3]), gateway.Value));
    }

    private async Task<IGenericResult<List<Dictionary<string, object?>>>> Read(
        Tables tables, DataStoreTarget table, IReadOnlyList<IFilterNode> conditions, IReadOnlyList<string> order, PagingExpression? paging, CancellationToken cancellationToken)
    {
        var command = new QueryCommand<Dictionary<string, object?>>
        {
            Filter = And(conditions),
            Ordering = new OrderingExpression { OrderedFields = [.. order.Select(field => new OrderedField { PropertyName = field, Direction = SortDirections.Ascending })] },
            Paging = paging,
        };

        var rows = await tables.Gateway.Execute<IEnumerable<Dictionary<string, object?>>>(command, table, cancellationToken).ConfigureAwait(false);
        if (!rows.IsSuccess || rows.Value is null)
        {
            DataOptionRulesLog.RuleTablesUnreadable(_logger, table.Container, table.DataStore);
            return rows.ToNewResult<List<Dictionary<string, object?>>>();
        }

        return GenericResult<List<Dictionary<string, object?>>>.Success([.. rows.Value]);
    }

    // One rule of a container with its values, removed or not.
    private async Task<IGenericResult<DataConfigurationRule>> Load(Tables tables, Caller caller, DataConfigurationRuleKey key, CancellationToken cancellationToken)
    {
        List<IFilterNode> conditions = [.. Scope(key.Target), Equal("IsCurrent", true), Equal("Id", key.RuleId)];
        var rows = await Read(tables, tables.Rules, conditions, ["Name"], null, cancellationToken).ConfigureAwait(false);
        if (!rows.IsSuccess || rows.Value is null)
            return rows.ToNewResult<DataConfigurationRule>();

        if (rows.Value.Count == 0)
            return GenericResult<DataConfigurationRule>.Failure(DataOptionRulesLog.RuleNotFound(_logger, key.RuleId.ToString(), key.Target.Container));

        var rules = await Rules(tables, caller, rows.Value, cancellationToken).ConfigureAwait(false);
        return rules.IsSuccess && rules.Value is { Count: > 0 } found
            ? GenericResult<DataConfigurationRule>.Success(found[0])
            : rules.ToNewResult<DataConfigurationRule>();
    }

    // The rules the rows describe, each with its current values (one query for all of them), who last changed it (one read of the audit) and whether its custodian still
    // holds datastores:write (one read of the holders; a read that fails fails the page with its own message, never a guessed state).
    private async Task<IGenericResult<IReadOnlyList<DataConfigurationRule>>> Rules(Tables tables, Caller caller, List<Dictionary<string, object?>> rows, CancellationToken cancellationToken)
    {
        var ids = RuleIds(tables, rows);
        if (!ids.IsSuccess || ids.Value is null)
            return ids.ToNewResult<IReadOnlyList<DataConfigurationRule>>();

        var byRule = await ValuesByRule(tables, ids.Value, cancellationToken).ConfigureAwait(false);
        if (!byRule.IsSuccess || byRule.Value is null)
            return byRule.ToNewResult<IReadOnlyList<DataConfigurationRule>>();

        // The rules' descriptions are not columns: one read of the descriptions table, in the language of the caller's tenant, for the whole page (FDW-6208).
        var texts = await _reader.ReadMany(RuleDescriptionType, DescriptionSubType, ids.Value, cancellationToken).ConfigureAwait(false);
        if (!texts.IsSuccess || texts.Value is null)
            return texts.ToNewResult<IReadOnlyList<DataConfigurationRule>>();

        var changers = await Changers(ids.Value, cancellationToken).ConfigureAwait(false);
        if (!changers.IsSuccess || changers.Value is null)
            return changers.ToNewResult<IReadOnlyList<DataConfigurationRule>>();

        var holders = await DataOptionCustodianHolders.Get(_holders, _tenant, cancellationToken).ConfigureAwait(false);
        if (!holders.IsSuccess || holders.Value is null)
            return holders.ToNewResult<IReadOnlyList<DataConfigurationRule>>();

        return Compose(tables, caller, rows, ids.Value, byRule.Value, texts.Value, changers.Value, holders.Value);
    }

    // The page's rules, each made from its row, its values, its description, who last changed it and whether its custodian still holds the permission (FDW-6208 moved the fill of
    // the description into this read, which took Rules over the branch limit, so the making of each rule is a member of its own).
    private IGenericResult<IReadOnlyList<DataConfigurationRule>> Compose(
        Tables tables, Caller caller, List<Dictionary<string, object?>> rows, List<Guid> ids, Dictionary<Guid, List<DataConfigurationRuleValue>> byRule,
        IReadOnlyDictionary<Guid, DescriptionEntry> texts, Dictionary<Guid, Guid> changers, IReadOnlyList<Guid> holders)
    {
        var rules = new List<DataConfigurationRule>();
        for (var index = 0; index < rows.Count; index++)
        {
            var id = ids[index];
            var rule = DataConfigurationRuleRows.ToRule(
                rows[index], byRule.GetValueOrDefault(id) ?? [], tables.Rules.Container, _logger,
                (custodian, permission) => MayChange(caller, custodian, permission), ruleId => changers.TryGetValue(ruleId, out var changer) ? changer : null,
                custodian => holders.Contains(custodian) ? DataOptionCustodianStates.Holder.Name : DataOptionCustodianStates.NoLongerHolder.Name,
                texts.TryGetValue(id, out var described) ? described.Text : null);
            if (!rule.IsSuccess || rule.Value is null)
                return rule.ToNewResult<IReadOnlyList<DataConfigurationRule>>();

            rules.Add(rule.Value);
        }

        return GenericResult<IReadOnlyList<DataConfigurationRule>>.Success(rules);
    }

    // The id of each stored rule row, in the order of the rows.
    private IGenericResult<List<Guid>> RuleIds(Tables tables, List<Dictionary<string, object?>> rows)
    {
        var ids = new List<Guid>();
        foreach (var row in rows)
        {
            if (!DataConfigurationRuleRows.IdOf(row, out var id))
                return GenericResult<List<Guid>>.Failure(DataOptionRulesLog.StoredRuleUnreadable(_logger, "Id", tables.Rules.Container));

            ids.Add(id);
        }

        return GenericResult<List<Guid>>.Success(ids);
    }

    // The current values of the rules, read in one query and grouped by the rule they belong to.
    private async Task<IGenericResult<Dictionary<Guid, List<DataConfigurationRuleValue>>>> ValuesByRule(Tables tables, List<Guid> ids, CancellationToken cancellationToken)
    {
        var byRule = new Dictionary<Guid, List<DataConfigurationRuleValue>>();
        if (ids.Count == 0)
            return GenericResult<Dictionary<Guid, List<DataConfigurationRuleValue>>>.Success(byRule);

        List<IFilterNode> conditions = [Equal("IsCurrent", true), Equal("IsDeleted", false), In("DataConfigurationRuleId", ids)];
        var valueRows = await Read(tables, tables.Values, conditions, ["Ordinal"], null, cancellationToken).ConfigureAwait(false);
        if (!valueRows.IsSuccess || valueRows.Value is null)
            return valueRows.ToNewResult<Dictionary<Guid, List<DataConfigurationRuleValue>>>();

        // The values' descriptions are not columns either: one read of the descriptions the rules own, for every rule of the page (FDW-6208).
        var texts = await _reader.ReadOwnedBy(ValueDescriptionType, DescriptionSubType, ids, cancellationToken).ConfigureAwait(false);
        if (!texts.IsSuccess || texts.Value is null)
            return texts.ToNewResult<Dictionary<Guid, List<DataConfigurationRuleValue>>>();

        foreach (var row in valueRows.Value)
        {
            if (!DataConfigurationRuleRows.RuleIdOf(row, out var owner))
                return GenericResult<Dictionary<Guid, List<DataConfigurationRuleValue>>>.Failure(DataOptionRulesLog.StoredRuleUnreadable(_logger, "DataConfigurationRuleId", tables.Values.Container));

            var value = DataConfigurationRuleRows.ToValue(row, tables.Values.Container, _logger, texts.Value);
            if (!value.IsSuccess || value.Value is null)
                return value.ToNewResult<Dictionary<Guid, List<DataConfigurationRuleValue>>>();

            if (!byRule.TryGetValue(owner, out var list))
                byRule[owner] = list = [];

            list.Add(value.Value);
        }

        return GenericResult<Dictionary<Guid, List<DataConfigurationRuleValue>>>.Success(byRule);
    }

    // Who last changed each rule: the actor of its newest recorded change, in one read of the audit for the whole page. A rule with no recorded change has none.
    private async Task<IGenericResult<Dictionary<Guid, Guid>>> Changers(List<Guid> ruleIds, CancellationToken cancellationToken)
    {
        var changers = new Dictionary<Guid, Guid>();
        if (ruleIds.Count == 0)
            return GenericResult<Dictionary<Guid, Guid>>.Success(changers);

        var filter = new AdministrativeChangeFilter(1, ruleIds.Count, null, null, null, null, null)
        {
            ItemKind = ItemKind,
            ItemIds = [.. ruleIds.Select(id => id.ToString())],
            NewestOfEachItem = true,
        };
        var read = await _audit.Read(filter, cancellationToken).ConfigureAwait(false);
        if (!read.IsSuccess || read.Value is null)
            return read.ToNewResult<Dictionary<Guid, Guid>>();

        foreach (var change in read.Value.Items)
        {
            if (Guid.TryParse(change.ItemId, out var id))
                changers[id] = change.ActorUserId;
        }

        return GenericResult<Dictionary<Guid, Guid>>.Success(changers);
    }

    // A name or an abbreviation another current, not removed rule of the column already has.
    private async Task<IGenericMessage?> Taken(Tables tables, DataConfigurationRuleContainer target, string column, string name, string abbreviation, Guid? except, CancellationToken cancellationToken)
    {
        foreach (var (field, value) in new[] { ("Name", name), ("Abbreviation", abbreviation) })
        {
            List<IFilterNode> conditions = [.. Scope(target), Equal("IsCurrent", true), Equal("IsDeleted", false), Equal("Column", column), Equal(field, value)];
            var rows = await Read(tables, tables.Rules, conditions, ["Name"], null, cancellationToken).ConfigureAwait(false);
            if (!rows.IsSuccess || rows.Value is null)
                return rows.Messages.Count > 0 ? rows.Messages[0] : DataOptionRulesLog.RuleTablesUnreadable(_logger, tables.Rules.Container, tables.Rules.DataStore);

            if (rows.Value.Any(row => except is not { } own || !row.TryGetValue("Id", out var id) || !string.Equals(id?.ToString(), own.ToString(), StringComparison.OrdinalIgnoreCase)))
                return string.Equals(field, "Name", StringComparison.Ordinal) ? DataOptionRulesLog.RuleNameTaken(_logger, value, column) : DataOptionRulesLog.RuleAbbreviationTaken(_logger, value, column);
        }

        return null;
    }

    private async Task<IGenericResult<DataConfigurationRule>> Replace(
        Tables tables, Caller caller, DataConfigurationRuleKey key, string operation, DataConfigurationRuleRecord record, IReadOnlyList<AdministrativeChangeEntry> entries, bool describes,
        CancellationToken cancellationToken)
    {
        // A change of the rule keeps its description in step in the same unit; a switch or a restore changes no text, so it writes none (FDW-6208).
        var written = await Write(
            tables,
            operation,
            async unit =>
            {
                var saved = await Save(unit, record, true, tables.Rules, cancellationToken).ConfigureAwait(false);
                return saved.IsSuccess && describes
                    ? await DescribeRule(unit, record, cancellationToken).ConfigureAwait(false)
                    : saved;
            },
            entries,
            cancellationToken).ConfigureAwait(false);
        if (written.IsFailure)
            return written.ToNewResult<DataConfigurationRule>();

        DataOptionRulesLog.Written(_logger, operation, record.Name, record.Container);
        return await Load(tables, caller, key, cancellationToken).ConfigureAwait(false);
    }

    // A new rule's rows, then its descriptions, in the one unit: the rule, its description, and for each value its row and its description (FDW-6208).
    private async Task<IGenericResult> SaveNew(
        ConfigurationWriteScope unit, Tables tables, DataConfigurationRuleRecord rule, IReadOnlyList<DataConfigurationRuleValueRecord> values, CancellationToken cancellationToken)
    {
        var saved = await Save(unit, rule, false, tables.Rules, cancellationToken).ConfigureAwait(false);
        if (!saved.IsSuccess)
            return saved;

        saved = await DescribeRule(unit, rule, cancellationToken).ConfigureAwait(false);
        return saved.IsSuccess
            ? await SaveValues(unit, tables, rule, values, cancellationToken).ConfigureAwait(false)
            : saved;
    }

    // New values of a rule: each row, then its description, in the unit (FDW-6208).
    private async Task<IGenericResult> SaveValues(
        ConfigurationWriteScope unit, Tables tables, DataConfigurationRuleRecord rule, IReadOnlyList<DataConfigurationRuleValueRecord> values, CancellationToken cancellationToken)
    {
        foreach (var value in values)
        {
            var saved = await Save(unit, value, false, tables.Values, cancellationToken).ConfigureAwait(false);
            if (!saved.IsSuccess)
                return saved;

            saved = await DescribeValue(unit, rule, value, cancellationToken).ConfigureAwait(false);
            if (!saved.IsSuccess)
                return saved;
        }

        return GenericResult.Success();
    }

    // The description of the rule, kept in step in the unit of the write that saved the rule (FDW-6208): the text the write carries is saved in the language of today, and a write
    // that carries none removes the description the rule had, so none is invented and none is left behind.
    private Task<IGenericResult> DescribeRule(ConfigurationWriteScope unit, DataConfigurationRuleRecord record, CancellationToken cancellationToken)
        => _writer.Describe(RuleDescriptionType, DescriptionSubType, record.Id, record.Name, record.Description, unit, cancellationToken);

    // The description of one value of the rule, owned by the rule and named by the value it holds (a literal or a parameter, as the audit names it); the same rule as the rule's.
    private async Task<IGenericResult> DescribeValue(ConfigurationWriteScope unit, DataConfigurationRuleRecord rule, DataConfigurationRuleValueRecord value, CancellationToken cancellationToken)
    {
        var language = _writer.Language();
        if (language.IsFailure || language.Value is not { } tag)
            return language;

        var key = new DescriptionKey(ValueDescriptionType, DescriptionSubType, value.Id, tag);
        return string.IsNullOrWhiteSpace(value.Description)
            ? await _writer.Delete(key, unit, cancellationToken).ConfigureAwait(false)
            : await _writer.Save(key, ValueText(value) ?? string.Empty, new DescriptionOwner(rule.Id, rule.Name), value.Description, unit, cancellationToken).ConfigureAwait(false);
    }

    // The order of every write: write, record, commit. The unit is a ConfigurationWriteScope opened on the store the rule tables are on, so its commit also drops the gateway's
    // cached reads of every table the unit wrote (FDW-7248): the read that follows a write is never the one kept from before it. A failure before the commit rolls everything back,
    // so a change that cannot be recorded does not take effect.
    private async Task<IGenericResult> Write(
        Tables tables, string operation, Func<ConfigurationWriteScope, Task<IGenericResult>> writes, IReadOnlyList<AdministrativeChangeEntry> entries, CancellationToken cancellationToken)
    {
        var begun = await ConfigurationWriteScope.Begin(tables.Gateway, tables.Rules.DataStore, _logger, cancellationToken).ConfigureAwait(false);
        if (!begun.IsSuccess || begun.Value is null)
            return GenericResult.Failure(Caused(DataOptionRulesLog.OptionsStoreNotTransactional(_logger, tables.Rules.DataStore), begun));

        var unit = begun.Value;
        await using (unit.ConfigureAwait(false))
        {
            var written = await writes(unit).ConfigureAwait(false);
            if (!written.IsSuccess)
            {
                await unit.Rollback(cancellationToken).ConfigureAwait(false);
                return GenericResult.Failure(Caused(DataOptionRulesLog.RuleTablesRefusedWrite(_logger, operation, tables.Rules.Container, tables.Rules.DataStore), written));
            }

            var recorded = await _audit.RecordAll(entries, cancellationToken).ConfigureAwait(false);
            if (!recorded.IsSuccess)
            {
                await unit.Rollback(cancellationToken).ConfigureAwait(false);
                return recorded;
            }

            var committed = await unit.Commit(cancellationToken).ConfigureAwait(false);
            return committed.IsSuccess
                ? committed
                : GenericResult.Failure(Caused(DataOptionRulesLog.RuleTablesRefusedWrite(_logger, operation + " (commit)", tables.Rules.Container, tables.Rules.DataStore), committed));
        }
    }

    private static List<IGenericMessage> Caused(IGenericMessage first, IGenericResult cause) => [first, .. cause.Messages];

    private static async Task<IGenericResult> Save<T>(ConfigurationWriteScope unit, T record, bool replacesCurrent, DataStoreTarget table, CancellationToken cancellationToken)
        where T : class
    {
        var saved = await unit.Write(new ConfigurationSaveCommand<T>(record, replacesCurrent), table, cancellationToken).ConfigureAwait(false);
        return saved;
    }

    private static async Task<IGenericResult> Retire(ConfigurationWriteScope unit, Guid id, DataStoreTarget table, CancellationToken cancellationToken)
    {
        var retired = await unit.Write(
            new ConfigurationRetireCommand(new Dictionary<string, object?>(StringComparer.Ordinal) { ["Id"] = id }, markDeleted: true), table, cancellationToken).ConfigureAwait(false);
        return retired;
    }

    private List<IGenericMessage> RequiredOfNew(NewDataConfigurationRule rule, string named)
    {
        var problems = new List<IGenericMessage>();
        if (string.IsNullOrWhiteSpace(rule.Name))
            problems.Add(DataOptionRulesLog.RuleNameMissing(_logger, rule.Target.Container, rule.Column ?? string.Empty));
        if (string.IsNullOrWhiteSpace(rule.Abbreviation))
            problems.Add(DataOptionRulesLog.RuleAbbreviationMissing(_logger, named));
        if (string.IsNullOrWhiteSpace(rule.Column))
            problems.Add(DataOptionRulesLog.RuleColumnMissing(_logger, named, rule.Target.Container));
        if (string.IsNullOrWhiteSpace(rule.OwnerKind) || rule.OwnerId is null)
            problems.Add(DataOptionRulesLog.OwnerMissing(_logger, named));
        else if (DataConfigurationRuleChecks.Owner(_logger, rule.OwnerKind) is { } ownerProblem)
            problems.Add(ownerProblem);
        if (rule.CustodianUserId is null)
            problems.Add(DataOptionRulesLog.CustodianMissing(_logger, named));
        if (rule.Active is null)
            problems.Add(DataOptionRulesLog.ActiveMissing(_logger, named));
        return problems;
    }

    private static string Joined(NewDataConfigurationRule rule) =>
        rule.Path is null ? string.Empty : DataConfigurationRuleRows.Joined(rule.Path, rule.Method ?? string.Empty);

    private static DataConfigurationRuleRecord RecordOf(DataConfigurationRule rule, bool active) => new()
    {
        Id = rule.Id,
        Name = rule.Name,
        Abbreviation = rule.Abbreviation,
        Description = rule.Description,
        DataStore = rule.Target.DataStore,
        DataPath = rule.Target.DataPath,
        Container = rule.Target.Container,
        Column = rule.Column,
        Path = DataConfigurationRuleRows.Joined(rule.Path, rule.Method),
        OwnerKind = rule.OwnerKind,
        OwnerId = rule.OwnerId,
        CustodianUserId = rule.CustodianUserId,
        RequiredPermission = rule.RequiredPermission,
        Active = active,
    };

    private static DataConfigurationRuleValueRecord ValueRecord(Guid ruleId, Guid id, NewDataConfigurationRuleValue value) => new()
    {
        Id = id,
        DataConfigurationRuleId = ruleId,
        Kind = value.Kind!,
        Value = value.Value,
        Parameter = value.Parameter,
        Name = value.Name,
        Abbreviation = value.Abbreviation,
        Description = value.Description,
        Ordinal = value.Ordinal!.Value,
    };

    // The text of a value for the audit is the member its kind holds: the literal for a Literal and the parameter's name for a Parameter; null when that member is not stated.
    private static string? ValueText(string kind, string? literal, string? parameter) =>
        string.Equals(DataOptionValueKinds.ByName(kind).Member, "parameter", StringComparison.Ordinal) ? parameter : literal;

    private static string? ValueText(DataConfigurationRuleValue value) => ValueText(value.Kind, value.Value, value.Parameter);

    private static string? ValueText(DataConfigurationRuleValueRecord value) => ValueText(value.Kind, value.Value, value.Parameter);

    // The audit side of a change: the level an owner kind records at (an organization's own, or the tenant's for a workspace), and one entry for a member that changed.
    private static AdministrativeChangeEntry Entry(string changeKind, DataConfigurationRuleRecord rule, string? oldValue, string? newValue)
    {
        var organization = string.Equals(rule.OwnerKind, "Organization", StringComparison.Ordinal);
        return new AdministrativeChangeEntry(
            changeKind,
            ItemKind,
            rule.Id.ToString(),
            organization ? AdministrativeLevels.ByName("Organization") : AdministrativeLevels.ByName("Tenant"),
            organization ? rule.OwnerId : null,
            oldValue,
            newValue,
            null);
    }

    private static List<AdministrativeChangeEntry> Differences(DataConfigurationRule before, DataConfigurationRuleRecord after)
    {
        var entries = new List<AdministrativeChangeEntry>();
        void Note(string kind, string? was, string? now)
        {
            if (!string.Equals(was, now, StringComparison.Ordinal))
                entries.Add(Entry(kind, after, was, now));
        }

        Note("NameChanged", before.Name, after.Name);
        Note("AbbreviationChanged", before.Abbreviation, after.Abbreviation);
        Note("DescriptionChanged", before.Description, after.Description);
        Note("ColumnChanged", before.Column, after.Column);
        Note("PathChanged", DataConfigurationRuleRows.Joined(before.Path, before.Method), after.Path);
        Note("OwnerKindChanged", before.OwnerKind, after.OwnerKind);
        Note("OwnerChanged", before.OwnerId.ToString(), after.OwnerId.ToString());
        Note("CustodianChanged", before.CustodianUserId.ToString(), after.CustodianUserId.ToString());
        Note("RequiredPermissionChanged", before.RequiredPermission, after.RequiredPermission);
        return entries;
    }

    private static IEnumerable<IFilterNode> Scope(DataConfigurationRuleContainer target) =>
    [
        Equal("DataStore", target.DataStore),
        Equal("DataPath", target.DataPath),
        Equal("Container", target.Container),
    ];

    private static FilterCondition Equal(string field, object value) => new() { PropertyName = field, Operator = FilterOperators.ByName("Equal"), Value = value };

    private static FilterCondition In(string field, IReadOnlyList<Guid> values) => new() { PropertyName = field, Operator = FilterOperators.ByName("In"), Value = values };

    private static FilterExpression And(IReadOnlyList<IFilterNode> conditions) => new() { Root = new FilterGroup { Operator = LogicalOperator.And, Nodes = conditions } };
}
