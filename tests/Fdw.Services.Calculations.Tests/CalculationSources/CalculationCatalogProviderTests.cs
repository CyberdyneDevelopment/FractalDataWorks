using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Calculations.Abstractions.CalculationSources;
using Fdw.Services.Calculations.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Calculations.Tests.CalculationSources;

/// <summary>
/// Covers <see cref="CalculationCatalogProvider"/> — the union provider assembling the unified
/// calculation catalog across every registered <see cref="CalculationSourceTypes"/> option.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "CoreFramework")]
public class CalculationCatalogProviderTests
{
    [Fact]
    public async Task GetReturnsUnionOfDefaultAndConfigurationSources()
    {
        var entityServiceMock = new Mock<ICalculationEntityService>();
        entityServiceMock.Setup(s => s.ListCalculations(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<ICalculationEntity>>.Success(
            [
                new TestCalculationEntity { Name = "ConfiguredOne", CalculationSource = "Configuration" }
            ]));
        var sut = new CalculationCatalogProvider(entityServiceMock.Object, NullLoggerFactory.Instance);

        var result = await sut.Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldContain(i => i.Name == "Sum" && i.CalculationSource == "Default");
        result.Value!.ShouldContain(i => i.Name == "ConfiguredOne" && i.CalculationSource == "Configuration");
    }

    [Fact]
    public async Task GetSurfacesAFailingSourceRatherThanSwallowingIt()
    {
        var entityServiceMock = new Mock<ICalculationEntityService>();
        entityServiceMock.Setup(s => s.ListCalculations(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<ICalculationEntity>>.Failure(new GenericMessage("boom")));
        var sut = new CalculationCatalogProvider(entityServiceMock.Object, NullLoggerFactory.Instance);

        var result = await sut.Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task GetBySourceAndNameDispatchesToTheNamedSource()
    {
        var sut = new CalculationCatalogProvider(Mock.Of<ICalculationEntityService>(), NullLoggerFactory.Instance);

        var result = await sut.Get("Default", "Sum", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Name.ShouldBe("Sum");
        result.Value!.CalculationSource.ShouldBe("Default");
    }

    [Fact]
    public async Task GetByUnknownSourceReturnsNotFoundWithoutDispatching()
    {
        var sut = new CalculationCatalogProvider(Mock.Of<ICalculationEntityService>(), NullLoggerFactory.Instance);

        var result = await sut.Get("Bogus", "Sum", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task GetByIdIteratesSourcesUntilOneResolvesIt()
    {
        var id = Guid.NewGuid();
        var entityServiceMock = new Mock<ICalculationEntityService>();
        entityServiceMock.Setup(s => s.GetCalculationById(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICalculationEntity>.Success(
                new TestCalculationEntity { Id = id, Name = "ConfiguredOne", CalculationSource = "Configuration" }));
        var sut = new CalculationCatalogProvider(entityServiceMock.Object, NullLoggerFactory.Instance);

        var result = await sut.Get(id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CalculationEntityId.ShouldBe(id);
    }

    [Fact]
    public async Task GetByIdNotFoundInAnySourceReturnsFailure()
    {
        var entityServiceMock = new Mock<ICalculationEntityService>();
        entityServiceMock.Setup(s => s.GetCalculationById(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICalculationEntity>.Failure(new GenericMessage("not found")));
        var sut = new CalculationCatalogProvider(entityServiceMock.Object, NullLoggerFactory.Instance);

        var result = await sut.Get(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }
}
