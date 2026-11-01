using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration.Logging;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using CmdBuilders = Fdw.Commands.Data.Extensions;

namespace Fdw.Services.Configuration;

/// <summary>
/// Supplies the configured members of one domain whose list shows nothing of a member.
/// </summary>
/// <typeparam name="TImplementationConfiguration">The domain's implementation contract.</typeparam>
/// <remarks>
/// The domain record is the four properties every domain has. A domain whose members are listed
/// declares a record of its own and derives from <see cref="DomainConfigurationProviderBase{TImplementationConfiguration, TDomain}"/>
/// instead; a domain that does not derives from this and has no logic to put in it.
/// </remarks>
public abstract class DomainConfigurationProviderBase<TImplementationConfiguration>
    : DomainConfigurationProviderBase<TImplementationConfiguration, DomainConfiguration>
    where TImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Initializes the provider.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto <paramref name="dataStoreName"/>.</param>
    /// <param name="dataStoreName">The configuration connection the rows live on.</param>
    /// <param name="pathName">The schema the rows live in.</param>
    /// <param name="tableName">The domain table.</param>
    protected DomainConfigurationProviderBase(
        ILogger<DomainConfigurationProviderBase<TImplementationConfiguration>>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string tableName)
        : base(logger, gatewayProvider, dataStoreName, pathName, tableName)
    {
    }
}

/// <summary>
/// Supplies the configured members of one domain.
/// </summary>
/// <typeparam name="TImplementationConfiguration">The domain's implementation contract.</typeparam>
/// <typeparam name="TDomain">
/// The domain's record: <see cref="DomainConfiguration"/> for a domain that shows no fields in its
/// list, or a record derived from it that adds the fields its list shows.
/// </typeparam>
/// <remarks>
/// A domain deals with more than one kind of implementation; that is what makes it a domain. Every
/// read of a member is the same three steps written out: read the domain row, take the implementation
/// it names and its own Id, and pass them to the provider registered under that name. The domain
/// record is how the implementation is found, not the answer.
/// <para>
/// Except for a list. The fields a list shows live on the domain row, so <see cref="Get(DomainQuery, CancellationToken)"/>
/// answers a page from the domain table alone -- one query, no implementation consulted. Those fields
/// are the record's properties beyond the four every domain has, found from its mapper rather than
/// named here. Reading a member stamps them onto its implementation; saving a member whose values
/// differ writes a new version of the domain row from them, so the two never disagree.
/// </para>
/// <para>
/// A derived provider supplies a name, a data store, a schema and a table, and nothing else. There
/// is no logic to put in one.
/// </para>
/// </remarks>
public abstract class DomainConfigurationProviderBase<TImplementationConfiguration, TDomain>
    : IDomainConfigurationProvider,
      IDomainConfigurationProvider<TImplementationConfiguration>,
      IDomainRecordProvider<TDomain>
    where TImplementationConfiguration : IImplementationConfiguration
    where TDomain : DomainConfiguration, new()
{
    private readonly ILogger<DomainConfigurationProviderBase<TImplementationConfiguration, TDomain>> _logger;
    private readonly IConfigurationGatewayProvider _gatewayProvider;
    private readonly DomainConfigurationCommand<TDomain> _commands;

    // Keyed by the value a domain row carries in its Implementation column.
    private readonly ConcurrentDictionary<string, IImplementationConfigurationProvider<TImplementationConfiguration>> _implementations
        = new(StringComparer.OrdinalIgnoreCase);

    // The record's properties beyond the four every domain has: derived once from its mapper, and the
    // same answer every time.
    private IReadOnlyList<string>? _listFields;

    // SQL Server allows 2,100 parameters per statement and every id is one; a thousand leaves room for the
    // rest of the statement, and is the same ceiling a query's In operator has.
    private const int MaxIds = 1000;

    // How many of the rows an ambiguous name matched are listed in the refusal: enough to address each by id.
    private const int MaxAmbiguousIds = 10;

    /// <summary>Gets the configuration connection this domain's rows live on.</summary>
    public string DataStoreName { get; private set; }

    /// <summary>Gets the schema this domain's rows live in.</summary>
    public string PathName { get; private set; }

    /// <summary>Initializes the provider.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto <paramref name="dataStoreName"/>.</param>
    /// <param name="dataStoreName">The configuration connection the rows live on.</param>
    /// <param name="pathName">The schema the rows live in.</param>
    /// <param name="tableName">The domain table.</param>
    protected DomainConfigurationProviderBase(
        ILogger<DomainConfigurationProviderBase<TImplementationConfiguration, TDomain>>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string tableName)
    {
        _logger = logger ?? NullLogger<DomainConfigurationProviderBase<TImplementationConfiguration, TDomain>>.Instance;
        _gatewayProvider = gatewayProvider ?? throw new ArgumentNullException(nameof(gatewayProvider));
        DataStoreName = dataStoreName ?? throw new ArgumentNullException(nameof(dataStoreName));
        PathName = pathName ?? throw new ArgumentNullException(nameof(pathName));
        _commands = new DomainConfigurationCommand<TDomain>(tableName ?? throw new ArgumentNullException(nameof(tableName)));
    }


    /// <inheritdoc/>
    public IGenericResult Register<T>(string name, T implementationConfigurationProvider)
    {
        if (implementationConfigurationProvider is not IImplementationConfigurationProvider<TImplementationConfiguration> typed)
        {
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.ProviderNotErasable(
                    _logger, name, implementationConfigurationProvider?.GetType().FullName ?? "(null)"));
        }

        if (typed is IConfigurationWriteParticipant<TImplementationConfiguration> participant)
            participant.AttachDomain(_commands.TableName);

        _implementations[name] = typed;
        DefaultConfigurationProviderLog.TypedProviderRegistered(_logger, typeof(TImplementationConfiguration).Name, name);
        return GenericResult.Success();
    }

    /// <inheritdoc/>
    public IGenericResult<IImplementationConfigurationProvider<TImplementationConfiguration>> Implementation(string implementationName)
        => _implementations.TryGetValue(implementationName, out var provider)
            ? GenericResult<IImplementationConfigurationProvider<TImplementationConfiguration>>.Success(provider)
            : GenericResult<IImplementationConfigurationProvider<TImplementationConfiguration>>.Failure(
                DefaultConfigurationProviderLog.ImplementationNotRegistered(_logger, typeof(TImplementationConfiguration).Name, implementationName));

    /// <inheritdoc/>
    public Task<IGenericResult<TImplementationConfiguration>> Get(string name, CancellationToken ct = default)
        => Get(name, ConfigurationLevels.WholeAggregate, ct);

    /// <inheritdoc/>
    public async Task<IGenericResult<TImplementationConfiguration>> Get(string name, int levels, CancellationToken ct = default)
    {
        if (levels < 0)
            return GenericResult<TImplementationConfiguration>.Failure(
                DefaultConfigurationProviderLog.ReadArgumentOutOfRange(_logger, typeof(TImplementationConfiguration).Name, nameof(levels), levels));

        var row = await DomainRow(_commands.Get(DataStoreName, PathName, name), name, ct).ConfigureAwait(false);
        if (row.IsFailure) return row.ToNewResult<TImplementationConfiguration>();
        return row.Value is not { } member
            ? GenericResult<TImplementationConfiguration>.Success(default!)
            : await Compose(member, provider => provider.Get(member.Id, levels, ct), levels, null, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<IGenericResult<TImplementationConfiguration>> Get(Guid id, CancellationToken ct = default)
        => Get(id, ConfigurationLevels.WholeAggregate, ct);

    /// <inheritdoc/>
    public async Task<IGenericResult<TImplementationConfiguration>> Get(Guid id, int levels, CancellationToken ct = default)
    {
        if (levels < 0)
            return GenericResult<TImplementationConfiguration>.Failure(
                DefaultConfigurationProviderLog.ReadArgumentOutOfRange(_logger, typeof(TImplementationConfiguration).Name, nameof(levels), levels));

        var row = await DomainRow(_commands.Get(DataStoreName, PathName, id), id.ToString(), ct).ConfigureAwait(false);
        if (row.IsFailure) return row.ToNewResult<TImplementationConfiguration>();
        return row.Value is not { } member
            ? GenericResult<TImplementationConfiguration>.Success(default!)
            : await Compose(member, provider => provider.Get(member.Id, levels, ct), levels, null, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<TImplementationConfiguration>> Get(Guid id, DateTimeOffset asOf, CancellationToken ct = default)
    {
        var row = await DomainRow(_commands.Get(DataStoreName, PathName, id, asOf), id.ToString(), ct).ConfigureAwait(false);
        if (row.IsFailure) return row.ToNewResult<TImplementationConfiguration>();

        // The instant reaches the implementation read too, or the answer mixes two versions.
        return row.Value is not { } member
            ? GenericResult<TImplementationConfiguration>.Success(default!)
            : await Compose(member, provider => provider.Get(member.Id, asOf, ct), ConfigurationLevels.WholeAggregate, asOf, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The page's domain rows are read in one query, with one row past the page: that row is how the
    /// page knows another follows, without counting the domain. Each member is composed only when the
    /// caller reaches it, so a caller streaming the page to a client holds one composed member at a time.
    /// </remarks>
    public async Task<IGenericResult<ConfigurationPage<TImplementationConfiguration>>> Get(int skip, int take, int levels, CancellationToken ct = default)
    {
        if (skip < 0)
            return GenericResult<ConfigurationPage<TImplementationConfiguration>>.Failure(
                DefaultConfigurationProviderLog.ReadArgumentOutOfRange(_logger, typeof(TImplementationConfiguration).Name, nameof(skip), skip));
        if (take is < 1 or int.MaxValue)
            return GenericResult<ConfigurationPage<TImplementationConfiguration>>.Failure(
                DefaultConfigurationProviderLog.ReadArgumentOutOfRange(_logger, typeof(TImplementationConfiguration).Name, nameof(take), take));
        if (levels < 0)
            return GenericResult<ConfigurationPage<TImplementationConfiguration>>.Failure(
                DefaultConfigurationProviderLog.ReadArgumentOutOfRange(_logger, typeof(TImplementationConfiguration).Name, nameof(levels), levels));

        var gateway = _gatewayProvider.Get(DataStoreName);
        if (gateway.IsFailure) return gateway.ToNewResult<ConfigurationPage<TImplementationConfiguration>>();

        var rows = await gateway.Value!.Execute<IEnumerable<TDomain>>(
            _commands.List(DataStoreName, PathName, skip, take + 1), Target, ct).ConfigureAwait(false);
        if (!rows.IsSuccess) return rows.ToNewResult<ConfigurationPage<TImplementationConfiguration>>();
        if (rows.Value is null)
            return GenericResult<ConfigurationPage<TImplementationConfiguration>>.Failure(
                DefaultConfigurationProviderLog.DomainPageReturnedNoResult(_logger, typeof(TImplementationConfiguration).Name, skip, take));

        var read = rows.Value.ToList();
        return GenericResult<ConfigurationPage<TImplementationConfiguration>>.Success(new ConfigurationPage<TImplementationConfiguration>(
            skip, take, read.Count > take, fail => Members(read.Take(take).ToList(), levels, fail, ct)));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// One query answers the page: the store filters, orders and pages the domain table, and no
    /// implementation is read. The page asks for one row past the take, which is how it knows another
    /// follows; a total is a second query, made only when the caller asked. The ordering is completed
    /// with the container's declared keys, read from its configuration, so consecutive pages neither
    /// overlap nor skip a row.
    /// </remarks>
    public async Task<IGenericResult<ConfigurationPage<TDomain>>> Get(DomainQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Skip < 0)
            return GenericResult<ConfigurationPage<TDomain>>.Failure(
                DefaultConfigurationProviderLog.ReadArgumentOutOfRange(_logger, typeof(TDomain).Name, nameof(query.Skip), query.Skip));
        if (query.Take is < 1 or int.MaxValue)
            return GenericResult<ConfigurationPage<TDomain>>.Failure(
                DefaultConfigurationProviderLog.ReadArgumentOutOfRange(_logger, typeof(TDomain).Name, nameof(query.Take), query.Take));

        var keys = KeyFields();
        if (keys.IsFailure) return keys.ToNewResult<ConfigurationPage<TDomain>>();

        var gateway = _gatewayProvider.Get(DataStoreName);
        if (gateway.IsFailure) return gateway.ToNewResult<ConfigurationPage<TDomain>>();

        var rows = await gateway.Value!.Execute<IEnumerable<TDomain>>(
            _commands.Get(DataStoreName, PathName, query, keys.Value!), Target, ct).ConfigureAwait(false);
        if (!rows.IsSuccess) return rows.ToNewResult<ConfigurationPage<TDomain>>();
        if (rows.Value is null)
            return GenericResult<ConfigurationPage<TDomain>>.Failure(
                DefaultConfigurationProviderLog.DomainPageReturnedNoResult(_logger, typeof(TDomain).Name, query.Skip, query.Take));

        var total = await Total(gateway.Value!, query, ct).ConfigureAwait(false);
        if (total.IsFailure) return total.ToNewResult<ConfigurationPage<TDomain>>();

        var read = rows.Value.ToList();
        DefaultConfigurationProviderLog.DomainPageRead(
            _logger, typeof(TDomain).Name, query.Skip, query.Take, Math.Min(read.Count, query.Take),
            total.Value is { } counted ? counted.ToString(CultureInfo.InvariantCulture) : "not asked");
        return GenericResult<ConfigurationPage<TDomain>>.Success(new ConfigurationPage<TDomain>(
            query.Skip, query.Take, read.Count > query.Take, _ => read.Take(query.Take).ToAsyncEnumerable(), total.Value));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// One query answers it: the domain table filtered by <c>Id IN (...)</c>, read as the caller, so a member
    /// row-level security hides is absent from the answer rather than an error. No implementation is read.
    /// </remarks>
    public async Task<IGenericResult<IReadOnlyList<TDomain>>> Get(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        var wanted = ids.Distinct().ToList();
        if (wanted.Count == 0)
            return GenericResult<IReadOnlyList<TDomain>>.Success(Array.Empty<TDomain>());
        if (wanted.Count > MaxIds)
            return GenericResult<IReadOnlyList<TDomain>>.Failure(
                DefaultConfigurationProviderLog.ReadArgumentOutOfRange(_logger, typeof(TDomain).Name, nameof(ids), wanted.Count));

        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = nameof(DomainConfiguration.Id),
                Operator = FilterOperators.ByName("In"),
                Value = wanted.Cast<object?>().ToList(),
            },
        };
        var page = await Get(new DomainQuery(filter, null, 0, wanted.Count, false), ct).ConfigureAwait(false);
        if (!page.IsSuccess) return page.ToNewResult<IReadOnlyList<TDomain>>();

        var found = await page.Value!.Items.ToListAsync(ct).ConfigureAwait(false);
        return page.Value.Outcome.IsFailure
            ? page.Value.Outcome.ToNewResult<IReadOnlyList<TDomain>>()
            : GenericResult<IReadOnlyList<TDomain>>.Success(found);
    }

    // How many members the query selects in all, or nothing when the caller did not ask for a count.
    private async Task<IGenericResult<int?>> Total(IConfigurationGateway gateway, DomainQuery query, CancellationToken ct)
    {
        if (!query.IncludeTotal)
            return GenericResult<int?>.Success(null);

        var counted = await gateway.Execute<IEnumerable<Dictionary<string, object?>>>(
            _commands.Count(DataStoreName, PathName, query), Target, ct).ConfigureAwait(false);
        if (!counted.IsSuccess) return counted.ToNewResult<int?>();

        return counted.Value?.FirstOrDefault() is { } row
               && row.TryGetValue(ConfigurationCommandBase<TDomain>.CountColumn, out var number)
               && number is IConvertible convertible
            ? GenericResult<int?>.Success(convertible.ToInt32(CultureInfo.InvariantCulture))
            : GenericResult<int?>.Failure(DefaultConfigurationProviderLog.CountReturnedNoResult(_logger, _commands.TableName));
    }

    // The fields the container declares as its own keys, which complete a caller's ordering.
    private IGenericResult<IReadOnlyList<string>> KeyFields()
    {
        var gateway = _gatewayProvider.Get(DataStoreName);
        if (gateway.IsFailure) return gateway.ToNewResult<IReadOnlyList<string>>();

        var path = gateway.Value!.DataStores
            .FirstOrDefault(store => string.Equals(store.Name, DataStoreName, StringComparison.Ordinal))?
            .Path(PathName);
        if (path is null || !path.IsSuccess || path.Value is null)
            return GenericResult<IReadOnlyList<string>>.Failure(
                DefaultConfigurationProviderLog.ContainerNotFoundInStore(_logger, typeof(TDomain).Name, _commands.TableName, DataStoreName));

        var container = path.Value.Container(_commands.TableName);
        if (!container.IsSuccess || container.Value is null)
            return GenericResult<IReadOnlyList<string>>.Failure(
                DefaultConfigurationProviderLog.ContainerNotFoundInStore(_logger, typeof(TDomain).Name, _commands.TableName, DataStoreName));

        var fields = new List<string>();
        foreach (var key in container.Value.Keys)
        {
            // A key that references another container is a foreign key, which says nothing of this row's identity.
            if (key.ReferencedContainer is not null) continue;
            foreach (var keyField in key.KeyFields)
            {
                if (!fields.Contains(keyField.LocalField.Name, StringComparer.Ordinal))
                    fields.Add(keyField.LocalField.Name);
            }
        }

        return fields.Count == 0
            ? GenericResult<IReadOnlyList<string>>.Failure(
                DefaultConfigurationProviderLog.ContainerKeyNotDeclared(_logger, _commands.TableName, "Physical"))
            : GenericResult<IReadOnlyList<string>>.Success(fields);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// A member that does not exist is a failure here, not an empty collection: an empty list would say
    /// the member has none, about a member that is not there.
    /// </remarks>
    public async Task<IGenericResult<IReadOnlyList<TChild>>> Get<TChild>(string name, int levels, CancellationToken ct = default)
    {
        var provider = await MemberProvider(name, ct).ConfigureAwait(false);
        if (provider.IsFailure) return provider.ToNewResult<IReadOnlyList<TChild>>();

        var (member, implementation) = provider.Value;
        return await implementation.Get<TImplementationConfiguration, TChild>(member.Id, levels, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The owner is identified by its id alone; the member's name selects the implementation that
    /// reads it. A member that does not exist is a failure, as it is for the member's own collections.
    /// </remarks>
    public async Task<IGenericResult<IReadOnlyList<TChild>>> Get<TOwner, TChild>(string name, Guid ownerId, int levels, CancellationToken ct = default)
    {
        var provider = await MemberProvider(name, ct).ConfigureAwait(false);
        if (provider.IsFailure) return provider.ToNewResult<IReadOnlyList<TChild>>();

        return await provider.Value.Implementation.Get<TOwner, TChild>(ownerId, levels, ct).ConfigureAwait(false);
    }

    // The members of one page, composed one at a time as the caller enumerates them. The first member
    // that cannot be composed ends the page and is reported through the page's outcome.
    private async IAsyncEnumerable<TImplementationConfiguration> Members(
        List<TDomain> rows,
        int levels,
        Action<IGenericResult> fail,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var composed = 0;
        foreach (var row in rows)
        {
            var loaded = await Compose(row, provider => provider.Get(row.Id, levels, ct), levels, null, ct).ConfigureAwait(false);
            if (loaded.IsSuccess)
            {
                if (loaded.Value is null) continue;
                composed++;
                yield return loaded.Value;
                continue;
            }

            fail(loaded);
            yield break;
        }

        DefaultConfigurationProviderLog.DomainListComposed(_logger, typeof(TImplementationConfiguration).Name, rows.Count, composed);
    }

    // The current domain row a command selects, or none. None is not a failure here: a read by name
    // or id answers "not configured" with an empty success, and the caller decides what that means.
    private async Task<IGenericResult<TDomain?>> DomainRow(IDataCommand command, string key, CancellationToken ct)
    {
        var gateway = _gatewayProvider.Get(DataStoreName);
        if (gateway.IsFailure) return gateway.ToNewResult<TDomain?>();

        var rows = await gateway.Value!.Execute<IEnumerable<TDomain>>(command, Target, ct).ConfigureAwait(false);
        if (!rows.IsSuccess) return rows.ToNewResult<TDomain?>();
        var single = OnlyRow(rows.Value, key);
        if (single.IsFailure || single.Value is not null) return single;

        DefaultConfigurationProviderLog.DomainRowNotFound(_logger, typeof(TImplementationConfiguration).Name, key);
        return GenericResult<TDomain?>.Success(null);
    }

    // The one current row a key selects, none, or a refusal when the caller can see more than one. A name is unique
    // within a tenant, and a caller can see several tenants' rows (shared rows from every tenant, every tenant's from
    // the global one), so the first row back is not necessarily the one asked for; acting on it would act on a
    // configuration the caller did not choose (FDW-1101).
    private IGenericResult<TDomain?> OnlyRow(IEnumerable<TDomain>? rows, string key)
    {
        // A read that answers with no row set has selected nothing, the same answer as an empty one.
        if (rows is null)
            return GenericResult<TDomain?>.Success(null);

        var found = rows.ToList();
        if (found.Count <= 1)
            return GenericResult<TDomain?>.Success(found.Count == 1 ? found[0] : null);

        return GenericResult<TDomain?>.Failure(
            ServicesResultCodes.ByName("ConfigurationNameAmbiguous"), _logger,
            ResultDetails.Create("type", typeof(TImplementationConfiguration).Name, "name", key, "count", found.Count)
                .With("ids", string.Join(", ", found.Take(MaxAmbiguousIds).Select(row => row.Id.ToString("D", CultureInfo.InvariantCulture)))));
    }

    // The named member's domain row and the implementation provider it names, for a read of part of
    // the member. Unlike a read of the whole member, the member must exist.
    private async Task<IGenericResult<(TDomain Member, IImplementationConfigurationProvider<TImplementationConfiguration> Implementation)>> MemberProvider(
        string name, CancellationToken ct)
    {
        var row = await DomainRow(_commands.Get(DataStoreName, PathName, name), name, ct).ConfigureAwait(false);
        if (row.IsFailure) return row.ToNewResult<(TDomain, IImplementationConfigurationProvider<TImplementationConfiguration>)>();
        if (row.Value is not { } member)
            return GenericResult<(TDomain, IImplementationConfigurationProvider<TImplementationConfiguration>)>.Failure(
                DefaultConfigurationProviderLog.ConfigurationNotFound(_logger, typeof(TImplementationConfiguration).Name, name));

        var provider = Provider(member);
        return provider.IsFailure
            ? provider.ToNewResult<(TDomain, IImplementationConfigurationProvider<TImplementationConfiguration>)>()
            : GenericResult<(TDomain, IImplementationConfigurationProvider<TImplementationConfiguration>)>.Success((member, provider.Value!));
    }

    private IGenericResult<IImplementationConfigurationProvider<TImplementationConfiguration>> Provider(TDomain row)
        => row.Implementation is { } implementation && _implementations.TryGetValue(implementation, out var provider)
            ? GenericResult<IImplementationConfigurationProvider<TImplementationConfiguration>>.Success(provider)
            : GenericResult<IImplementationConfigurationProvider<TImplementationConfiguration>>.Failure(
                DefaultConfigurationProviderLog.NoImplementationProvider(_logger, row.Name, row.Implementation ?? "(none)"));

    // Reads a domain row's implementation through the provider the row names, then stamps the row's
    // identity onto it.
    private async Task<IGenericResult<TImplementationConfiguration>> Compose(
        TDomain row,
        Func<IImplementationConfigurationProvider<TImplementationConfiguration>, Task<IGenericResult<TImplementationConfiguration>>> read,
        int levels,
        DateTimeOffset? asOf,
        CancellationToken ct,
        bool attachTrees = true)
    {
        var provider = Provider(row);
        if (provider.IsFailure) return provider.ToNewResult<TImplementationConfiguration>();

        var loaded = await read(provider.Value!).ConfigureAwait(false);
        if (loaded is { IsSuccess: true, Value: not null })
        {
            // Id with the other three: Save stamps the domain row's Id onto the implementation, and
            // every foreign key that names a member -- UserRole.UserId, RolePermission.RoleId -- holds
            // that domain Id. Leaving the implementation row's own Id here made a read disagree with
            // the write and with every reference to the member, so a user resolved for login was
            // verified, tenanted and made a principal under an Id nothing else knew.
            loaded.Value.Id = row.Id;
            loaded.Value.Name = row.Name;
            loaded.Value.Domain = row.Domain;
            loaded.Value.Implementation = row.Implementation!;

            // The list fields are read from the domain row, not the implementation: the
            // implementation's table no longer holds them, so nothing else would set them.
            var stamped = StampListFields(row, loaded.Value);
            if (stamped.IsFailure) return stamped.ToNewResult<TImplementationConfiguration>();

            // The members of a tree are members of this domain, so they can only be hung once this one's identity is stamped.
            if (attachTrees)
            {
                var hung = await AttachTrees(loaded.Value, levels, asOf, ct).ConfigureAwait(false);
                if (hung.IsFailure) return hung.ToNewResult<TImplementationConfiguration>();
            }

            DefaultConfigurationProviderLog.DomainRowComposed(
                _logger, typeof(TImplementationConfiguration).Name, row.Name, row.Id, row.Implementation!);
        }

        return loaded;
    }

    // A record that holds a list of its own type is a tree (see ConfigurationTrees): its members are the other members of this
    // domain whose parent link names it. Every member is composed once, flat, and hung in memory.
    private async Task<IGenericResult> AttachTrees(TImplementationConfiguration root, int levels, DateTimeOffset? asOf, CancellationToken ct)
    {
        // A record with no generated mapper declares no child lists, so it holds no tree.
        var recordType = root.GetType();
        var mapper = PocoMapperCollection.ByName(recordType.Name);
        if (mapper == PocoMapperCollection.NotFound)
            return GenericResult.Success();

        var trees = mapper.CascadeChildren.Where(d => ConfigurationTrees.IsTree(d, recordType)).ToList();
        if (trees.Count == 0 || levels <= 0)
            return GenericResult.Success();

        if (asOf is not null)
            return GenericResult.Failure(DefaultConfigurationProviderLog.TreeAsOfNotSupported(_logger, recordType.Name, root.Name));

        var link = TreeLink(root);
        if (link.IsFailure) return link;

        var members = await ComposeMembers(levels - 1, ct).ConfigureAwait(false);
        if (members.IsFailure) return members;

        return ConfigurationTrees.Attach(root, members.Value!, link.Value!, trees, mapper, levels, _logger);
    }

    // Every member of the domain, composed to the given levels but not hung into trees.
    private async Task<IGenericResult<IReadOnlyList<IImplementationConfiguration>>> ComposeMembers(int levels, CancellationToken ct)
    {
        var gateway = _gatewayProvider.Get(DataStoreName);
        if (gateway.IsFailure) return gateway.ToNewResult<IReadOnlyList<IImplementationConfiguration>>();

        var rows = await gateway.Value!.Execute<IEnumerable<TDomain>>(
            _commands.List(DataStoreName, PathName), Target, ct).ConfigureAwait(false);
        if (!rows.IsSuccess) return rows.ToNewResult<IReadOnlyList<IImplementationConfiguration>>();

        var members = new List<IImplementationConfiguration>();
        if (rows.Value is null)
            return GenericResult<IReadOnlyList<IImplementationConfiguration>>.Failure(
                DefaultConfigurationProviderLog.DomainListReturnedNoResult(_logger, typeof(TImplementationConfiguration).Name));

        foreach (var row in rows.Value)
        {
            var loaded = await Compose(row, provider => provider.Get(row.Id, levels, ct), levels, null, ct, attachTrees: false).ConfigureAwait(false);
            if (!loaded.IsSuccess) return loaded.ToNewResult<IReadOnlyList<IImplementationConfiguration>>();
            if (loaded.Value is not null) members.Add(loaded.Value);
        }

        return GenericResult<IReadOnlyList<IImplementationConfiguration>>.Success(members);
    }

    // The field a member's parent is named in: the one Foreign key from its implementation container to this domain's durable Id.
    private IGenericResult<string> TreeLink(TImplementationConfiguration root)
    {
        if (!_implementations.TryGetValue(root.Implementation, out var provider))
            return GenericResult<string>.Failure(DefaultConfigurationProviderLog.NoImplementationProvider(_logger, root.Name, root.Implementation));

        var links = LinksToDomain(provider.ContainerName);
        if (links.IsFailure) return links.ToNewResult<string>();

        return links.Value!.Count switch
        {
            0 => GenericResult<string>.Failure(DefaultConfigurationProviderLog.TreeLinkNotDeclared(_logger, root.GetType().Name, provider.ContainerName)),
            1 => GenericResult<string>.Success(links.Value[0]),
            _ => GenericResult<string>.Failure(DefaultConfigurationProviderLog.TreeLinkAmbiguous(_logger, root.GetType().Name, provider.ContainerName, links.Value.Count)),
        };
    }

    // The fields of an implementation container that are Foreign keys to this domain's durable Id, read from the store's configuration.
    private IGenericResult<List<string>> LinksToDomain(string implementationContainer)
    {
        var gateway = _gatewayProvider.Get(DataStoreName);
        if (gateway.IsFailure) return gateway.ToNewResult<List<string>>();

        var path = gateway.Value!.DataStores
            .FirstOrDefault(store => string.Equals(store.Name, DataStoreName, StringComparison.Ordinal))?
            .Path(PathName);
        var domain = path is { IsSuccess: true, Value: not null } ? path.Value.Container(_commands.TableName) : null;
        if (domain is not { IsSuccess: true, Value: not null })
            return GenericResult<List<string>>.Failure(
                DefaultConfigurationProviderLog.ContainerNotFoundInStore(_logger, typeof(TDomain).Name, _commands.TableName, DataStoreName));

        var logical = ConfigurationKeys.FieldOf(domain.Value.Keys, "Logical");
        if (logical is null)
            return GenericResult<List<string>>.Failure(
                DefaultConfigurationProviderLog.NoSuitableKeyForContainer(_logger, typeof(TImplementationConfiguration).Name, domain.Value.Name));

        var referencing = domain.Value.ReferencingKeys;
        if (referencing.IsFailure) return referencing.ToNewResult<List<string>>();

        return GenericResult<List<string>>.Success(referencing.Value!
            .Where(b => string.Equals(b.Owner.Name, implementationContainer, StringComparison.Ordinal)
                        && b.Key.KeyFields.Count == 1
                        && string.Equals(b.Key.KeyFields[0].ReferencedField?.Name, logical, StringComparison.Ordinal))
            .Select(b => b.Key.KeyFields[0].LocalField.Name)
            .ToList());
    }

    /// <summary>The record's properties beyond the four every domain has, in the order its mapper declares them.</summary>
    /// <remarks>
    /// Found by comparing the record's mapper with <see cref="DomainConfiguration"/>'s, so what a domain
    /// lists is whatever its record declares -- no name is written here. A domain that keeps the plain
    /// record has none, and every use of this is then a no-op.
    /// </remarks>
    private IGenericResult<IReadOnlyList<string>> ListFields()
    {
        if (_listFields is { } known)
            return GenericResult<IReadOnlyList<string>>.Success(known);

        var record = PocoMapperCollection.ByName(typeof(TDomain).Name);
        if (record == PocoMapperCollection.NotFound)
            return GenericResult<IReadOnlyList<string>>.Failure(
                DefaultConfigurationProviderLog.DetachMapperMissing(_logger, typeof(TDomain).Name));

        var plain = PocoMapperCollection.ByName(nameof(DomainConfiguration));
        if (plain == PocoMapperCollection.NotFound)
            return GenericResult<IReadOnlyList<string>>.Failure(
                DefaultConfigurationProviderLog.DetachMapperMissing(_logger, nameof(DomainConfiguration)));

        _listFields = record.GetPropertyNames().Where(name => !plain.GetPropertyNames().Contains(name, StringComparer.Ordinal)).ToList();
        return GenericResult<IReadOnlyList<string>>.Success(_listFields);
    }

    /// <summary>Copies the domain row's list fields onto the implementation a read composed.</summary>
    private IGenericResult StampListFields(TDomain row, TImplementationConfiguration implementation)
    {
        var fields = ListFields();
        if (fields.IsFailure) return fields;
        if (fields.Value!.Count == 0) return GenericResult.Success();

        var mapper = PocoMapperCollection.ByName(implementation.GetType().Name);
        if (mapper == PocoMapperCollection.NotFound)
            return GenericResult.Failure(DefaultConfigurationProviderLog.DetachMapperMissing(_logger, implementation.GetType().Name));

        var values = PocoMapperCollection.ByName(typeof(TDomain).Name).MapToParameters(row);
        foreach (var field in fields.Value)
        {
            if (!mapper.GetPropertyNames().Contains(field, StringComparer.Ordinal))
                return GenericResult.Failure(DefaultConfigurationProviderLog.ListFieldNotOnImplementation(
                    _logger, typeof(TDomain).Name, implementation.GetType().Name, field));

            mapper.SetValue(implementation, field, values[field]);
        }

        return GenericResult.Success();
    }

    /// <summary>Copies the implementation's list fields onto the domain record a save is about to write.</summary>
    private IGenericResult TakeListFields(TImplementationConfiguration implementation, TDomain row)
    {
        var fields = ListFields();
        if (fields.IsFailure) return fields;
        if (fields.Value!.Count == 0) return GenericResult.Success();

        var mapper = PocoMapperCollection.ByName(implementation.GetType().Name);
        if (mapper == PocoMapperCollection.NotFound)
            return GenericResult.Failure(DefaultConfigurationProviderLog.DetachMapperMissing(_logger, implementation.GetType().Name));

        var values = mapper.MapToParameters(implementation);
        foreach (var field in fields.Value)
        {
            if (!values.TryGetValue(field, out var value))
                return GenericResult.Failure(DefaultConfigurationProviderLog.ListFieldNotOnImplementation(
                    _logger, typeof(TDomain).Name, implementation.GetType().Name, field));

            PocoMapperCollection.ByName(typeof(TDomain).Name).SetValue(row, field, value);
        }

        return GenericResult.Success();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// FDW-811 REVERT (2026-09-18): this used to batch domain rows by Implementation and dispatch one
    /// <c>Get(IEnumerable&lt;Guid&gt;)</c> call per implementation, matching each returned item back to
    /// its domain row via a dictionary keyed by <c>item.Id</c>. That key is the *implementation* row's
    /// own Id -- a different Guid from the domain row's Id by design (the domain/implementation split:
    /// the implementation only gets the domain's Id stamped onto it AFTER a successful match). Looking
    /// that dictionary up by <c>domainRow.Id</c> can therefore never match, for any domain using this
    /// method, regardless of connection type. Confirmed live: "Composed 0 of 2
    /// IAuthenticationFlowImplementationConfiguration row(s)" despite both individual implementation
    /// reads succeeding (each logged its own "Composed AuthenticationFlowImplementationConfiguration
    /// for domain id ..." immediately before) -- the aggregation step silently discarded both.
    /// Reverted to the pre-FDW-811 per-row loop below: one single-id
    /// <c>IImplementationConfigurationProvider{TImplementationConfiguration}.Get(Guid, CancellationToken)</c>
    /// call per domain row, with no dictionary/Guid-matching step to get wrong. See FDW-811.
    /// </remarks>
    public Task<IGenericResult<IReadOnlyList<TImplementationConfiguration>>> Get(CancellationToken ct = default)
        => Get(ConfigurationLevels.WholeAggregate, ct);

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyList<TImplementationConfiguration>>> Get(int levels, CancellationToken ct = default)
    {
        if (levels < 0)
            return GenericResult<IReadOnlyList<TImplementationConfiguration>>.Failure(
                DefaultConfigurationProviderLog.ReadArgumentOutOfRange(_logger, typeof(TImplementationConfiguration).Name, nameof(levels), levels));

        var gateway = _gatewayProvider.Get(DataStoreName);
        if (gateway.IsFailure) return gateway.ToNewResult<IReadOnlyList<TImplementationConfiguration>>();

        var rows = await gateway.Value!.Execute<IEnumerable<TDomain>>(
            _commands.List(DataStoreName, PathName), Target, ct).ConfigureAwait(false);
        if (!rows.IsSuccess) return rows.ToNewResult<IReadOnlyList<TImplementationConfiguration>>();

        var found = new List<TImplementationConfiguration>();
        foreach (var row in rows.Value ?? [])
        {
            var loaded = await Compose(row, provider => provider.Get(row.Id, levels, ct), levels, null, ct).ConfigureAwait(false);
            if (!loaded.IsSuccess) return loaded.ToNewResult<IReadOnlyList<TImplementationConfiguration>>();
            if (loaded.Value is null) continue;
            found.Add(loaded.Value);
        }

        return GenericResult<IReadOnlyList<TImplementationConfiguration>>.Success(found);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyList<T>>> Find<T>(Func<T, bool> predicate, CancellationToken ct = default)
        where T : TImplementationConfiguration
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var all = await Get(ct).ConfigureAwait(false);
        if (!all.IsSuccess) return all.ToNewResult<IReadOnlyList<T>>();

        var matched = new List<T>();
        foreach (var one in all.Value ?? [])
        {
            if (one is T typed && predicate(typed)) matched.Add(typed);
        }

        return GenericResult<IReadOnlyList<T>>.Success(matched);
    }

    /// <inheritdoc/>
    public Task<IGenericResult> Save<T>(
        T implementationConfiguration, string domain, string implementationName, string name,
        CancellationToken ct = default)
        where T : TImplementationConfiguration
        => WriteInOwnUnit(implementationConfiguration, domain, implementationName, name, mayCreate: true, mayReplace: true, ct);

    /// <inheritdoc/>
    public Task<IGenericResult> Update<T>(
        T implementationConfiguration, string domain, string implementationName, string name,
        CancellationToken ct = default)
        where T : TImplementationConfiguration
        => WriteInOwnUnit(implementationConfiguration, domain, implementationName, name, mayCreate: false, mayReplace: true, ct);

    /// <inheritdoc/>
    public Task<IGenericResult> Create<T>(
        T implementationConfiguration, string domain, string implementationName, string name,
        CancellationToken ct = default)
        where T : TImplementationConfiguration
        => WriteInOwnUnit(implementationConfiguration, domain, implementationName, name, mayCreate: true, mayReplace: false, ct);

    // One write in a unit of its own: opened here, committed on success and rolled back on a failure.
    private async Task<IGenericResult> WriteInOwnUnit<T>(
        T implementationConfiguration, string domain, string implementationName, string name, bool mayCreate, bool mayReplace,
        CancellationToken ct)
        where T : TImplementationConfiguration
    {
        // Checked before the unit is opened: a save naming an implementation nothing is registered
        // for cannot proceed however the transaction goes, so it should not take one out.
        if (!_implementations.ContainsKey(implementationName))
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.NoImplementationProvider(_logger, name, implementationName));

        var unit = await BeginWriteScope(ct).ConfigureAwait(false);
        if (unit.IsFailure) return unit;

        await using var scope = unit.Value!;
        var saved = await WriteMember(implementationConfiguration, domain, implementationName, name, mayCreate, mayReplace, scope, ct).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            await scope.Rollback(ct).ConfigureAwait(false);
            return saved;
        }

        var committed = await scope.Commit(ct).ConfigureAwait(false);
        return committed.IsSuccess ? saved : committed;
    }

    /// <summary>Saves a record into a unit of work the caller opened and will commit.</summary>
    /// <remarks>
    /// For an operation made of several records — replacing a set rather than writing one — so they
    /// land together. A single save uses the overload without a unit, which opens and commits its own.
    /// </remarks>
    public Task<IGenericResult> Save<T>(
        T implementationConfiguration, string domain, string implementationName, string name,
        ConfigurationWriteScope unit, CancellationToken ct = default)
        where T : TImplementationConfiguration
        => WriteMember(implementationConfiguration, domain, implementationName, name, mayCreate: true, mayReplace: true, unit, ct);

    /// <summary>Updates an existing member inside a unit of work the caller opened and will commit.</summary>
    /// <remarks>
    /// The sibling of the unit overload of <c>Save</c> that never creates a member: the record must identify one that exists.
    /// </remarks>
    public Task<IGenericResult> Update<T>(
        T implementationConfiguration, string domain, string implementationName, string name,
        ConfigurationWriteScope unit, CancellationToken ct = default)
        where T : TImplementationConfiguration
        => WriteMember(implementationConfiguration, domain, implementationName, name, mayCreate: false, mayReplace: true, unit, ct);

    // The write Save, Update and Create make. Only a Save or a Create (mayCreate) may mint a member; an Update whose record identifies
    // none fails, so a read that outlived its row cannot bring the member back or overwrite one made since under its name.
    // Only a Save or an Update (mayReplace) may write over a member; a Create whose name or Id is taken fails, so a create cannot
    // overwrite the member it collides with.
    private async Task<IGenericResult> WriteMember<T>(
        T implementationConfiguration, string domain, string implementationName, string name, bool mayCreate, bool mayReplace,
        ConfigurationWriteScope unit, CancellationToken ct)
        where T : TImplementationConfiguration
    {
        var writable = WritableImplementation(implementationName, name);
        if (writable.IsFailure) return writable;

        var domainContainer = unit.Container(PathName, _commands.TableName);
        if (domainContainer.IsFailure) return domainContainer;

        // Read on the unit, not the gateway: a second connection blocks on the locks this unit
        // already holds once it has written a record, and waits until the command times out.
        var existing = await unit.Read<IEnumerable<TDomain>>(
            _commands.Get(DataStoreName, PathName, name), Target, ct).ConfigureAwait(false);
        if (!existing.IsSuccess) return existing.ToNewResult<TImplementationConfiguration>();

        implementationConfiguration.Name = name;
        implementationConfiguration.Domain = domain;
        implementationConfiguration.Implementation = implementationName;

        // The name identifies the member, so a row already carrying it is the row to hang from. A name no row carries is a new
        // member, unless the record's Id names a member: then the record is that member under a new name.
        // The two rows are never written apart: a domain row naming an implementation that was not
        // written is the record that fails to compose on the next read.
        var current = OnlyRow(existing.Value, name);
        if (current.IsFailure) return current;
        var target = await CheckTarget(current.Value, implementationConfiguration.Id, name, mayCreate, mayReplace, unit, ct).ConfigureAwait(false);
        if (target.IsFailure) return target;

        if (current.Value is { } holder)
        {
            var named = await RefuseOtherMembersName(holder, implementationConfiguration.Id, name, unit, ct).ConfigureAwait(false);
            if (named.IsFailure) return named;
        }

        var domainRow = current.Value is { } found
            ? await CurrentDomainRow(found, implementationConfiguration, writable.Value!, domainContainer.Value!, unit, ct).ConfigureAwait(false)
            : await RenamedOrMintedDomainRow(name, domain, implementationName, implementationConfiguration, unit, ct).ConfigureAwait(false);
        if (domainRow.IsFailure) return domainRow;
        var domainId = domainRow.Value.Id;

        implementationConfiguration.Id = domainId;

        // The implementation fills the Foreign key its container declares to this domain container
        // from the domain row's key values; this provider passes them and names no column.
        var saved = await writable.Value!.Save(implementationConfiguration, domainContainer.Value!, domainRow.Value.Keys, unit, ct).ConfigureAwait(false);
        if (saved.IsFailure) return saved;

        DefaultConfigurationProviderLog.DomainRowSaved(
            _logger, typeof(TImplementationConfiguration).Name, name, domainId, domainRow.Value.Action);
        return saved;
    }

    // The provider that writes an implementation, which has to join the caller's unit of work.
    // A minted domain row goes back with an implementation that is refused, because the caller
    // rolls the whole unit back. One left behind names an implementation that does not exist, and
    // every later read of that name fails to compose.
    private IGenericResult<IConfigurationWriteParticipant<TImplementationConfiguration>> WritableImplementation(
        string implementationName, string name)
    {
        if (!_implementations.TryGetValue(implementationName, out var provider))
            return GenericResult<IConfigurationWriteParticipant<TImplementationConfiguration>>.Failure(
                DefaultConfigurationProviderLog.NoImplementationProvider(_logger, name, implementationName));

        return provider is IConfigurationWriteParticipant<TImplementationConfiguration> writable
            ? GenericResult<IConfigurationWriteParticipant<TImplementationConfiguration>>.Success(writable)
            : GenericResult<IConfigurationWriteParticipant<TImplementationConfiguration>>.Failure(
                DefaultConfigurationProviderLog.ProviderCannotJoinWrite(_logger, name, provider.GetType().FullName ?? "(null)"));
    }

    // What the kind of write requires of the member the record names: a Save requires nothing, an Update requires one, a Create requires none.
    private Task<IGenericResult> CheckTarget(
        TDomain? holder, Guid recordId, string name, bool mayCreate, bool mayReplace, ConfigurationWriteScope unit, CancellationToken ct)
        => !mayCreate ? UpdateTarget(holder, recordId, name, unit, ct)
            : !mayReplace ? CreateTarget(holder, recordId, name, unit, ct)
            : Task.FromResult<IGenericResult>(GenericResult.Success());

    /// <summary>Refuses an update whose record identifies no existing member.</summary>
    /// <remarks>
    /// The row under the name is the member when the record carries no Id (the name identifies it) or carries that row's Id. Otherwise the
    /// record's Id has to name a member: the same member under another name is a rename, and another member is refused further on as one
    /// member saved under the name of another. A record with no Id and no row under its name, or an Id that names no member, is a read
    /// that outlived its row, so it fails where a save would mint a member (or write over one made since under the same name).
    /// </remarks>
    private async Task<IGenericResult> UpdateTarget(
        TDomain? holder, Guid recordId, string name, ConfigurationWriteScope unit, CancellationToken ct)
    {
        if (holder is not null && (recordId == Guid.Empty || recordId == holder.Id))
            return GenericResult.Success();

        if (recordId == Guid.Empty)
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.UpdateTargetNotFound(_logger, typeof(TImplementationConfiguration).Name, name));

        var named = await unit.Read<IEnumerable<TDomain>>(
            _commands.Get(DataStoreName, PathName, recordId), Target, ct).ConfigureAwait(false);
        if (!named.IsSuccess) return named.ToNewResult<TImplementationConfiguration>();

        return named.Value?.FirstOrDefault() is not null
            ? GenericResult.Success()
            : GenericResult.Failure(
                DefaultConfigurationProviderLog.UpdateTargetNotFound(_logger, typeof(TImplementationConfiguration).Name, name));
    }

    /// <summary>Refuses a create whose name or Id already identifies a member.</summary>
    /// <remarks>
    /// A row under the name is a member, so the create fails. A record that carries an Id is refused too when that Id names a member: written
    /// under a free name it would rename that member, which is an update and not a create.
    /// </remarks>
    private async Task<IGenericResult> CreateTarget(
        TDomain? holder, Guid recordId, string name, ConfigurationWriteScope unit, CancellationToken ct)
    {
        if (holder is not null)
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.CreateTargetExists(_logger, typeof(TImplementationConfiguration).Name, name));

        if (recordId == Guid.Empty)
            return GenericResult.Success();

        var named = await unit.Read<IEnumerable<TDomain>>(
            _commands.Get(DataStoreName, PathName, recordId), Target, ct).ConfigureAwait(false);
        if (!named.IsSuccess) return named.ToNewResult<TImplementationConfiguration>();

        return named.Value?.FirstOrDefault() is not null
            ? GenericResult.Failure(
                DefaultConfigurationProviderLog.CreateTargetExists(_logger, typeof(TImplementationConfiguration).Name, name))
            : GenericResult.Success();
    }

    /// <summary>The domain row a save of an existing member hangs its implementation from.</summary>
    /// <remarks>
    /// The current row itself when the list fields the save carries are the ones it holds -- the common
    /// case, and the only one for a domain that lists nothing. Otherwise a new version of it, because a
    /// domain row is versioned on write like any other: the implementation under the old version is
    /// superseded by that version's keys, the old row is superseded, and the new row is inserted with
    /// the columns the record does not map carried over from the old one. Those are the row's tenancy
    /// among them; they are found as the columns the container declares that the record leaves
    /// unmapped, so no column is named here and none can be forgotten when one is added.
    /// </remarks>
    private async Task<IGenericResult<(Guid Id, IReadOnlyDictionary<string, object?> Keys, string Action)>> CurrentDomainRow(
        TDomain current,
        TImplementationConfiguration implementation,
        IConfigurationWriteParticipant<TImplementationConfiguration> writable,
        IDataContainer domainContainer,
        ConfigurationWriteScope unit,
        CancellationToken ct)
    {
        var stored = await ExistingDomainRow(current, unit, ct).ConfigureAwait(false);
        if (stored.IsFailure) return stored.ToNewResult<(Guid, IReadOnlyDictionary<string, object?>, string)>();

        var wanted = new TDomain
        {
            Id = current.Id,
            Name = current.Name,
            Domain = current.Domain,
            Implementation = current.Implementation,
        };
        var taken = TakeListFields(implementation, wanted);
        if (taken.IsFailure) return taken.ToNewResult<(Guid, IReadOnlyDictionary<string, object?>, string)>();

        var mapper = PocoMapperCollection.ByName(typeof(TDomain).Name);
        var held = mapper.MapToParameters(current);
        var carried = mapper.MapToParameters(wanted);
        var listed = ListFields();
        if (listed.IsFailure) return listed.ToNewResult<(Guid, IReadOnlyDictionary<string, object?>, string)>();

        if (listed.Value!.All(field => Equals(held[field], carried[field])))
            return GenericResult<(Guid, IReadOnlyDictionary<string, object?>, string)>.Success((current.Id, stored.Value.Keys, "reused"));

        if (writable is not IConfigurationSupersedeParticipant<TImplementationConfiguration> superseding)
            return GenericResult<(Guid, IReadOnlyDictionary<string, object?>, string)>.Failure(
                DefaultConfigurationProviderLog.ProviderCannotSupersede(_logger, current.Name, writable.GetType().FullName ?? "(null)"));

        // The implementation goes first, while the row it hangs from is still current: the security
        // predicate on its table reaches the domain row through its key.
        var superseded = await superseding.Supersede(domainContainer, stored.Value.Keys, unit, ct).ConfigureAwait(false);
        if (superseded.IsFailure) return superseded.ToNewResult<(Guid, IReadOnlyDictionary<string, object?>, string)>();

        var retired = await unit.Write(_commands.Supersede(DataStoreName, PathName, current.Id), Target, ct).ConfigureAwait(false);
        if (retired.IsFailure) return retired.ToNewResult<(Guid, IReadOnlyDictionary<string, object?>, string)>();
        if (retired.Value!.Retired.Count == 0)
            return GenericResult<(Guid, IReadOnlyDictionary<string, object?>, string)>.Failure(
                DefaultConfigurationProviderLog.DomainRowNotFound(_logger, typeof(TImplementationConfiguration).Name, current.Id.ToString()));

        var written = await unit.Write(
            _commands.Insert(DataStoreName, PathName, wanted, Carried(stored.Value.Keys)), Target, ct).ConfigureAwait(false);
        if (written.IsFailure) return written.ToNewResult<(Guid, IReadOnlyDictionary<string, object?>, string)>();

        return written.Value?.Written is { } keys
            ? GenericResult<(Guid, IReadOnlyDictionary<string, object?>, string)>.Success((wanted.Id, keys, "reversioned"))
            : GenericResult<(Guid, IReadOnlyDictionary<string, object?>, string)>.Failure(
                DefaultConfigurationProviderLog.WriteReturnedNoRow(_logger, _commands.TableName));
    }

    /// <summary>Refuses a record that is one member saved under the name of another.</summary>
    /// <remarks>
    /// A record carrying the Id of a current member is that member. The row that carries the name is the row the save writes into, so a record
    /// that is member A saved under member B's name would overwrite B and leave A as it was. A record whose Id names no member (a create mints a
    /// fresh Id) is a new record and saves into the row that carries the name, as it always did.
    /// </remarks>
    private async Task<IGenericResult> RefuseOtherMembersName(
        TDomain holder, Guid recordId, string name, ConfigurationWriteScope unit, CancellationToken ct)
    {
        if (recordId == Guid.Empty || recordId == holder.Id)
            return GenericResult.Success();

        var named = await unit.Read<IEnumerable<TDomain>>(
            _commands.Get(DataStoreName, PathName, recordId), Target, ct).ConfigureAwait(false);
        if (!named.IsSuccess) return named.ToNewResult<TImplementationConfiguration>();

        return named.Value?.FirstOrDefault() is not null
            ? GenericResult.Failure(DefaultConfigurationProviderLog.NameHeldByAnotherMember(_logger, typeof(TImplementationConfiguration).Name, name, recordId))
            : GenericResult.Success();
    }

    /// <summary>The domain row a record with no row under its name hangs from: the member its Id names, renamed, or a new member.</summary>
    /// <remarks>
    /// A member's durable identity is its Id and its name is an attribute of it, so a loaded member saved under another name is the
    /// same member renamed. Reading it as a new member left the old one current beside it. A record whose Id names no member (a
    /// create mints a fresh Id) is a new member.
    /// </remarks>
    private async Task<IGenericResult<(Guid Id, IReadOnlyDictionary<string, object?> Keys, string Action)>> RenamedOrMintedDomainRow(
        string name,
        string domain,
        string implementationName,
        TImplementationConfiguration implementation,
        ConfigurationWriteScope unit,
        CancellationToken ct)
    {
        if (implementation.Id == Guid.Empty)
            return await MintDomainRow(name, domain, implementationName, implementation, unit, ct).ConfigureAwait(false);

        var named = await unit.Read<IEnumerable<TDomain>>(
            _commands.Get(DataStoreName, PathName, implementation.Id), Target, ct).ConfigureAwait(false);
        if (!named.IsSuccess) return named.ToNewResult<(Guid, IReadOnlyDictionary<string, object?>, string)>();

        return named.Value?.FirstOrDefault() is { } renamed
            ? await RenameDomainRow(renamed, name, unit, ct).ConfigureAwait(false)
            : await MintDomainRow(name, domain, implementationName, implementation, unit, ct).ConfigureAwait(false);
    }

    /// <summary>Gives a member's domain row its new name, in place.</summary>
    /// <remarks>
    /// Why in place and not a new version of the row: the domain tables declare their unique index on Id without a version filter, and other
    /// tables cite that Id, so a domain row has one row for its whole life and its Id is its identity. A new version under the same Id would
    /// be refused by the index (FDW-3082). The implementation is saved against the same row's keys afterwards, as for any update.
    /// </remarks>
    private async Task<IGenericResult<(Guid Id, IReadOnlyDictionary<string, object?> Keys, string Action)>> RenameDomainRow(
        TDomain row, string name, ConfigurationWriteScope unit, CancellationToken ct)
    {
        var update = CmdBuilders.Update.In<DomainRowRename>(_commands.TableName)
            .DataStore(DataStoreName)
            .Path(PathName)
            .Where("Id", row.Id)
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .Value(new DomainRowRename { Name = name });
        var renamed = await unit.Read<int>(update.Command, update.Target, ct).ConfigureAwait(false);
        if (!renamed.IsSuccess) return renamed.ToNewResult<(Guid, IReadOnlyDictionary<string, object?>, string)>();
        if (renamed.Value != 1)
            return GenericResult<(Guid, IReadOnlyDictionary<string, object?>, string)>.Failure(
                DefaultConfigurationProviderLog.DomainRowNotFound(_logger, typeof(TImplementationConfiguration).Name, row.Id.ToString()));

        var stored = await ExistingDomainRow(row, unit, ct).ConfigureAwait(false);
        return stored.IsSuccess
            ? GenericResult<(Guid, IReadOnlyDictionary<string, object?>, string)>.Success((row.Id, stored.Value.Keys, "renamed"))
            : stored.ToNewResult<(Guid, IReadOnlyDictionary<string, object?>, string)>();
    }

    // The columns the row holds that the record does not map, as the store returned them.
    private static Dictionary<string, object?> Carried(IReadOnlyDictionary<string, object?> stored)
    {
        var mapped = PocoMapperCollection.ByName(typeof(TDomain).Name).GetPropertyNames();
        var carried = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var column in stored)
        {
            if (!mapped.Contains(column.Key, StringComparer.Ordinal))
                carried[column.Key] = column.Value;
        }

        return carried;
    }

    /// <summary>The key values of a domain row that already exists, read with all its configured fields.</summary>
    private async Task<IGenericResult<(Guid Id, IReadOnlyDictionary<string, object?> Keys)>> ExistingDomainRow(
        TDomain row, ConfigurationWriteScope unit, CancellationToken ct)
    {
        var fields = await unit.Read<IEnumerable<Dictionary<string, object?>>>(
            _commands.Get(DataStoreName, PathName, row.Id), Target, ct).ConfigureAwait(false);
        if (!fields.IsSuccess) return fields.ToNewResult<(Guid, IReadOnlyDictionary<string, object?>)>();

        return fields.Value?.FirstOrDefault() is { } values
            ? GenericResult<(Guid, IReadOnlyDictionary<string, object?>)>.Success((row.Id, values))
            : GenericResult<(Guid, IReadOnlyDictionary<string, object?>)>.Failure(
                DefaultConfigurationProviderLog.DomainRowNotFound(_logger, typeof(TImplementationConfiguration).Name, row.Id.ToString()));
    }

    /// <summary>Writes the domain row a new record hangs from, inside the record's unit of work.</summary>
    /// <remarks>
    /// The row is minted and written in one step because it exists only to own the implementation that
    /// follows it in the same unit; neither is ever written without the other. It carries the list
    /// fields the implementation holds. The write returns the row's key values, which the
    /// implementation's Foreign key is filled from.
    /// </remarks>
    private async Task<IGenericResult<(Guid Id, IReadOnlyDictionary<string, object?> Keys, string Action)>> MintDomainRow(
        string name,
        string domain,
        string implementationName,
        TImplementationConfiguration implementation,
        ConfigurationWriteScope unit,
        CancellationToken ct)
    {
        var record = new TDomain
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Domain = domain,
            Implementation = implementationName,
        };
        var taken = TakeListFields(implementation, record);
        if (taken.IsFailure) return taken.ToNewResult<(Guid, IReadOnlyDictionary<string, object?>, string)>();

        var written = await unit.Write(
            _commands.Create(DataStoreName, PathName, record), Target, ct).ConfigureAwait(false);
        if (written.IsFailure) return written.ToNewResult<(Guid, IReadOnlyDictionary<string, object?>, string)>();

        return written.Value?.Written is { } keys
            ? GenericResult<(Guid, IReadOnlyDictionary<string, object?>, string)>.Success((record.Id, keys, "minted"))
            : GenericResult<(Guid, IReadOnlyDictionary<string, object?>, string)>.Failure(
                DefaultConfigurationProviderLog.WriteReturnedNoRow(_logger, _commands.TableName));
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> Delete(Guid id, CancellationToken ct = default)
    {
        var scope = await BeginWriteScope(ct).ConfigureAwait(false);
        if (scope.IsFailure) return scope;

        await using var unit = scope.Value!;
        var removed = await Delete(id, unit, ct).ConfigureAwait(false);
        if (removed.IsFailure)
        {
            await unit.Rollback(ct).ConfigureAwait(false);
            return removed;
        }

        return await unit.Commit(ct).ConfigureAwait(false);
    }

    /// <summary>Deletes a record into a unit of work the caller opened and will commit.</summary>
    /// <remarks>
    /// For an operation made of several records, so they retire together. A single delete uses the
    /// overload without a unit, which opens and commits its own.
    /// </remarks>
    public async Task<IGenericResult> Delete(Guid id, ConfigurationWriteScope unit, CancellationToken ct = default)
    {
        var found = await ResolveRowForDelete(id, unit, ct).ConfigureAwait(false);
        if (found.IsFailure) return found;
        if (found.Value is not { } row)
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.DeleteTargetNotFound(_logger, typeof(TImplementationConfiguration).Name, id.ToString()));

        if (row.Implementation is not { } implementation || !_implementations.TryGetValue(implementation, out var provider))
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.NoImplementationProvider(_logger, row.Name, row.Implementation ?? "(none)"));

        // The implementation goes first: a domain row whose implementation outlived it is a row
        // that names something no longer there. Both retire in one unit, so neither can outlive the
        // other however the other fails.
        if (provider is not IConfigurationWriteParticipant<TImplementationConfiguration> writable)
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.ProviderCannotJoinWrite(_logger, row.Name, provider.GetType().FullName ?? "(null)"));

        var domainContainer = unit.Container(PathName, _commands.TableName);
        if (domainContainer.IsFailure) return domainContainer;

        var domainRow = await ExistingDomainRow(row, unit, ct).ConfigureAwait(false);
        if (domainRow.IsFailure) return domainRow;

        IGenericResult removed = await writable.Delete(domainContainer.Value!, domainRow.Value.Keys, unit, ct).ConfigureAwait(false);
        if (removed.IsFailure) return removed;

        removed = await unit.Write(
            _commands.Delete(DataStoreName, PathName, row.Id), Target, ct).ConfigureAwait(false);
        if (removed.IsFailure) return removed;

        DefaultConfigurationProviderLog.DomainRowDeleted(_logger, typeof(TImplementationConfiguration).Name, row.Id);
        return removed;
    }

    /// <summary>Opens a unit of work over this provider's store, for an operation of several records.</summary>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The open unit, which the caller commits or rolls back.</returns>
    protected async Task<IGenericResult<ConfigurationWriteScope>> BeginWriteScope(CancellationToken ct = default)
    {
        var gateway = _gatewayProvider.Get(DataStoreName);
        return gateway.IsSuccess && gateway.Value is { } resolved
            ? await ConfigurationWriteScope.Begin(resolved, DataStoreName, _logger, ct).ConfigureAwait(false)
            : gateway.ToNewResult<ConfigurationWriteScope>();
    }

    /// <summary>Reads the domain row a delete addresses, if it is still there.</summary>
    private async Task<IGenericResult<TDomain?>> ResolveRowForDelete(
        Guid id, ConfigurationWriteScope unit, CancellationToken ct)
    {
        // On the unit for the same reason the save's lookup is -- see Save.
        var rows = await unit.Read<IEnumerable<TDomain>>(
            _commands.Get(DataStoreName, PathName, id), Target, ct).ConfigureAwait(false);
        if (!rows.IsSuccess) return rows.ToNewResult<TDomain?>();

        if (rows.Value?.FirstOrDefault() is not { } row)
        {
            DefaultConfigurationProviderLog.DomainRowNotFound(_logger, typeof(TImplementationConfiguration).Name, id.ToString());
            return GenericResult<TDomain?>.Success(null);
        }

        return GenericResult<TDomain?>.Success(row);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> Delete(string name, CancellationToken ct = default)
    {
        var gateway = _gatewayProvider.Get(DataStoreName);
        if (gateway.IsFailure) return gateway.ToNewResult<TImplementationConfiguration>();

        var rows = await gateway.Value!.Execute<IEnumerable<TDomain>>(
            _commands.Get(DataStoreName, PathName, name), Target, ct).ConfigureAwait(false);
        if (!rows.IsSuccess) return rows.ToNewResult<TImplementationConfiguration>();
        var single = OnlyRow(rows.Value, name);
        if (single.IsFailure) return single;
        if (single.Value is not { } row)
        {
            return GenericResult.Failure(
                DefaultConfigurationProviderLog.DeleteTargetNotFound(_logger, typeof(TImplementationConfiguration).Name, name));
        }

        return await Delete(row.Id, ct).ConfigureAwait(false);
    }

    // ── the erased surface ──────────────────────────────────────────────────
    // Explicit only where the erased contract and the typed one share a signature but not a return
    // type. Everything else on the two contracts is satisfied by one public method.

    async Task<IGenericResult<IImplementationConfiguration>> IDomainConfigurationProvider.Get(
        string name, CancellationToken ct)
    {
        var found = await Get(name, ct).ConfigureAwait(false);
        return found.IsSuccess
            ? GenericResult<IImplementationConfiguration>.Success(found.Value!)
            : found.ToNewResult<IImplementationConfiguration>();
    }

    async Task<IGenericResult<IImplementationConfiguration>> IDomainConfigurationProvider.Get(
        Guid id, CancellationToken ct)
    {
        var found = await Get(id, ct).ConfigureAwait(false);
        return found.IsSuccess
            ? GenericResult<IImplementationConfiguration>.Success(found.Value!)
            : found.ToNewResult<IImplementationConfiguration>();
    }

    async Task<IGenericResult> IDomainConfigurationProvider.Save(
        IImplementationConfiguration implementationConfiguration,
        string domain, string implementationName, string name, CancellationToken ct)
        => implementationConfiguration is TImplementationConfiguration typed
            ? await Save(typed, domain, implementationName, name, ct).ConfigureAwait(false)
            : GenericResult.Failure(
                DefaultConfigurationProviderLog.UntypedSaveTypeMismatch(
                    _logger,
                    typeof(TImplementationConfiguration).Name,
                    implementationConfiguration?.GetType().Name ?? "(null)"));

    async Task<IGenericResult> IDomainConfigurationProvider.Update(
        IImplementationConfiguration implementationConfiguration,
        string domain, string implementationName, string name, CancellationToken ct)
        => implementationConfiguration is TImplementationConfiguration typed
            ? await Update(typed, domain, implementationName, name, ct).ConfigureAwait(false)
            : GenericResult.Failure(
                DefaultConfigurationProviderLog.UntypedSaveTypeMismatch(
                    _logger,
                    typeof(TImplementationConfiguration).Name,
                    implementationConfiguration?.GetType().Name ?? "(null)"));

    private DataStoreTarget Target => new(DataStoreName, PathName, _commands.TableName);
}
