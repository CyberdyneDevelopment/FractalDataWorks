using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests.DataQuery;

/// <summary>
/// FDW-1075: an export streams the data set's own rows, page by page through <see cref="DataSetQuery"/>, never the table under it.
/// </summary>
public sealed class DataSetRecordSourceTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task EachRecordCarriesOnlyTheDeclaredFields()
    {
        var gateway = DataSetQueryTests.Gateway([DataSetQueryTests.Row(("Name", "Ada"), ("Salary", 100m), ("SSN", "123-45-6789"))]);
        var query = new DataSetQuery(DataSetQueryTests.Provider(gateway), NullLogger.Instance);

        using var source = DataSetRecordSource.Open(query, DataSetQueryTests.People(), 10, 100, NullLogger.Instance).Value.ShouldNotBeNull();
        var records = await Records(source);

        source.Schema.Fields.Select(f => f.Name).ShouldBe(new[] { "Name", "Salary" });
        var record = records.ShouldHaveSingleItem();
        record.FieldCount.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task PagesAreReadInKeyOrderUntilTheStoreHasNoMore()
    {
        var gateway = new Mock<IDataGateway>();
        gateway
            .SetupSequence(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(
                [DataSetQueryTests.Row(("Name", "Ada")), DataSetQueryTests.Row(("Name", "Grace")), DataSetQueryTests.Row(("Name", "Edsger"))]))
            .ReturnsAsync(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success([DataSetQueryTests.Row(("Name", "Edsger"))]));
        var query = new DataSetQuery(DataSetQueryTests.Provider(gateway), NullLogger.Instance);

        using var source = DataSetRecordSource.Open(query, DataSetQueryTests.People(), 2, 100, NullLogger.Instance).Value.ShouldNotBeNull();
        var records = await Records(source);

        records.Count.ShouldBe(3);
        gateway.Invocations.Count.ShouldBe(2);
        foreach (var invocation in gateway.Invocations)
        {
            var command = (IQueryCommand)invocation.Arguments[0];
            command.Ordering.ShouldNotBeNull().OrderedFields.Select(f => f.PropertyName).ShouldBe(new[] { "Name" });
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void ADataSetWithNoKeyFieldIsRefusedRatherThanReadInAnOrderThatCouldRepeatOrDropRows()
    {
        var keyless = DataSetQueryTests.People();
        foreach (var field in keyless.Fields)
            field.IsKey = false;
        var query = new DataSetQuery(DataSetQueryTests.Provider(DataSetQueryTests.Gateway([])), NullLogger.Instance);

        var opened = DataSetRecordSource.Open(query, keyless, 10, 100, NullLogger.Instance);

        opened.IsFailure.ShouldBeTrue();
        opened.CurrentMessage.ShouldNotBeNull().ShouldContain("declares none");
    }

    private static async Task<List<DataRecord>> Records(IRecordSource<DataRecord> source)
    {
        var records = new List<DataRecord>();
        await foreach (var record in source.Read(TestContext.Current.CancellationToken))
        {
            record.IsSuccess.ShouldBeTrue();
            records.Add(record.Value);
        }

        return records;
    }
}
