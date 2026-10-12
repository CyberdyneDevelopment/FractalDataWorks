using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Configuration;
using Moq;
using Fdw.Tests.Shared;

namespace Fdw.Services.Calculations.Tests.TestSupport;

/// <summary>
/// The calculation domain over a faked store: the REAL <see cref="CalculationConfigurationProvider"/>
/// reading domain rows from a mocked gateway and dispatching them to a mocked implementation provider.
/// </summary>
/// <remarks>
/// Why not a mock of the provider: <see cref="CalculationEntityService"/> takes the provider itself,
/// which is sealed and whose reads are not virtual. A test therefore states what the STORE holds —
/// the rows, and the record each row names — and asserts against the implementation provider, which
/// is where a write lands.
/// </remarks>
internal sealed class CalculationStore
{
    private const string EntityDomain = "CalculationEntity";
    private const string OneImplementation = "Formula";
    private const string StoreName = "PlatformConfiguration";

    public CalculationStore()
    {
        Gateway = new Mock<IConfigurationGateway>();
        GatewayWriteTransaction.StubOn(Gateway);
        Gateway
            .Setup(g => g.Execute<CalculationEntityDomainConfiguration>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<CalculationEntityDomainConfiguration>.Success(default!));

        // A save resolves the domain container from the gateway's loaded store before it writes
        // (FDW-892: relationships come from the configured keys), so the store has to answer with a
        // path and a container. Any container will do: the implementation participant is faked, and
        // it is what reads the container's key.
        var container = new Mock<IDataContainer>();
        var path = new Mock<IDataNodePath>();
        path.Setup(p => p.Container(It.IsAny<string>())).Returns(GenericResult<IDataContainer>.Success(container.Object));
        var dataStore = new Mock<IDataStore>();
        dataStore.SetupGet(d => d.Name).Returns(StoreName);
        dataStore.Setup(d => d.Path(It.IsAny<string>())).Returns(GenericResult<IDataNodePath>.Success(path.Object));
        Gateway.SetupGet(g => g.DataStores).Returns([dataStore.Object]);

        // Minting a domain row writes it and reads back its key values; saving onto an existing one
        // reads that row's key values. The implementation participant is faked, so any key values
        // stand in for the store's.
        Gateway
            .Setup(g => g.Execute<ConfigurationWriteResult>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDataCommand command, DataStoreTarget _, CancellationToken __) =>
                GenericResult<ConfigurationWriteResult>.Success(
                    // A retire reports the row it retired -- a new version of a domain row is only written once
                    // the previous one is known to have been retired -- and a write reports the row it wrote.
                    command is IConfigurationRetireCommand
                        ? new ConfigurationWriteResult([DomainKeys()], new Dictionary<string, object?>(StringComparer.Ordinal))
                        : new ConfigurationWriteResult([], DomainKeys())));
        Gateway
            .Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success([DomainKeys()]));

        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways
            .Setup(p => p.Get(It.IsAny<string>()))
            .Returns(GenericResult<IConfigurationGateway>.Success(Gateway.Object));

        Implementations = new Mock<IConfigurationSupersedeParticipant<ICalculationEntityImplementationConfiguration>>();
        Implementations
            .Setup(p => p.Supersede(It.IsAny<IDataContainer>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        Provider = new CalculationConfigurationProvider(
            NullLogger<CalculationConfigurationProvider>.Instance, gateways.Object, StoreName, "calc", "CalculationEntity");
        Provider.Register(OneImplementation, Implementations.Object);

        Holds();
    }

    private static Dictionary<string, object?> DomainKeys()
        => new(StringComparer.Ordinal) { ["RowId"] = 1 };

    /// <summary>The store this domain reads from.</summary>
    public Mock<IConfigurationGateway> Gateway { get; }

    /// <summary>The implementation provider every row dispatches to.</summary>
    public Mock<IConfigurationSupersedeParticipant<ICalculationEntityImplementationConfiguration>> Implementations { get; }

    /// <summary>The domain provider under test.</summary>
    public CalculationConfigurationProvider Provider { get; }

    /// <summary>What the store holds: one row per record, each naming the record.</summary>
    public CalculationStore Holds(params ICalculationEntityImplementationConfiguration[] records)
    {
        Rows(GenericResult<IEnumerable<CalculationEntityDomainConfiguration>>.Success(RowsFor(records)));
        foreach (var record in records)
        {
            // A single read asks for the record to a depth; the list read asks for the whole of it.
            Implementations
                .Setup(p => p.Get(record.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<ICalculationEntityImplementationConfiguration>.Success(record));
            Implementations
                .Setup(p => p.Get(record.Id, It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<ICalculationEntityImplementationConfiguration>.Success(record));
        }

        return this;
    }

    /// <summary>What each successive read of the store answers, in order.</summary>
    public CalculationStore HoldsInTurn(params ICalculationEntityImplementationConfiguration[][] reads)
    {
        var rows = Gateway.SetupSequence(g => g.Execute<IEnumerable<CalculationEntityDomainConfiguration>>(
            It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()));
        foreach (var read in reads)
            rows = rows.ReturnsAsync(GenericResult<IEnumerable<CalculationEntityDomainConfiguration>>.Success(RowsFor(read)));

        foreach (var record in reads.SelectMany(read => read))
        {
            // A single read asks for the record to a depth; the list read asks for the whole of it.
            Implementations
                .Setup(p => p.Get(record.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<ICalculationEntityImplementationConfiguration>.Success(record));
            Implementations
                .Setup(p => p.Get(record.Id, It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<ICalculationEntityImplementationConfiguration>.Success(record));
        }

        return this;
    }

    /// <summary>What each successive read of ONE record answers, in order.</summary>
    public CalculationStore RecordInTurn(
        Guid id, params IGenericResult<ICalculationEntityImplementationConfiguration>[] reads)
    {
        // The list read and a single read reach the record through different overloads; one queue
        // answers both, so the reads come back in the order they are made.
        var answers = new Queue<IGenericResult<ICalculationEntityImplementationConfiguration>>(reads);
        Implementations
            .Setup(p => p.Get(id, It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(answers.Dequeue()));
        Implementations
            .Setup(p => p.Get(id, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(answers.Dequeue()));
        return this;
    }

    /// <summary>The store cannot be read.</summary>
    public CalculationStore Unreadable(string reason)
        => Rows(GenericResult<IEnumerable<CalculationEntityDomainConfiguration>>.Failure(new GenericMessage(reason)));

    /// <summary>Reading the store throws.</summary>
    public CalculationStore Throws(Exception error)
    {
        Gateway
            .Setup(g => g.Execute<IEnumerable<CalculationEntityDomainConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(error);
        return this;
    }

    /// <summary>A write lands, and the record it was given is captured.</summary>
    public CalculationStore Saves(Action<ICalculationEntityImplementationConfiguration>? captured = null)
    {
        Implementations
            .Setup(p => p.Save(It.IsAny<ICalculationEntityImplementationConfiguration>(), It.IsAny<IDataContainer>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .Callback<ICalculationEntityImplementationConfiguration, IDataContainer, IReadOnlyDictionary<string, object?>, ConfigurationWriteScope, CancellationToken>((record, _, _, _, _) => captured?.Invoke(record))
            .ReturnsAsync(GenericResult<ConfigurationWriteResult>.Success(new ConfigurationWriteResult([], new Dictionary<string, object?>(StringComparer.Ordinal))));
        return this;
    }

    /// <summary>A write fails.</summary>
    public CalculationStore SaveFails(string reason)
    {
        Implementations
            .Setup(p => p.Save(It.IsAny<ICalculationEntityImplementationConfiguration>(), It.IsAny<IDataContainer>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ConfigurationWriteResult>.Failure(new GenericMessage(reason)));
        return this;
    }

    /// <summary>A write throws.</summary>
    public CalculationStore SaveThrows(Exception error)
    {
        Implementations
            .Setup(p => p.Save(It.IsAny<ICalculationEntityImplementationConfiguration>(), It.IsAny<IDataContainer>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(error);
        return this;
    }

    /// <summary>A delete lands.</summary>
    public CalculationStore Deletes()
    {
        Implementations
            .Setup(p => p.Delete(It.IsAny<IDataContainer>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        return this;
    }

    /// <summary>A delete fails.</summary>
    public CalculationStore DeleteFails(string reason)
    {
        Implementations
            .Setup(p => p.Delete(It.IsAny<IDataContainer>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new GenericMessage(reason)));
        return this;
    }

    /// <summary>A delete throws.</summary>
    public CalculationStore DeleteThrows(Exception error)
    {
        Implementations
            .Setup(p => p.Delete(It.IsAny<IDataContainer>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(error);
        return this;
    }

    /// <summary>How many writes landed.</summary>
    public void SavedTimes(Times times)
        => Implementations.Verify(
            p => p.Save(It.IsAny<ICalculationEntityImplementationConfiguration>(), It.IsAny<IDataContainer>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()), times);

    /// <summary>How many deletes landed.</summary>
    public void DeletedTimes(Times times)
        => Implementations.Verify(p => p.Delete(It.IsAny<IDataContainer>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<ConfigurationWriteScope>(), It.IsAny<CancellationToken>()), times);

    /// <summary>How many records were read.</summary>
    public void ReadRecordsTimes(Times times)
        => Implementations.Verify(p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), times);

    private static List<CalculationEntityDomainConfiguration> RowsFor(IEnumerable<ICalculationEntityImplementationConfiguration> records)
        => records.Select(record => new CalculationEntityDomainConfiguration
        {
            Id = record.Id,
            Name = record.Name,
            Domain = EntityDomain,
            Implementation = OneImplementation,
            Description = record.Description,
            IsEnabled = record.IsEnabled,
        }).ToList();

    private CalculationStore Rows(IGenericResult<IEnumerable<CalculationEntityDomainConfiguration>> rows)
    {
        Gateway
            .Setup(g => g.Execute<IEnumerable<CalculationEntityDomainConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);
        return this;
    }
}
