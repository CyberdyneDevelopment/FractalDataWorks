using Fdw.Data.MsSql;

namespace Fdw.Data.MsSql.Tests;

/// <summary>
/// A type name as SQL Server reports it resolves to its native type, including the ones whose option names are capitalised
/// because the SQL name is a C# keyword (FDW-1588).
/// </summary>
[Collection(nameof(DataMsSqlTestCollection))]
public class MsSqlNativeTypeResolverTests
{
    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("int", "Int32")]
    [InlineData("decimal", "Decimal")]
    [InlineData("float", "Double")]
    [InlineData("nvarchar", "String")]
    [InlineData("NVARCHAR", "String")]
    [InlineData("datetime", "DateTime")]
    [InlineData("numeric", "Decimal")]
    public void ResolveFindsTheNativeTypeBySqlName(string sqlTypeName, string portableType)
    {
        var resolved = MsSqlNativeTypeResolver.Resolve(sqlTypeName);

        resolved.ShouldNotBe(MsSqlNativeTypes.NotFound);
        resolved.NativeName.ShouldBe(sqlTypeName.ToLowerInvariant());
        resolved.AbstractType.Name.ShouldBe(portableType);
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("sql_variant")]
    [InlineData("")]
    [InlineData(null)]
    public void ResolveAnswersNotFoundForANameWithoutANativeType(string? sqlTypeName)
    {
        MsSqlNativeTypeResolver.Resolve(sqlTypeName).ShouldBe(MsSqlNativeTypes.NotFound);
    }
}
