using System.Linq;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;
using Fdw.Schema;

namespace Fdw.Integration.Tests.TypeSystem;

/// <summary>
/// Integration tests for MsSql converter system end-to-end flow.
/// Tests: Schema Import → Field Metadata → Converter Lookup → Result Materialization
/// </summary>
public class MsSqlConverterIntegrationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BySourceTypeShouldFindAllStandardSqlTypes()
    {
        // Test lookup for common SQL types
        var intConverter = MsSqlConverters.BySourceType("int");
        var bigintConverter = MsSqlConverters.BySourceType("bigint");
        var nvarcharConverter = MsSqlConverters.BySourceType("nvarchar");
        var bitConverter = MsSqlConverters.BySourceType("bit");
        var datetimeConverter = MsSqlConverters.BySourceType("datetime");

        intConverter.ShouldNotBe(MsSqlConverters.NotFound);
        bigintConverter.ShouldNotBe(MsSqlConverters.NotFound);
        nvarcharConverter.ShouldNotBe(MsSqlConverters.NotFound);
        bitConverter.ShouldNotBe(MsSqlConverters.NotFound);
        datetimeConverter.ShouldNotBe(MsSqlConverters.NotFound);

        // Verify correct mappings
        intConverter.TargetClrType.ShouldBe(typeof(int));
        bigintConverter.TargetClrType.ShouldBe(typeof(long));
        nvarcharConverter.TargetClrType.ShouldBe(typeof(string));
        bitConverter.TargetClrType.ShouldBe(typeof(bool));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void FieldShouldStoreTypeSystemMetadata()
    {
        // Get field with new type system properties
        var intConverter = MsSqlConverters.BySourceType("int");
        var field = new Field
        {
            Name = "CustomerId",
            FieldType = new SimpleFieldType { TypeName = "Int32", ClrType = typeof(int) },
            Role = DatumTypes.ByName("DbKey"),
            IsNullable = false,
            IsIdentity = true,
            IsComputed = false,
            TypeSystemId = "MsSql",
            ConverterTypeId = intConverter.Id
        };

        field.TypeSystemId.ShouldBe("MsSql");
        field.ConverterTypeId.ShouldBe(intConverter.Id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConverterToClrShouldConvertDatabaseValues()
    {
        var intConverter = MsSqlConverters.BySourceType("int");
        var stringConverter = MsSqlConverters.BySourceType("nvarchar");
        var boolConverter = MsSqlConverters.BySourceType("bit");

        // Test int conversion
        var intResult = intConverter.ToClr(42);
        intResult.ShouldBe(42);

        // Test string conversion
        var stringResult = stringConverter.ToClr("test value");
        stringResult.ShouldBe("test value");

        // Test bool conversion
        var boolResult = boolConverter.ToClr(true);
        boolResult.ShouldBe(true);

        // Test null handling
        intConverter.ToClr(System.DBNull.Value).ShouldBeNull();
        stringConverter.ToClr(null).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SchemaFieldsShouldReferenceCorrectConverters()
    {
        // Simulate what schema importer creates
        var fields = new[]
        {
            new Field
            {
                Name = "Id",
                FieldType = new SimpleFieldType { TypeName = "Int32", ClrType = typeof(int) },
                Role = DatumTypes.ByName("DbKey"),
                TypeSystemId = "MsSql",
                ConverterTypeId = MsSqlConverters.BySourceType("int").Id
            },
            new Field
            {
                Name = "Name",
                FieldType = new SimpleFieldType { TypeName = "String", ClrType = typeof(string) },
                Role = DatumTypes.ByName("Attribute"),
                TypeSystemId = "MsSql",
                ConverterTypeId = MsSqlConverters.BySourceType("nvarchar").Id
            },
            new Field
            {
                Name = "IsActive",
                FieldType = new SimpleFieldType { TypeName = "Boolean", ClrType = typeof(bool) },
                Role = DatumTypes.ByName("Attribute"),
                TypeSystemId = "MsSql",
                ConverterTypeId = MsSqlConverters.BySourceType("bit").Id
            }
        };

        // Verify all fields have valid converters
        foreach (var field in fields)
        {
            var converter = MsSqlConverters.ById(field.ConverterTypeId!.Value);
            converter.ShouldNotBe(MsSqlConverters.NotFound);

            // Verify converter produces correct CLR type
            converter.TargetClrType.ShouldBe(field.FieldType.ClrType);
        }
    }
}
