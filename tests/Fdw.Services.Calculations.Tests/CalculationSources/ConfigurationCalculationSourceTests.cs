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
/// Covers <see cref="ConfigurationCalculationSource"/> — the source that surfaces
/// <c>calc.CalculationEntity</c> rows written through <see cref="ICalculationEntityService"/>,
/// filtered to rows this source itself wrote (CalculationSource == "Configuration").
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "CoreFramework")]
public class ConfigurationCalculationSourceTests
{
    [Fact]
    public async Task ListKeepsOnlyEntitiesTaggedConfiguration()
    {
        var entityServiceMock = new Mock<ICalculationEntityService>();
        entityServiceMock.Setup(s => s.ListCalculations(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<ICalculationEntity>>.Success(
            [
                new TestCalculationEntity { Name = "ConfiguredOne", CalculationSource = "Configuration" },
                new TestCalculationEntity { Name = "VendorOne", CalculationSource = "Vendor" }
            ]));
        var context = new CalculationSourceContext(entityServiceMock.Object, NullLoggerFactory.Instance);
        var sut = new ConfigurationCalculationSource();

        var result = await sut.List(context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(1);
        result.Value![0].Name.ShouldBe("ConfiguredOne");
        result.Value![0].CalculationSource.ShouldBe("Configuration");
        result.Value![0].CalculationEntityId.ShouldNotBeNull();
        result.Value![0].OperatorId.ShouldBeNull();
    }

    [Fact]
    public async Task ListPropagatesServiceFailure()
    {
        var entityServiceMock = new Mock<ICalculationEntityService>();
        entityServiceMock.Setup(s => s.ListCalculations(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<ICalculationEntity>>.Failure(new GenericMessage("boom")));
        var context = new CalculationSourceContext(entityServiceMock.Object, NullLoggerFactory.Instance);
        var sut = new ConfigurationCalculationSource();

        var result = await sut.List(context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldBe("boom");
    }

    [Fact]
    public async Task ResolveByNameNotOwnedByThisSourceReturnsFailure()
    {
        var entityServiceMock = new Mock<ICalculationEntityService>();
        entityServiceMock.Setup(s => s.GetCalculation("VendorOne", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICalculationEntity>.Success(
                new TestCalculationEntity { Name = "VendorOne", CalculationSource = "Vendor" }));
        var context = new CalculationSourceContext(entityServiceMock.Object, NullLoggerFactory.Instance);
        var sut = new ConfigurationCalculationSource();

        var result = await sut.Resolve("VendorOne", context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task ResolveByIdOwnedByThisSourceReturnsSuccess()
    {
        var id = Guid.NewGuid();
        var entityServiceMock = new Mock<ICalculationEntityService>();
        entityServiceMock.Setup(s => s.GetCalculationById(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ICalculationEntity>.Success(
                new TestCalculationEntity { Id = id, Name = "ConfiguredOne", CalculationSource = "Configuration" }));
        var context = new CalculationSourceContext(entityServiceMock.Object, NullLoggerFactory.Instance);
        var sut = new ConfigurationCalculationSource();

        var result = await sut.Resolve(id, context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CalculationEntityId.ShouldBe(id);
    }
}
