using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// FDW-3630: listing the data sets returns the failure of a configuration read that failed, with its own coded message, and not an empty list that reads as "there are none".
/// </summary>
public sealed class DataSetProviderGetAllTests
{
    private static DataSetProvider Over(IGenericResult<IReadOnlyList<IDataSetImplementationConfiguration>> read)
    {
        var configuration = new Mock<IDataSetConfigurationProvider>();
        configuration.Setup(c => c.Get(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(read);
        return new DataSetProvider(NullLogger<DataSetProvider>.Instance, configuration.Object);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets")]
    public async Task AFailedConfigurationReadIsAFailureWithItsOwnCode()
    {
        var provider = Over(GenericResult<IReadOnlyList<IDataSetImplementationConfiguration>>.Failure(DataServiceResultCodes.ByName("DataSetNotFound")));

        var result = await provider.Get(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.First().Code.ShouldBe("DATA-71019");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets")]
    public async Task AProviderWithNoConfigurationSourceFailsWithItsOwnCode()
    {
        var result = await new DataSetProvider(NullLogger<DataSetProvider>.Instance).Get(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.First().Code.ShouldBe("DATA-60360");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets")]
    public async Task ASuccessfulReadOfNoDataSetsIsStillAnEmptySuccess()
    {
        var provider = Over(GenericResult<IReadOnlyList<IDataSetImplementationConfiguration>>.Success(new List<IDataSetImplementationConfiguration>()));

        var result = await provider.Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets")]
    public async Task ASuccessfulReadReturnsTheDataSetsItFound()
    {
        var found = Mock.Of<IDataSetImplementationConfiguration>(d => d.Name == "Sales");
        var provider = Over(GenericResult<IReadOnlyList<IDataSetImplementationConfiguration>>.Success(new List<IDataSetImplementationConfiguration> { found }));

        var result = await provider.Get(TestContext.Current.CancellationToken);

        result.Value!.ShouldHaveSingleItem().ShouldBeSameAs(found);
    }

    // ── by name and by id (FDW-3630): a failed read is its own failure; NotFound is only for a read that succeeded and found nothing ──

    private static DataSetProvider OverNamedAndIdRead(IGenericResult<IDataSetImplementationConfiguration> read)
    {
        var configuration = new Mock<IDataSetConfigurationProvider>();
        configuration.Setup(c => c.Get(It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(read);
        configuration.Setup(c => c.Get(It.IsAny<System.Guid>(), It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(read);
        return new DataSetProvider(NullLogger<DataSetProvider>.Instance, configuration.Object);
    }

    private static IGenericResult<IDataSetImplementationConfiguration> Failed() =>
        GenericResult<IDataSetImplementationConfiguration>.Failure(DataServiceResultCodes.ByName("DataSetNotFound"));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets")]
    public async Task AFailedReadByNameIsItsOwnFailureAndNotDataSetNotFound()
    {
        var result = await OverNamedAndIdRead(Failed()).Get("Sales", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.First().Code.ShouldBe("DATA-71134");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets")]
    public async Task AFailedReadByIdIsItsOwnFailureAndNotDataSetNotFound()
    {
        var result = await OverNamedAndIdRead(Failed()).Get(System.Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.First().Code.ShouldBe("DATA-71135");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets")]
    public async Task AReadByNameThatSucceededAndFoundNothingIsDataSetNotFound()
    {
        var result = await OverNamedAndIdRead(GenericResult<IDataSetImplementationConfiguration>.Success(default!)).Get("Sales", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.First().Code.ShouldNotBe("DATA-71134");
        result.FailureReason().Code.ShouldBe(DataServiceResultCodes.ByName("DataSetNotFound").Code);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets")]
    public async Task AReadByIdThatSucceededAndFoundNothingIsDataSetNotFound()
    {
        var result = await OverNamedAndIdRead(GenericResult<IDataSetImplementationConfiguration>.Success(default!)).Get(System.Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.First().Code.ShouldNotBe("DATA-71135");
        result.FailureReason().Code.ShouldBe(DataServiceResultCodes.ByName("DataSetNotFound").Code);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets")]
    public async Task AProviderWithNoConfigurationSourceCannotAnswerByNameOrId()
    {
        var provider = new DataSetProvider(NullLogger<DataSetProvider>.Instance);

        (await provider.Get("Sales", TestContext.Current.CancellationToken)).Messages.First().Code.ShouldBe("DATA-60360");
        (await provider.Get(System.Guid.NewGuid(), TestContext.Current.CancellationToken)).Messages.First().Code.ShouldBe("DATA-60360");
    }
}
