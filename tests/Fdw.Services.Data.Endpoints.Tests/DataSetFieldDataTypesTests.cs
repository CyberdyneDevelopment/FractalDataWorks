using System.Linq;
using Fdw.Services.Data.Clients.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-1159: a data set field states its data type as a DataTypes option's name. A create or an update that
/// states a CLR type name, a C# keyword or a miscased name is refused with the name and the options; a field
/// that states none is one nobody has typed yet and passes.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "DataIntegrity")]
public class DataSetFieldDataTypesTests
{
    private static CreateDataSetFieldRequest Field(string name, string dataType) => new() { Name = name, DataType = dataType };

    [Fact]
    public void FieldsNamingDataTypesOptionsPass()
        => DataSetQueryHelper.ValidateFieldDataTypes(
                [Field("Id", "Guid"), Field("Units", "Int32"), Field("Seen", "DateTimeOffset")], NullLogger.Instance)
            .IsSuccess.ShouldBeTrue();

    [Fact]
    public void AFieldThatStatesNoDataTypeIsNotTypedYetAndPasses()
        => DataSetQueryHelper.ValidateFieldDataTypes([Field("Id", "Guid"), Field("Sketched", string.Empty)], NullLogger.Instance)
            .IsSuccess.ShouldBeTrue();

    [Theory]
    [InlineData("System.String")]
    [InlineData("System.Int32")]
    [InlineData("string")]
    [InlineData("int")]
    [InlineData("INT32")]
    [InlineData("Int")]
    public void AClrNameAKeywordOrAMiscasedNameIsRefused(string dataType)
        => DataSetQueryHelper.ValidateFieldDataTypes([Field("Id", "Guid"), Field("Units", dataType)], NullLogger.Instance)
            .IsSuccess.ShouldBeFalse();

    [Fact]
    public void ARequestThatCarriesNoFieldsHasNothingToCheck()
        => DataSetQueryHelper.ValidateFieldTypes(null, NullLogger.Instance).IsSuccess.ShouldBeTrue();

    [Fact]
    public void TheCombinedCheckRefusesAFieldWhoseDataTypeIsAClrName()
        => DataSetQueryHelper.ValidateFieldTypes([Field("Units", "System.Int32")], NullLogger.Instance).IsSuccess.ShouldBeFalse();

    [Fact]
    public void TheCombinedCheckRefusesAFieldWhoseDatumTypeIsNotOne()
        => DataSetQueryHelper.ValidateFieldTypes(
                [new CreateDataSetFieldRequest { Name = "Units", DataType = "Int32", DatumTypeId = -1 }], NullLogger.Instance)
            .IsSuccess.ShouldBeFalse();

    [Fact]
    public void MappedFieldsCarryTheStatedNameUnchanged()
    {
        var mapped = DataSetQueryHelper.MapFields([new CreateDataSetFieldRequest { Name = "Units", DataType = "Int32", IsFilterable = true }]);

        mapped.Single().TypeName.ShouldBe("Int32");
    }
}
