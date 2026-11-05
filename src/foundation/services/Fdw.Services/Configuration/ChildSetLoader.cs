using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Results;
using Fdw.Services.Configuration.Logging;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Configuration;

/// <summary>
/// Loads the child collections of a set of owner rows, one statement for each child table at each level, whatever the number of owners.
/// </summary>
/// <remarks>
/// <para>
/// The cascade used to read one owner at a time: a statement for each child collection of each row, and again for each row of those
/// collections that has collections of its own. A list of a few thousand rows took minutes (FDW-3906). Here the owners' physical keys go in one
/// <c>IN</c> condition on the child's Foreign key, the rows come back as the columns the store returned, and each is built into its record through
/// the mapper's coercing reader and handed to the owner whose key it holds. The same chunk of keys is the most one statement carries
/// (<see cref="ParentChain.MaxKeys"/>); a larger set is read in chunks.
/// </para>
/// <para>
/// What a level is, which rows are current, and the depth-first rule are the single-owner cascade's: a collection goes to its owner whole, grandchildren
/// first, so no reader sees it half built.
/// </para>
/// </remarks>
internal sealed class ChildSetLoader
{
    private readonly IConfigurationGatewayProvider _gateways;
    private readonly string _dataStoreName;
    private readonly ILogger _logger;
    private readonly Func<string, IGenericResult<IDataContainer>> _lookupContainer;
    private readonly Func<string, string, IGenericResult<(string ChildColumn, string OwnerColumn)>> _foreignKeyToOwner;
    private readonly Func<IChildCascadeDescriptor, string, IGenericResult<string>> _childContainer;

    /// <summary>Initializes a new instance of the <see cref="ChildSetLoader"/> class.</summary>
    /// <param name="gateways">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The configuration connection the rows live on.</param>
    /// <param name="logger">The logger a read is traced through.</param>
    /// <param name="lookupContainer">Finds a container by name in the data store's configuration.</param>
    /// <param name="foreignKeyToOwner">Finds the Foreign key a child container declares to its owner container.</param>
    /// <param name="childContainer">Finds the container a child collection's rows live in.</param>
    internal ChildSetLoader(
        IConfigurationGatewayProvider gateways,
        string dataStoreName,
        ILogger logger,
        Func<string, IGenericResult<IDataContainer>> lookupContainer,
        Func<string, string, IGenericResult<(string ChildColumn, string OwnerColumn)>> foreignKeyToOwner,
        Func<IChildCascadeDescriptor, string, IGenericResult<string>> childContainer)
    {
        _gateways = gateways;
        _dataStoreName = dataStoreName;
        _logger = logger;
        _lookupContainer = lookupContainer;
        _foreignKeyToOwner = foreignKeyToOwner;
        _childContainer = childContainer;
    }

    private sealed record ChildPlan(string ChildName, IDataContainer Child, string ChildColumn, string OwnerColumn, string? OwnerPhysical);

    private sealed record KeyedOwner(object Value, ConfigurationSetOwner Owner);

    /// <summary>Loads every child collection of every owner, to the given levels.</summary>
    /// <param name="owners">The owners; all are records of one type, in one container.</param>
    /// <param name="ownerMapper">The owners' mapper, which lists their child collections.</param>
    /// <param name="ownerContainer">The container the owners live in.</param>
    /// <param name="levels">How many hops down the cascade to load; at zero the owners are left as they are.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success, or the failure of the first collection that could not be loaded.</returns>
    internal async Task<IGenericResult> Load(
        IReadOnlyList<ConfigurationSetOwner> owners, IPocoMapper ownerMapper, string ownerContainer, int levels, CancellationToken cancellationToken)
    {
        if (owners.Count == 0 || levels <= 0)
            return GenericResult.Success();

        // A list of the owner's own type is a tree, attached by the domain provider (see ConfigurationTrees); it is not a child table.
        var ownerType = owners[0].Row.GetType();
        foreach (var descriptor in ownerMapper.CascadeChildren.Where(d => !ConfigurationTrees.IsTree(d, ownerType)))
        {
            var loaded = await LoadChild(owners, ownerMapper, ownerContainer, descriptor, levels, cancellationToken).ConfigureAwait(false);
            if (loaded.IsFailure)
                return loaded;
        }

        return GenericResult.Success();
    }

    /// <summary>Builds the records of a container's rows through the mapper's coercing reader, in the rows' order.</summary>
    /// <param name="mapper">The mapper of the record the container holds.</param>
    /// <param name="container">The container the rows were read from.</param>
    /// <param name="rows">The rows.</param>
    /// <returns>One record for each row, or the failure of the first row that could not be built.</returns>
    internal IGenericResult<List<object>> Materialize(IPocoMapper mapper, IDataContainer container, IReadOnlyList<ConfigurationRow> rows)
    {
        var items = new List<object>(rows.Count);
        using var reader = new RecordDictionaryReader(rows.Select(row => row.Columns).ToList());
        while (reader.Read())
        {
            var built = mapper.MapFromReader(reader, container);
            if (built.IsFailure)
                return built.ToNewResult<List<object>>();

            if (built.Value is null)
                return GenericResult<List<object>>.Failure(DefaultConfigurationProviderLog.SetRowNotBuilt(_logger, container.Name));

            items.Add(built.Value);
        }

        return GenericResult<List<object>>.Success(items);
    }

    /// <summary>The text a key is compared by: the same for the key in an owner and in the column of a child that holds it.</summary>
    /// <param name="value">The key as the store returned it.</param>
    /// <returns>The key as text, in the invariant culture.</returns>
    internal static string KeyText(object? value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

    private async Task<IGenericResult> LoadChild(
        IReadOnlyList<ConfigurationSetOwner> owners,
        IPocoMapper ownerMapper,
        string ownerContainer,
        IChildCascadeDescriptor descriptor,
        int levels,
        CancellationToken cancellationToken)
    {
        var ownerTypeName = owners[0].Row.GetType().Name;
        var plan = Plan(ownerTypeName, ownerContainer, descriptor);
        if (plan.IsFailure)
            return plan;

        var keyed = KeyOwners(owners, ownerMapper, ownerContainer, plan.Value!);
        if (keyed.IsFailure)
            return keyed;

        var rows = await Read(plan.Value!, keyed.Value!, cancellationToken).ConfigureAwait(false);
        if (rows.IsFailure)
            return rows;

        return descriptor.IsPropertyCollection
            ? Dictionaries(descriptor, keyed.Value!, rows.Value!, plan.Value!)
            : await Lists(descriptor, keyed.Value!, rows.Value!, plan.Value!, ownerTypeName, levels, cancellationToken).ConfigureAwait(false);
    }

    // Where a child collection lives and how it hangs from its owner: its container, the Foreign key it declares to the owner's container, and
    // the owner's physical key field (the one the Foreign key holds when it holds the row's own key and not its durable Id).
    private IGenericResult<ChildPlan> Plan(string ownerTypeName, string ownerContainer, IChildCascadeDescriptor descriptor)
    {
        var childName = _childContainer(descriptor, ownerTypeName);
        if (childName.IsFailure)
            return childName.ToNewResult<ChildPlan>();

        var join = _foreignKeyToOwner(ownerContainer, childName.Value!);
        if (join.IsFailure)
            return join.ToNewResult<ChildPlan>();

        var child = _lookupContainer(childName.Value!);
        if (child.IsFailure)
            return child.ToNewResult<ChildPlan>();

        var owner = _lookupContainer(ownerContainer);
        if (owner.IsFailure)
            return owner.ToNewResult<ChildPlan>();

        return GenericResult<ChildPlan>.Success(new ChildPlan(
            childName.Value!, child.Value!, join.Value.ChildColumn, join.Value.OwnerColumn, ConfigurationKeys.FieldOf(owner.Value!.Keys, "Physical")));
    }

    // Each owner under the key its children's Foreign key holds. Two owners under one key would hand a child to both.
    private IGenericResult<Dictionary<string, KeyedOwner>> KeyOwners(
        IReadOnlyList<ConfigurationSetOwner> owners, IPocoMapper ownerMapper, string ownerContainer, ChildPlan plan)
    {
        var keyed = new Dictionary<string, KeyedOwner>(StringComparer.Ordinal);
        foreach (var owner in owners)
        {
            var value = OwnerValue(owner, ownerMapper, plan);
            if (value is null)
                return GenericResult<Dictionary<string, KeyedOwner>>.Failure(
                    DefaultConfigurationProviderLog.OwnerLogicalKeyMissing(_logger, owner.Row.GetType().Name, ownerContainer, plan.OwnerColumn));

            if (!keyed.TryAdd(KeyText(value), new KeyedOwner(value, owner)))
                return GenericResult<Dictionary<string, KeyedOwner>>.Failure(
                    DefaultConfigurationProviderLog.SetOwnerKeyRepeated(_logger, ownerContainer, KeyText(value)));
        }

        return GenericResult<Dictionary<string, KeyedOwner>>.Success(keyed);
    }

    private static object? OwnerValue(ConfigurationSetOwner owner, IPocoMapper ownerMapper, ChildPlan plan)
    {
        if (string.Equals(plan.OwnerColumn, plan.OwnerPhysical, StringComparison.Ordinal))
            return owner.RowId;

        return ownerMapper.MapToParameters(owner.Row).TryGetValue(plan.OwnerColumn, out var value) ? value : null;
    }

    // The current rows whose Foreign key holds one of the owners' keys, a chunk of keys to a statement.
    private async Task<IGenericResult<List<ConfigurationRow>>> Read(
        ChildPlan plan, Dictionary<string, KeyedOwner> keyed, CancellationToken cancellationToken)
    {
        var reader = new ConfigurationRowReader(_gateways, _dataStoreName, plan.Child.Parent.Name, plan.Child.Name, _logger);
        var rows = new List<ConfigurationRow>();
        foreach (var chunk in keyed.Values.Select(entry => entry.Value).Chunk(ParentChain.MaxKeys))
        {
            var read = await reader.All(
                new FilterCondition { PropertyName = plan.ChildColumn, Operator = FilterOperators.ByName("In"), Value = chunk.ToList() },
                cancellationToken).ConfigureAwait(false);
            if (read.IsFailure)
                return read.ToNewResult<List<ConfigurationRow>>();

            rows.AddRange(read.Value!);
        }

        DefaultConfigurationProviderLog.SetChildrenRead(_logger, plan.ChildName, keyed.Count, rows.Count);
        return GenericResult<List<ConfigurationRow>>.Success(rows);
    }

    // The key of the owner a child row hangs from, as text.
    private IGenericResult<string> OwnerKeyOf(ConfigurationRow row, ChildPlan plan)
        => row.Columns.TryGetValue(plan.ChildColumn, out var value) && value is not null and not DBNull
            ? GenericResult<string>.Success(KeyText(value))
            : GenericResult<string>.Failure(DefaultConfigurationProviderLog.SetChildKeyUnreadable(_logger, plan.ChildName, plan.ChildColumn));

    // A name/value collection: the rows of an owner become its dictionary. An owner with no rows gets an empty one, as a single-owner read gives.
    private IGenericResult Dictionaries(
        IChildCascadeDescriptor descriptor, Dictionary<string, KeyedOwner> keyed, List<ConfigurationRow> rows, ChildPlan plan)
    {
        var bags = keyed.Keys.ToDictionary(key => key, _ => new Dictionary<string, string?>(StringComparer.Ordinal), StringComparer.Ordinal);
        foreach (var row in rows)
        {
            var key = OwnerKeyOf(row, plan);
            if (key.IsFailure)
                return key;

            if (!bags.TryGetValue(key.Value!, out var bag))
                return GenericResult.Failure(DefaultConfigurationProviderLog.SetChildOrphaned(_logger, plan.ChildName, key.Value!));

            if (row.Text("Name") is { Length: > 0 } name)
                bag[name] = row.Text("Value");
        }

        foreach (var entry in keyed)
            descriptor.FillDictionary(entry.Value.Owner.Row, bags[entry.Key]);

        return GenericResult.Success();
    }

    // A typed list: the rows are built into records, the records' own collections are loaded (depth first), then each owner takes its list whole.
    private async Task<IGenericResult> Lists(
        IChildCascadeDescriptor descriptor,
        Dictionary<string, KeyedOwner> keyed,
        List<ConfigurationRow> rows,
        ChildPlan plan,
        string ownerTypeName,
        int levels,
        CancellationToken cancellationToken)
    {
        var childMapper = PocoMapperCollection.ByName(descriptor.ChildTypeName);
        if (childMapper == PocoMapperCollection.NotFound)
            return GenericResult.Failure(DefaultConfigurationProviderLog.ChildMapperMissing(_logger, descriptor.ChildTypeName, ownerTypeName));

        var items = Materialize(childMapper, plan.Child, rows);
        if (items.IsFailure)
            return items;

        var grouped = Group(keyed, rows, items.Value!, plan);
        if (grouped.IsFailure)
            return grouped;

        var deeper = await LoadGrandchildren(childMapper, descriptor, plan, rows, items.Value!, levels - 1, cancellationToken).ConfigureAwait(false);
        if (deeper.IsFailure)
            return deeper;

        foreach (var entry in keyed)
        {
            var list = childMapper.CreateList();
            foreach (var item in grouped.Value![entry.Key])
                list.Add(item);

            descriptor.SetCollection(entry.Value.Owner.Row, list);
        }

        return GenericResult.Success();
    }

    private IGenericResult<Dictionary<string, List<object>>> Group(
        Dictionary<string, KeyedOwner> keyed, List<ConfigurationRow> rows, List<object> items, ChildPlan plan)
    {
        var grouped = keyed.Keys.ToDictionary(key => key, _ => new List<object>(), StringComparer.Ordinal);
        for (var i = 0; i < rows.Count; i++)
        {
            var key = OwnerKeyOf(rows[i], plan);
            if (key.IsFailure)
                return key.ToNewResult<Dictionary<string, List<object>>>();

            if (!grouped.TryGetValue(key.Value!, out var list))
                return GenericResult<Dictionary<string, List<object>>>.Failure(
                    DefaultConfigurationProviderLog.SetChildOrphaned(_logger, plan.ChildName, key.Value!));

            list.Add(items[i]);
        }

        return GenericResult<Dictionary<string, List<object>>>.Success(grouped);
    }

    // The records just built are owners in their turn, under the physical key of their own row. A record with no collections of its own, or a
    // level past the last, needs none of this and its container need not declare a physical key.
    private async Task<IGenericResult> LoadGrandchildren(
        IPocoMapper childMapper,
        IChildCascadeDescriptor descriptor,
        ChildPlan plan,
        List<ConfigurationRow> rows,
        List<object> items,
        int levels,
        CancellationToken cancellationToken)
    {
        if (levels <= 0 || !childMapper.CascadeChildren.Any(d => !ConfigurationTrees.IsTree(d, descriptor.ChildType)))
            return GenericResult.Success();

        var physical = ConfigurationKeys.FieldOf(plan.Child.Keys, "Physical");
        if (physical is null)
            return GenericResult.Failure(DefaultConfigurationProviderLog.ContainerKeyNotDeclared(_logger, plan.ChildName, "Physical"));

        var owners = new List<ConfigurationSetOwner>(items.Count);
        for (var i = 0; i < items.Count; i++)
        {
            if (rows[i].Number(physical) is not { } rowId)
                return GenericResult.Failure(DefaultConfigurationProviderLog.SetChildKeyUnreadable(_logger, plan.ChildName, physical));

            owners.Add(new ConfigurationSetOwner(items[i], rowId));
        }

        return await Load(owners, childMapper, plan.ChildName, levels, cancellationToken).ConfigureAwait(false);
    }
}
