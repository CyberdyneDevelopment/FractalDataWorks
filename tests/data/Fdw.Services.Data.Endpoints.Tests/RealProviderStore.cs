using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Moq;
using Shouldly;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// A REAL domain configuration provider over a faked configuration store, for reads (FDW-6717).
/// </summary>
/// <remarks>
/// Why not a stand-in for the provider: what the owner picker needs from an organization is the provider's own behavior, which reads one domain record and the
/// implementation it names, copies the domain's list fields onto the implementation and fails when the implementation cannot carry one. A stand-in answered "fine"
/// and hid that. The store is supplied the way production supplies it: domain rows from the gateway, each dispatched to the implementation provider registered for
/// the row's implementation. A test states only what the store HOLDS, a domain row and the implementation it names, so a domain that lists fields holds them on
/// the row, as the table does. Reads only; every read is answered with every row, so a test holds only the rows it means to be read.
/// </remarks>
/// <typeparam name="TProvider">The domain provider under test.</typeparam>
/// <typeparam name="TContract">The domain's implementation contract.</typeparam>
/// <typeparam name="TDomain">The domain's record: the plain record, or the one a domain that lists fields declares.</typeparam>
internal sealed class RealProviderStore<TProvider, TContract, TDomain>
    where TProvider : DomainConfigurationProviderBase<TContract, TDomain>
    where TContract : class, IImplementationConfiguration
    where TDomain : DomainConfiguration, new()
{
    public const string StoreName = "PlatformConfiguration";

    private readonly List<TDomain> _rows = [];
    private readonly Dictionary<Guid, TContract> _implementations = [];

    private RealProviderStore(TProvider provider) => Provider = provider;

    /// <summary>The real provider, reading through the faked store.</summary>
    public TProvider Provider { get; }

    /// <summary>A store that holds nothing, whose provider is made by <paramref name="create"/> and whose rows are read under <paramref name="implementationName"/>.</summary>
    public static RealProviderStore<TProvider, TContract, TDomain> Empty(Func<IConfigurationGatewayProvider, TProvider> create, string implementationName)
    {
        // A page read completes its ordering with the keys the container declares, so the container declares one: its own Id, a key that references no other container.
        var keyedField = new Mock<IDataField>();
        keyedField.SetupGet(f => f.Name).Returns("Id");
        var keyField = new Mock<IContainerKeyField>();
        keyField.SetupGet(k => k.LocalField).Returns(keyedField.Object);
        var key = new Mock<IContainerKey>();
        key.SetupGet(k => k.ReferencedContainer).Returns((IDataContainer?)null);
        key.SetupGet(k => k.KeyFields).Returns(new List<IContainerKeyField> { keyField.Object });
        var container = new Mock<IDataContainer>();
        container.SetupGet(c => c.Keys).Returns(new List<IContainerKey> { key.Object });
        var path = new Mock<IDataNodePath>();
        path.Setup(p => p.Container(It.IsAny<string>())).Returns(GenericResult<IDataContainer>.Success(container.Object));
        var dataStore = new Mock<IDataStore>();
        dataStore.SetupGet(d => d.Name).Returns(StoreName);
        dataStore.Setup(d => d.Path(It.IsAny<string>())).Returns(GenericResult<IDataNodePath>.Success(path.Object));
        var gateway = new Mock<IConfigurationGateway>();
        gateway.SetupGet(g => g.DataStores).Returns([dataStore.Object]);
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.SetupGet(p => p.KeyChunkSize).Returns(1000);
        gateways.Setup(p => p.Get(It.IsAny<string>())).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));

        var store = new RealProviderStore<TProvider, TContract, TDomain>(create(gateways.Object));
        gateway
            .Setup(g => g.Execute<IEnumerable<TDomain>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDataCommand command, DataStoreTarget _, CancellationToken __) => GenericResult<IEnumerable<TDomain>>.Success(store.Matching(command)));

        // The implementation provider the domain dispatches to answers a record by the id of its domain row, singly and as a set.
        var implementations = new Mock<IImplementationConfigurationProvider<TContract>>();
        implementations
            .Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns((Guid id, CancellationToken _) => Task.FromResult(store.Serve(id)));
        implementations
            .Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns((Guid id, int _, CancellationToken _) => Task.FromResult(store.Serve(id)));
        implementations
            .Setup(p => p.GetSet(It.IsAny<IEnumerable<Guid>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns((IEnumerable<Guid> ids, int _, CancellationToken _) => Task.FromResult(store.ServeSet(ids)));
        store.Provider.Register(implementationName, implementations.Object).IsSuccess.ShouldBeTrue();
        store.ImplementationName = implementationName;
        return store;
    }

    /// <summary>The implementation every held row is read under.</summary>
    public string ImplementationName { get; private set; } = string.Empty;

    /// <summary>Holds a domain row, as the table does, and the implementation it names.</summary>
    /// <param name="row">The domain row, with the list fields its record declares.</param>
    /// <param name="implementation">The implementation record the row names.</param>
    public void Holds(TDomain row, TContract implementation)
    {
        row.Domain = typeof(TContract).Name;
        row.Implementation = ImplementationName;
        _rows.RemoveAll(held => held.Id == row.Id);
        _rows.Add(row);
        _implementations[row.Id] = implementation;
    }

    // The rows a read asks for: a query that names an equality on one of the record's properties (a page of one tenant's organizations) is answered with the rows that have that
    // value, as the table answers it; a read with no filter is answered with every row. Only equality is honoured, which is all the reads here state.
    private List<TDomain> Matching(IDataCommand command)
    {
        var rows = _rows.ToList();
        foreach (var condition in ConditionsOf(command))
        {
            var property = typeof(TDomain).GetProperty(condition.PropertyName);
            if (property is null)
                continue;

            rows = [.. rows.Where(row => Equals(property.GetValue(row), condition.Value))];
        }

        return rows;
    }

    private static IEnumerable<IFilterCondition> ConditionsOf(IDataCommand command)
        => command is IQueryCommand { Filter.Root: { } root } ? Conditions(root) : [];

    private static IEnumerable<IFilterCondition> Conditions(IFilterNode node)
    {
        switch (node)
        {
            case IFilterCondition condition:
                yield return condition;
                break;
            case FilterGroup group:
                foreach (var child in group.Nodes)
                    foreach (var condition in Conditions(child))
                        yield return condition;
                break;
            default:
                break;
        }
    }

    private IGenericResult<TContract> Serve(Guid id)
        => GenericResult<TContract>.Success(_implementations.TryGetValue(id, out var found) ? found : default!);

    private IGenericResult<IReadOnlyDictionary<Guid, TContract>> ServeSet(IEnumerable<Guid> ids)
        => GenericResult<IReadOnlyDictionary<Guid, TContract>>.Success(
            ids.Distinct().Where(_implementations.ContainsKey).ToDictionary(id => id, id => _implementations[id]));
}
