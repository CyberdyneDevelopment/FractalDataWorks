using System.Data;
using Fdw.Schema;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.Schema;

/// <summary>
/// The field type name a <see cref="FieldToColumnAdapter"/> reads as a SQL column type goes through <see cref="SqlTypeWords"/>;
/// a name outside the set is whatever the field's source called it, and reads as NVarChar.
/// </summary>
public sealed class FieldToColumnAdapterTests
{
    private static SqlDbType SqlTypeOf(string typeName) =>
        new FieldToColumnAdapter(new Field
        {
            Name = "Column1",
            FieldType = new SimpleFieldType { TypeName = typeName, ClrType = typeof(object) },
            Role = DatumTypes.Attribute,
        }).SqlType;

    [Theory]
    [InlineData("int", SqlDbType.Int)]
    [InlineData("Int32", SqlDbType.Int)]
    [InlineData("long", SqlDbType.BigInt)]
    [InlineData("short", SqlDbType.SmallInt)]
    [InlineData("byte", SqlDbType.TinyInt)]
    [InlineData("bool", SqlDbType.Bit)]
    [InlineData("decimal", SqlDbType.Decimal)]
    [InlineData("float", SqlDbType.Real)]
    [InlineData("double", SqlDbType.Float)]
    [InlineData("datetime", SqlDbType.DateTime2)]
    [InlineData("datetimeoffset", SqlDbType.DateTimeOffset)]
    [InlineData("date", SqlDbType.Date)]
    [InlineData("time", SqlDbType.Time)]
    [InlineData("guid", SqlDbType.UniqueIdentifier)]
    [InlineData("string", SqlDbType.VarChar)]
    [InlineData("nvarchar", SqlDbType.NVarChar)]
    [InlineData("varbinary", SqlDbType.VarBinary)]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ANameTheSetHoldsMapsToItsSqlType(string typeName, SqlDbType expected) =>
        SqlTypeOf(typeName).ShouldBe(expected);

    [Theory]
    [InlineData("json")]
    [InlineData("xml")]
    [InlineData("geography")]
    [InlineData("")]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ANameOutsideTheSetReadsAsNVarChar(string typeName) =>
        SqlTypeOf(typeName).ShouldBe(SqlDbType.NVarChar);
}
