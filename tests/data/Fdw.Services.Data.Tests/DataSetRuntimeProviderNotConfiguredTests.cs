using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// FDW-3630: the domain provider answers a data set that is not configured with a success that holds nothing. The runtime provider turns that into DataSetNotFound; it used to hand the
/// nothing to the builder, which threw.
/// </summary>
public sealed class DataSetRuntimeProviderNotConfiguredTests
{
    private static DataSetRuntimeProvider Over(Mock<IDataSetConfigurationProvider> configuration)
        => new(configuration.Object, new Mock<IDataSetBuilder>().Object, NullLogger<DataSetRuntimeProvider>.Instance);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets")]
    public async Task ANameNobodyConfiguredIsDataSetNotFound()
    {
        var configuration = new Mock<IDataSetConfigurationProvider>();
        configuration.Setup(c => c.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Success(default!));

        var result = await Over(configuration).Get("Sales", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.FailureReason().Code.ShouldBe(DataServiceResultCodes.ByName("DataSetNotFound").Code);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets")]
    public async Task AnIdNobodyConfiguredIsDataSetNotFound()
    {
        var configuration = new Mock<IDataSetConfigurationProvider>();
        configuration.Setup(c => c.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Success(default!));

        var result = await Over(configuration).Get(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.FailureReason().Code.ShouldBe(DataServiceResultCodes.ByName("DataSetNotFound").Code);
    }
}
