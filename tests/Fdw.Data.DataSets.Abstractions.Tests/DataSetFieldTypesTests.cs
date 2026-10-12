using Fdw.Data.DataSets.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Abstractions.Tests;

/// <summary>
/// FDW-997: a query checks a filter value against the type a data set field is declared with, so the
/// names a field is declared with have to resolve, and a name that is not one has to say so. FDW-1159: the
/// names are the DataTypes options', matched exactly; a CLR type name is not one.
/// </summary>
public sealed class DataSetFieldTypesTests
{
    [Theory]
    [InlineData("Guid", typeof(Guid))]
    [InlineData("Int16", typeof(short))]
    [InlineData("Int32", typeof(int))]
    [InlineData("Int64", typeof(long))]
    [InlineData("Byte", typeof(byte))]
    [InlineData("Bool", typeof(bool))]
    [InlineData("Decimal", typeof(decimal))]
    [InlineData("Float", typeof(float))]
    [InlineData("Double", typeof(double))]
    [InlineData("Date", typeof(DateTime))]
    [InlineData("DateTime", typeof(DateTime))]
    [InlineData("DateTimeOffset", typeof(DateTimeOffset))]
    [InlineData("String", typeof(string))]
    [InlineData("Binary", typeof(byte[]))]
    [InlineData("Time", typeof(TimeSpan))]
    public void ADataTypeNameResolvesToItsClrType(string name, Type expected)
        => DataSetFieldTypes.Resolve(name).ShouldBe(expected);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Some.Custom.Thing")]
    [InlineData("varchar")]
    [InlineData("System.String")]
    [InlineData("System.Int32")]
    [InlineData("string")]
    [InlineData("int")]
    [InlineData("INT32")]
    [InlineData("Boolean")]
    public void ANameThatIsNotADataTypeResolvesToNothing(string? name)
        => DataSetFieldTypes.Resolve(name).ShouldBeNull();
}
