using System;
using System.Linq;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.DataTypeNaming;

/// <summary>
/// FDW-1159: a field states its data type by the DataTypes option's name, matched exactly. The fully qualified
/// CLR name is a property of the option, read off its CLR type; it is never what a field stores.
/// </summary>
public class DataTypeNamesTests
{
    [Theory]
    [InlineData("String", typeof(string))]
    [InlineData("Int32", typeof(int))]
    [InlineData("Int64", typeof(long))]
    [InlineData("DateTimeOffset", typeof(DateTimeOffset))]
    [InlineData("Binary", typeof(byte[]))]
    [InlineData("Vector", typeof(float[]))]
    public void AnOptionsNameFindsTheOption(string name, Type clrType)
    {
        var found = DataTypeNames.Find(name);

        found.ShouldNotBeNull();
        found!.Name.ShouldBe(name);
        found.ClrType.ShouldBe(clrType);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("System.String")]
    [InlineData("System.Int32")]
    [InlineData("string")]
    [InlineData("int")]
    [InlineData("INT32")]
    [InlineData("Int")]
    [InlineData("nvarchar")]
    public void AClrNameAKeywordOrAMiscasedNameFindsNothing(string? name)
        => DataTypeNames.Find(name).ShouldBeNull();

    [Theory]
    [InlineData("String", "System.String")]
    [InlineData("Int32", "System.Int32")]
    [InlineData("Decimal", "System.Decimal")]
    [InlineData("Guid", "System.Guid")]
    [InlineData("DateTimeOffset", "System.DateTimeOffset")]
    [InlineData("Binary", "System.Byte[]")]
    public void AnOptionCarriesTheFullNameOfItsClrType(string name, string fullName)
        => DataTypeNames.Find(name)!.FullName.ShouldBe(fullName);

    [Fact]
    public void EveryOptionsFullNameIsItsClrTypesAndNoOptionIsNamedByIt()
    {
        foreach (var option in Fdw.Data.Abstractions.DataTypes.All())
        {
            option.FullName.ShouldBe(option.ClrType.ToString());
            option.FullName.ShouldNotBe(option.Name);
        }
    }

    [Fact]
    public void AClrTypeOneOptionReadsNamesThatOption()
        => DataTypeNames.Reading(typeof(int)).Select(option => option.Name).ShouldBe(["Int32"]);

    [Fact]
    public void AClrTypeTwoOptionsReadNamesBothInNameOrder()
        => DataTypeNames.Reading(typeof(DateTime)).Select(option => option.Name).ShouldBe(["Date", "DateTime"]);

    [Fact]
    public void AClrTypeNoOptionReadsNamesNone()
        => DataTypeNames.Reading(typeof(Uri)).ShouldBeEmpty();
}
