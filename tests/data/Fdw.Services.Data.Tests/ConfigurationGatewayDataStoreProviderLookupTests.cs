using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// FDW-5875: the store provider the host resolves answers "not found" only for a store that is not there. A read that failed (a dependency
/// down, a transient failure, a denial, a timeout) keeps its own failure, and a row that is there without a name is a configuration fault, for
/// the lookup by id as for the lookup by name (FDW-4281 gave the guard to the lookup by name only).
/// </summary>
public sealed class ConfigurationGatewayDataStoreProviderLookupTests
{
    private readonly Mock<IDomainConfigurationProvider<IDataStoreImplementationConfiguration>> _configuration = new();

    private ConfigurationGatewayDataStoreProvider CreateSut()
        => new(
            NullLogger<ConfigurationGatewayDataStoreProvider>.Instance,
            new ConfiguredDataStoreProvider(
                NullLogger<ConfiguredDataStoreProvider>.Instance, _configuration.Object, new Mock<IDataStoreBuilderSelector>().Object),
            _configuration.Object);

    private static GenericMessage CodedFailure(string? code)
        => code is null
            ? new GenericMessage("the configuration read failed")
            : new GenericMessage(MessageSeverity.Error, "the configuration read failed", code);

    [Theory]
    [InlineData(null)]
    [InlineData("TST-70001")]
    [InlineData("TST-80001")]
    [InlineData("TST-100001")]
    [InlineData("TST-110001")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task GetByIdKeepsTheCauseOfAFailedReadOfAnyKindAndDoesNotAnswerNotFound(string? code)
    {
        var id = Guid.NewGuid();
        _configuration
            .Setup(p => p.Get(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreImplementationConfiguration>.Failure(CodedFailure(code)));
        var sut = CreateSut();

        var result = await sut.Get(id, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        FailureCategory.IsMissing(result).ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("the configuration read failed");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("TST-70001")]
    [InlineData("TST-80001")]
    [InlineData("TST-100001")]
    [InlineData("TST-110001")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task GetByNameKeepsTheCauseOfAFailedReadOfAnyKindAndDoesNotAnswerNotFound(string? code)
    {
        _configuration
            .Setup(p => p.Get("Store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreImplementationConfiguration>.Failure(CodedFailure(code)));
        var sut = CreateSut();

        var result = await sut.Get("Store1", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        FailureCategory.IsMissing(result).ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("the configuration read failed");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task GetByIdAnswersTheMissingCategoryForAStoreThatDoesNotExist()
    {
        var id = Guid.NewGuid();
        _configuration
            .Setup(p => p.Get(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreImplementationConfiguration>.Success(default!));
        var sut = CreateSut();

        var result = await sut.Get(id, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        FailureCategory.IsMissing(result).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task GetByIdAnswersTheMissingCategoryWhenTheReadItselfFailedAsMissing()
    {
        var id = Guid.NewGuid();
        _configuration
            .Setup(p => p.Get(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreImplementationConfiguration>.Failure(CodedFailure("TST-31100")));
        var sut = CreateSut();

        var result = await sut.Get(id, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        FailureCategory.IsMissing(result).ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task GetByIdAnswersARowWithoutANameAsAConfigurationFaultAndNotAsMissing(string name)
    {
        var id = Guid.NewGuid();
        _configuration
            .Setup(p => p.Get(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreImplementationConfiguration>.Success(new DataStoreImplementationConfiguration { Id = id, Name = name }));
        var sut = CreateSut();

        var result = await sut.Get(id, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        FailureCategory.IsMissing(result).ShouldBeFalse();
        FailureCategory.Of(result).ShouldNotBeNull().Id.ShouldBe(6);
        _configuration.Verify(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
