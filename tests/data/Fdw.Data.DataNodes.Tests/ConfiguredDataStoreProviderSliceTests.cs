using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Builders;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.DataNodes.Tests;

/// <summary>
/// A container is got by reading the containers a command names and the ones their keys refer to, and no other (FDW-4328).
/// </summary>
public sealed class ConfiguredDataStoreProviderSliceTests
{
    private readonly Mock<IDomainConfigurationProvider<IDataStoreImplementationConfiguration>> _configurations = new();
    private readonly Mock<IDataStoreBuilderSelector> _selector = new();

    private ConfiguredDataStoreProvider CreateSut()
    {
        _selector
            .Setup(s => s.Select(It.IsAny<IDataStoreImplementationConfiguration>(), It.IsAny<ILogger>()))
            .Returns(() => GenericResult<IDataStoreBuilder>.Success(new GenericDataStoreBuilder(FormatTypes.NotFound)));
        return new ConfiguredDataStoreProvider(new Mock<ILogger<ConfiguredDataStoreProvider>>().Object, _configurations.Object, _selector.Object);
    }

    private static DataStoreImplementationConfiguration StoreHolding(string container, string? pointsAt)
    {
        var keys = new List<DataContainerKeyConfiguration>();
        if (pointsAt is not null)
        {
            keys.Add(new DataContainerKeyConfiguration
            {
                Id = Guid.NewGuid(), Name = $"FK_{container}", TypeId = "Foreign", ReferencedContainerName = pointsAt, ReferencedKeyName = $"Surrogate_{pointsAt}",
                KeyFields = [new DataContainerKeyFieldConfiguration { Id = Guid.NewGuid(), Name = "Id", Ordinal = 0 }],
            });
        }

        return new DataStoreImplementationConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Rig",
            Paths =
            [
                new DataPathConfiguration
                {
                    Id = Guid.NewGuid(),
                    Name = "dsperf",
                    Containers =
                    [
                        new DataContainerConfiguration
                        {
                            Id = Guid.NewGuid(), Name = container, Fields = [new DataContainerFieldConfiguration { Id = Guid.NewGuid(), Name = "Id", Ordinal = 0 }], Keys = keys,
                        },
                    ],
                },
            ],
        };
    }

    private void Answers(string names, DataStoreImplementationConfiguration store)
        => _configurations
            .Setup(p => p.Get("Rig", ConfigurationLevels.WholeAggregate, It.Is<ChildNarrowing>(n => string.Join(",", n.Values) == names), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataStoreImplementationConfiguration>.Success(store));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Performance")]
    public async Task TheNamedContainerIsReadThenTheOnesItsKeysReferToAndNoWholeStoreIsComposed()
    {
        Answers("A", StoreHolding("A", pointsAt: "B"));
        Answers("B", StoreHolding("B", pointsAt: null));

        var result = await CreateSut().Get("Rig", "dsperf", "A", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Name.ShouldBe("A");
        result.Value.Keys.Single().ReferencedContainer!.Name.ShouldBe("B");
        _configurations.Verify(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _configurations.Verify(p => p.Get(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _configurations.Verify(
            p => p.Get(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<ChildNarrowing>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Performance")]
    public async Task AContainerThatPointsAtNoOtherIsReadOnce()
    {
        Answers("A", StoreHolding("A", pointsAt: null));

        var result = await CreateSut().Get("Rig", "dsperf", "A", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _configurations.Verify(
            p => p.Get(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<ChildNarrowing>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AJoinedContainerIsNamedWithTheContainerTheCommandRunsOn()
    {
        Answers("A,C", StoreHolding("A", pointsAt: null));

        var result = await CreateSut().Get("Rig", "dsperf", "A", ["C"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _configurations.Verify(
            p => p.Get("Rig", ConfigurationLevels.WholeAggregate, It.Is<ChildNarrowing>(n => n.Values.SequenceEqual(new[] { "A", "C" })), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AContainerTheStoreDoesNotHoldIsNotFoundAndNothingIsSubstituted()
    {
        Answers("Missing", StoreHolding("A", pointsAt: null));

        var result = await CreateSut().Get("Rig", "dsperf", "Missing", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AReadThatBrokeKeepsItsOwnFailure()
    {
        _configurations
            .Setup(p => p.Get("Rig", ConfigurationLevels.WholeAggregate, It.IsAny<ChildNarrowing>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataStoreImplementationConfiguration>.Failure(new Fdw.Messages.GenericMessage("the configuration read broke")));

        var result = await CreateSut().Get("Rig", "dsperf", "A", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        FailureCategory.IsMissing(result).ShouldBeFalse();
        _selector.Verify(s => s.Select(It.IsAny<IDataStoreImplementationConfiguration>(), It.IsAny<ILogger>()), Times.Never);
    }
}
