using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// FDW-6129: a variation cites the data set it varies by its durable Id with no foreign key (the data set's row is versioned on write, so its
/// Id repeats across versions). The check the foreign key made is the variation provider's: a variation that names a data set that does not
/// exist is refused, with a code of its own, before anything is written.
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "DataIntegrity")]
public sealed class DataSetVariationBaseCitationTests
{
    private static readonly Guid Base = Guid.NewGuid();

    private static Mock<IDataSetConfigurationProvider> DataSets(bool baseExists)
    {
        var sets = new Mock<IDataSetConfigurationProvider>(MockBehavior.Strict);
        sets.Setup(p => p.Get(Base, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<IGenericResult<IDataSetImplementationConfiguration>>(
                GenericResult<IDataSetImplementationConfiguration>.Success(
                    baseExists ? new SimpleDataSetConfiguration { Id = Base, Name = "Players", Implementation = "Simple" } : null!)));
        sets.Setup(p => p.Save(It.IsAny<IDataSetVariationImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<IGenericResult>(GenericResult.Success()));
        sets.Setup(p => p.Update(It.IsAny<IDataSetVariationImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<IGenericResult>(GenericResult.Success()));
        return sets;
    }

    private static DataSetVariationConfigurationProvider Provider(Mock<IDataSetConfigurationProvider> sets) => new(sets.Object, null!, null);

    private static VariationDataSetConfiguration View() => new() { Name = "ws/weekly", VariationName = "weekly", BaseDataSetId = Base };

    [Fact]
    public async Task ASaveThatCitesNoDataSetIsRefusedWithItsOwnCodeAndWritesNothing()
    {
        var sets = DataSets(baseExists: false);

        var saved = await Provider(sets).Save(View(), "ws/weekly", TestContext.Current.CancellationToken);

        saved.IsSuccess.ShouldBeFalse();
        saved.Code!.Code.ShouldBe("DATASETS-31102");
        saved.Details!.GetValue<string>("dataSetId").ShouldBe(Base.ToString("D"));
        sets.Verify(p => p.Save(It.IsAny<IDataSetVariationImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AnUpdateThatCitesNoDataSetIsRefusedWithItsOwnCodeAndWritesNothing()
    {
        var sets = DataSets(baseExists: false);

        var updated = await Provider(sets).Update(View(), "ws/weekly", TestContext.Current.CancellationToken);

        updated.IsSuccess.ShouldBeFalse();
        updated.Code!.Code.ShouldBe("DATASETS-31102");
        sets.Verify(p => p.Update(It.IsAny<IDataSetVariationImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ASaveThatCitesADataSetThatExistsIsWritten()
    {
        var sets = DataSets(baseExists: true);

        var saved = await Provider(sets).Save(View(), "ws/weekly", TestContext.Current.CancellationToken);

        saved.IsSuccess.ShouldBeTrue();
        sets.Verify(p => p.Save(It.IsAny<IDataSetVariationImplementationConfiguration>(), "DataSet", "Variation", "ws/weekly", It.IsAny<CancellationToken>()), Times.Once);
    }
}
