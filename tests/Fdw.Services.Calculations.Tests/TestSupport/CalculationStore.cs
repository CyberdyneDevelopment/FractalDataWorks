using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

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

    public CalculationStore()
    {
        Gateway = new Mock<IConfigurationGateway>();
        Gateway
            .Setup(g => g.Execute<DomainConfiguration>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DomainConfiguration>.Success(default!));

        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways
            .Setup(p => p.Get(It.IsAny<string>()))
            .Returns(GenericResult<IConfigurationGateway>.Success(Gateway.Object));

        Implementations = new Mock<IImplementationConfigurationProvider<ICalculationEntityImplementationConfiguration>>();
        Provider = new CalculationConfigurationProvider(
            NullLogger<CalculationConfigurationProvider>.Instance, gateways.Object, "PlatformConfiguration");
        Provider.Register(OneImplementation, Implementations.Object);

        Holds();
    }

    /// <summary>The store this domain reads from.</summary>
    public Mock<IConfigurationGateway> Gateway { get; }

    /// <summary>The implementation provider every row dispatches to.</summary>
    public Mock<IImplementationConfigurationProvider<ICalculationEntityImplementationConfiguration>> Implementations { get; }

    /// <summary>The domain provider under test.</summary>
    public CalculationConfigurationProvider Provider { get; }

    /// <summary>What the store holds: one row per record, each naming the record.</summary>
    public CalculationStore Holds(params ICalculationEntityImplementationConfiguration[] records)
    {
        Rows(GenericResult<IEnumerable<DomainConfiguration>>.Success(RowsFor(records)));
        foreach (var record in records)
        {
            Implementations
                .Setup(p => p.Get(record.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<ICalculationEntityImplementationConfiguration>.Success(record));
        }

        return this;
    }

    /// <summary>What each successive read of the store answers, in order.</summary>
    public CalculationStore HoldsInTurn(params ICalculationEntityImplementationConfiguration[][] reads)
    {
        var rows = Gateway.SetupSequence(g => g.Execute<IEnumerable<DomainConfiguration>>(
            It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()));
        foreach (var read in reads)
            rows = rows.ReturnsAsync(GenericResult<IEnumerable<DomainConfiguration>>.Success(RowsFor(read)));

        foreach (var record in reads.SelectMany(read => read))
        {
            Implementations
                .Setup(p => p.Get(record.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<ICalculationEntityImplementationConfiguration>.Success(record));
        }

        return this;
    }

    /// <summary>What each successive read of ONE record answers, in order.</summary>
    public CalculationStore RecordInTurn(
        Guid id, params IGenericResult<ICalculationEntityImplementationConfiguration>[] reads)
    {
        var record = Implementations.SetupSequence(p => p.Get(id, It.IsAny<CancellationToken>()));
        foreach (var read in reads) record = record.ReturnsAsync(read);
        return this;
    }

    /// <summary>The store cannot be read.</summary>
    public CalculationStore Unreadable(string reason)
        => Rows(GenericResult<IEnumerable<DomainConfiguration>>.Failure(new GenericMessage(reason)));

    /// <summary>Reading the store throws.</summary>
    public CalculationStore Throws(Exception error)
    {
        Gateway
            .Setup(g => g.Execute<IEnumerable<DomainConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(error);
        return this;
    }

    /// <summary>A write lands, and the record it was given is captured.</summary>
    public CalculationStore Saves(Action<ICalculationEntityImplementationConfiguration>? captured = null)
    {
        Implementations
            .Setup(p => p.Save(It.IsAny<ICalculationEntityImplementationConfiguration>(), It.IsAny<CancellationToken>()))
            .Callback<ICalculationEntityImplementationConfiguration, CancellationToken>((record, _) => captured?.Invoke(record))
            .ReturnsAsync((ICalculationEntityImplementationConfiguration record, CancellationToken _) =>
                GenericResult<ICalculationEntityImplementationConfiguration>.Success(record));
        return this;
    }

    /// <summary>A write fails.</summary>
    public CalculationStore SaveFails(string reason)
    {
        Implementations
            .Setup(p => p.Save(It.IsAny<ICalculationEntityImplementationConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICalculationEntityImplementationConfiguration>.Failure(new GenericMessage(reason)));
        return this;
    }

    /// <summary>A write throws.</summary>
    public CalculationStore SaveThrows(Exception error)
    {
        Implementations
            .Setup(p => p.Save(It.IsAny<ICalculationEntityImplementationConfiguration>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(error);
        return this;
    }

    /// <summary>A delete lands.</summary>
    public CalculationStore Deletes()
    {
        Implementations
            .Setup(p => p.Delete(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        return this;
    }

    /// <summary>A delete fails.</summary>
    public CalculationStore DeleteFails(string reason)
    {
        Implementations
            .Setup(p => p.Delete(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new GenericMessage(reason)));
        return this;
    }

    /// <summary>A delete throws.</summary>
    public CalculationStore DeleteThrows(Exception error)
    {
        Implementations
            .Setup(p => p.Delete(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(error);
        return this;
    }

    /// <summary>How many writes landed.</summary>
    public void SavedTimes(Times times)
        => Implementations.Verify(
            p => p.Save(It.IsAny<ICalculationEntityImplementationConfiguration>(), It.IsAny<CancellationToken>()), times);

    /// <summary>How many deletes landed.</summary>
    public void DeletedTimes(Times times)
        => Implementations.Verify(p => p.Delete(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), times);

    /// <summary>How many records were read.</summary>
    public void ReadRecordsTimes(Times times)
        => Implementations.Verify(p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), times);

    private static List<DomainConfiguration> RowsFor(IEnumerable<ICalculationEntityImplementationConfiguration> records)
        => records.Select(record => new DomainConfiguration
        {
            Id = record.Id,
            Name = record.Name,
            Domain = EntityDomain,
            Implementation = OneImplementation,
        }).ToList();

    private CalculationStore Rows(IGenericResult<IEnumerable<DomainConfiguration>> rows)
    {
        Gateway
            .Setup(g => g.Execute<IEnumerable<DomainConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);
        return this;
    }
}
