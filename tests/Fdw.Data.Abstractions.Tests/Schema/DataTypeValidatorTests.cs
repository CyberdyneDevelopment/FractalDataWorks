using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Results;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.Schema;

/// <summary>
/// FDW-1103: a container field states its portable data type as a DataTypes option, matched exactly; a missing or
/// unknown name is refused with the name and the options in the reason the caller sees.
/// </summary>
public class DataTypeValidatorTests
{
    [Theory]
    [InlineData("String")]
    [InlineData("Int32")]
    [InlineData("DateTimeOffset")]
    [InlineData("Guid")]
    public void APortableTypeThatIsADataTypesOptionIsAccepted(string dataType)
        => DataTypeValidator.Validate(dataType, NullLogger.Instance).IsSuccess.ShouldBeTrue();

    [Theory]
    [InlineData("nvarchar")]
    [InlineData("int")]
    [InlineData("INT32")]
    [InlineData("Int")]
    public void ANativeOrMiscasedNameIsRefused(string dataType)
        => DataTypeValidator.Validate(dataType, NullLogger.Instance).IsSuccess.ShouldBeFalse();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AMissingPortableTypeIsRefusedNotDefaulted(string? dataType)
        => DataTypeValidator.Validate(dataType, NullLogger.Instance).IsSuccess.ShouldBeFalse();

    [Fact]
    public void TheFirstRefusedFieldDecidesASetOfFields()
        => DataTypeValidator.Validate(new[] { "String", "varchar", "Int64" }, NullLogger.Instance).IsSuccess.ShouldBeFalse();

    [Fact]
    public void TheRefusalShowsTheCallerTheNameAndTheOptions()
        => DataStoresResultCodes.ByName("DataTypeUnknown")
            .FormatClientMessage(ResultDetails.Create("DataType", "varchar", "Options", "Guid, String"))
            .ShouldBe("Data type 'varchar' is not one of the data types (Guid, String)");
}
