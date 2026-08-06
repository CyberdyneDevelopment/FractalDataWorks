using Fdw.Schema.Ddl;
using Fdw.Schema.Ddl.Commands;
using Fdw.Schema.Ddl.Definitions;
using Fdw.Schema.Ddl.MsSql;
using Fdw.Schema.Indexes;
using Fdw.Schema.Keys;
using Fdw.Schema.Properties;
using Fdw.Schema.Schemas;

namespace Fdw.Schema.Ddl.MsSql.Tests;

/// <summary>
/// Tests for <see cref="MsSqlDdlGenerator.GenerateCommands{TProperty}"/> which converts
/// ISchemaDefinition into DDL commands.
/// </summary>
public sealed class MsSqlDdlGeneratorGenerateCommandsTests
{
    private readonly MsSqlDdlGenerator _sut = new();

    private static Mock<IPropertyDefinition> CreateProperty(
        string name,
        bool isRequired = true,
        string? roleName = null,
        Dictionary<string, object>? metadata = null)
    {
        var prop = new Mock<IPropertyDefinition>();
        prop.Setup(p => p.Name).Returns(name);
        prop.Setup(p => p.IsRequired).Returns(isRequired);
        prop.Setup(p => p.Metadata).Returns(metadata);

        if (roleName != null)
        {
            var role = new Mock<IPropertyRole>();
            role.Setup(r => r.Name).Returns(roleName);
            prop.Setup(p => p.Role).Returns(role.Object);
        }

        return prop;
    }

    private static Mock<ISchemaDefinition<IPropertyDefinition>> CreateSchema(
        string name,
        IReadOnlyList<IPropertyDefinition> properties,
        IKeyDefinition<IPropertyDefinition>? surrogateKey = null,
        IKeyDefinition<IPropertyDefinition>? naturalKey = null,
        IReadOnlyList<IIndexDefinition<IPropertyDefinition>>? indexes = null)
    {
        var schema = new Mock<ISchemaDefinition<IPropertyDefinition>>();
        schema.Setup(s => s.Name).Returns(name);
        schema.Setup(s => s.Properties).Returns(properties);
        schema.Setup(s => s.SurrogateKey).Returns(surrogateKey);
        schema.Setup(s => s.NaturalKey).Returns(naturalKey);
        schema.Setup(s => s.Indexes).Returns(indexes ?? []);
        return schema;
    }

    // --- Basic GenerateCommands ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateCommandsReturnsSuccessWithCreateTableCommand()
    {
        var prop = CreateProperty("Id", isRequired: true, metadata: new Dictionary<string, object>
        {
            ["ClrType"] = "System.Int32"
        });
        var schema = CreateSchema("TestTable", [prop.Object]);

        var result = _sut.GenerateCommands(schema.Object);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBeGreaterThanOrEqualTo(1);
        result.Value[0].ShouldBeOfType<CreateTableCommand>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateCommandsUsesDefaultOptionsWhenNull()
    {
        var prop = CreateProperty("Id", metadata: new Dictionary<string, object>
        {
            ["ClrType"] = "System.Int32"
        });
        var schema = CreateSchema("TestTable", [prop.Object]);

        var result = _sut.GenerateCommands(schema.Object, null);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        var createTable = result.Value[0].ShouldBeOfType<CreateTableCommand>();
        createTable.SchemaName.ShouldBe("dbo");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateCommandsUsesSchemaNameFromOptions()
    {
        var prop = CreateProperty("Id", metadata: new Dictionary<string, object>
        {
            ["ClrType"] = "System.Int32"
        });
        var schema = CreateSchema("TestTable", [prop.Object]);
        var options = new DdlGenerationOptions { SchemaName = "cfg" };

        var result = _sut.GenerateCommands(schema.Object, options);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        var createTable = result.Value[0].ShouldBeOfType<CreateTableCommand>();
        createTable.SchemaName.ShouldBe("cfg");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateCommandsSetsTableNameFromSchemaName()
    {
        var prop = CreateProperty("Col1", metadata: new Dictionary<string, object>
        {
            ["ClrType"] = "System.String"
        });
        var schema = CreateSchema("Connection", [prop.Object]);

        var result = _sut.GenerateCommands(schema.Object);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        var createTable = result.Value[0].ShouldBeOfType<CreateTableCommand>();
        createTable.TableName.ShouldBe("Connection");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateCommandsSetsPrimaryKeyName()
    {
        var prop = CreateProperty("Id", roleName: "SurrogateKey", metadata: new Dictionary<string, object>
        {
            ["ClrType"] = "System.Int32"
        });

        var surrogateKey = new Mock<IKeyDefinition<IPropertyDefinition>>();
        surrogateKey.Setup(k => k.Members).Returns([new KeyMember(0, "Id")]);

        var schema = CreateSchema("TestTable", [prop.Object], surrogateKey: surrogateKey.Object);

        var result = _sut.GenerateCommands(schema.Object);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        var createTable = result.Value[0].ShouldBeOfType<CreateTableCommand>();
        createTable.PrimaryKeyName.ShouldBe("PK_TestTable");
        createTable.PrimaryKeyColumns.ShouldNotBeNull();
        createTable.PrimaryKeyColumns.ShouldContain("Id");
    }

    // --- SurrogateKey vs NaturalKey ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateCommandsUsesSurrogateKeyForPrimaryKey()
    {
        var prop = CreateProperty("RowId", roleName: "SurrogateKey", metadata: new Dictionary<string, object>
        {
            ["ClrType"] = "System.Guid"
        });

        var surrogateKey = new Mock<IKeyDefinition<IPropertyDefinition>>();
        surrogateKey.Setup(k => k.Members).Returns([new KeyMember(0, "RowId")]);

        var schema = CreateSchema("TestTable", [prop.Object], surrogateKey: surrogateKey.Object);

        var result = _sut.GenerateCommands(schema.Object);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        var createTable = result.Value[0].ShouldBeOfType<CreateTableCommand>();
        createTable.PrimaryKeyColumns.ShouldNotBeNull();
        createTable.PrimaryKeyColumns.ShouldContain("RowId");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateCommandsUsesNaturalKeyWhenNoSurrogateKey()
    {
        var prop1 = CreateProperty("FirstName", roleName: "NaturalKey", metadata: new Dictionary<string, object>
        {
            ["ClrType"] = "System.String"
        });
        var prop2 = CreateProperty("LastName", roleName: "NaturalKey", metadata: new Dictionary<string, object>
        {
            ["ClrType"] = "System.String"
        });

        var naturalKey = new Mock<IKeyDefinition<IPropertyDefinition>>();
        naturalKey.Setup(k => k.Members).Returns(
        [
            new KeyMember(0, "FirstName"),
            new KeyMember(1, "LastName")
        ]);

        var schema = CreateSchema("TestTable", [prop1.Object, prop2.Object], naturalKey: naturalKey.Object);

        var result = _sut.GenerateCommands(schema.Object);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        var createTable = result.Value[0].ShouldBeOfType<CreateTableCommand>();
        createTable.PrimaryKeyColumns.ShouldNotBeNull();
        createTable.PrimaryKeyColumns.ShouldContain("FirstName");
        createTable.PrimaryKeyColumns.ShouldContain("LastName");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateCommandsSurrogateKeyTakesPrecedenceOverNaturalKey()
    {
        var prop = CreateProperty("Id", roleName: "SurrogateKey", metadata: new Dictionary<string, object>
        {
            ["ClrType"] = "System.Int32"
        });

        var surrogateKey = new Mock<IKeyDefinition<IPropertyDefinition>>();
        surrogateKey.Setup(k => k.Members).Returns([new KeyMember(0, "Id")]);

        var naturalKey = new Mock<IKeyDefinition<IPropertyDefinition>>();
        naturalKey.Setup(k => k.Members).Returns([new KeyMember(0, "Name")]);

        var schema = CreateSchema("TestTable", [prop.Object],
            surrogateKey: surrogateKey.Object,
            naturalKey: naturalKey.Object);

        var result = _sut.GenerateCommands(schema.Object);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        var createTable = result.Value[0].ShouldBeOfType<CreateTableCommand>();
        createTable.PrimaryKeyColumns.ShouldNotBeNull();
        createTable.PrimaryKeyColumns.ShouldContain("Id");
        createTable.PrimaryKeyColumns.ShouldNotContain("Name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateCommandsWithNoKeysHasNoPrimaryKey()
    {
        var prop = CreateProperty("Col1", metadata: new Dictionary<string, object>
        {
            ["ClrType"] = "System.String"
        });

        var schema = CreateSchema("HeapTable", [prop.Object]);

        var result = _sut.GenerateCommands(schema.Object);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        var createTable = result.Value[0].ShouldBeOfType<CreateTableCommand>();
        createTable.PrimaryKeyColumns.ShouldBeNull();
    }

    // --- Column Mapping ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateCommandsMapsPropertyNameToColumnName()
    {
        var prop = CreateProperty("ConnectionName", metadata: new Dictionary<string, object>
        {
            ["ClrType"] = "System.String",
            ["MaxLength"] = 200
        });

        var schema = CreateSchema("Connection", [prop.Object]);

        var result = _sut.GenerateCommands(schema.Object);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        var createTable = result.Value[0].ShouldBeOfType<CreateTableCommand>();
        createTable.Columns.ShouldContain(c => string.Equals(c.Name, "ConnectionName", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateCommandsMapsNullableFromIsRequired()
    {
        var requiredProp = CreateProperty("Id", isRequired: true, metadata: new Dictionary<string, object>
        {
            ["ClrType"] = "System.Int32"
        });
        var optionalProp = CreateProperty("Description", isRequired: false, metadata: new Dictionary<string, object>
        {
            ["ClrType"] = "System.String"
        });

        var schema = CreateSchema("TestTable", [requiredProp.Object, optionalProp.Object]);

        var result = _sut.GenerateCommands(schema.Object);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        var createTable = result.Value[0].ShouldBeOfType<CreateTableCommand>();

        var idCol = createTable.Columns.First(c => string.Equals(c.Name, "Id", StringComparison.Ordinal));
        idCol.IsNullable.ShouldBeFalse();

        var descCol = createTable.Columns.First(c => string.Equals(c.Name, "Description", StringComparison.Ordinal));
        descCol.IsNullable.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateCommandsDetectsSurrogateKeyRoleAsPrimaryKey()
    {
        var prop = CreateProperty("RowId", roleName: "SurrogateKey", metadata: new Dictionary<string, object>
        {
            ["ClrType"] = "System.Guid"
        });

        var schema = CreateSchema("TestTable", [prop.Object]);

        var result = _sut.GenerateCommands(schema.Object);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        var createTable = result.Value[0].ShouldBeOfType<CreateTableCommand>();
        var rowIdCol = createTable.Columns.First(c => string.Equals(c.Name, "RowId", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateCommandsDetectsNaturalKeyRoleAsPrimaryKey()
    {
        var prop = CreateProperty("Code", roleName: "NaturalKey", metadata: new Dictionary<string, object>
        {
            ["ClrType"] = "System.String",
            ["MaxLength"] = 10
        });

        var schema = CreateSchema("TestTable", [prop.Object]);

        var result = _sut.GenerateCommands(schema.Object);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        var createTable = result.Value[0].ShouldBeOfType<CreateTableCommand>();
        var codeCol = createTable.Columns.First(c => string.Equals(c.Name, "Code", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateCommandsNonKeyRoleIsNotPrimaryKey()
    {
        var prop = CreateProperty("Name", roleName: "Attribute", metadata: new Dictionary<string, object>
        {
            ["ClrType"] = "System.String"
        });

        var schema = CreateSchema("TestTable", [prop.Object]);

        var result = _sut.GenerateCommands(schema.Object);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        var createTable = result.Value[0].ShouldBeOfType<CreateTableCommand>();
        var nameCol = createTable.Columns.First(c => string.Equals(c.Name, "Name", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateCommandsNullRoleIsNotPrimaryKey()
    {
        var prop = CreateProperty("Name", metadata: new Dictionary<string, object>
        {
            ["ClrType"] = "System.String"
        });
        // Role is null by default in our mock

        var schema = CreateSchema("TestTable", [prop.Object]);

        var result = _sut.GenerateCommands(schema.Object);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        var createTable = result.Value[0].ShouldBeOfType<CreateTableCommand>();
        var nameCol = createTable.Columns.First(c => string.Equals(c.Name, "Name", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateCommandsMapsDefaultValueFromMetadata()
    {
        var prop = CreateProperty("RowId", metadata: new Dictionary<string, object>
        {
            ["ClrType"] = "System.Guid",
            ["DefaultValue"] = "NEWSEQUENTIALID()"
        });

        var schema = CreateSchema("TestTable", [prop.Object]);

        var result = _sut.GenerateCommands(schema.Object);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        var createTable = result.Value[0].ShouldBeOfType<CreateTableCommand>();
        var rowIdCol = createTable.Columns.First(c => string.Equals(c.Name, "RowId", StringComparison.Ordinal));
        rowIdCol.DefaultValue.ShouldBe("NEWSEQUENTIALID()");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateCommandsDefaultValueIsNullWhenNoMetadata()
    {
        var prop = CreateProperty("Col1", metadata: new Dictionary<string, object>
        {
            ["ClrType"] = "System.Int32"
        });

        var schema = CreateSchema("TestTable", [prop.Object]);

        var result = _sut.GenerateCommands(schema.Object);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        var createTable = result.Value[0].ShouldBeOfType<CreateTableCommand>();
        var col = createTable.Columns.First(c => string.Equals(c.Name, "Col1", StringComparison.Ordinal));
        col.DefaultValue.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateCommandsDefaultValueIsNullWhenMetadataIsNull()
    {
        var prop = CreateProperty("Col1");
        // Metadata is null by default

        var schema = CreateSchema("TestTable", [prop.Object]);

        var result = _sut.GenerateCommands(schema.Object);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        var createTable = result.Value[0].ShouldBeOfType<CreateTableCommand>();
        var col = createTable.Columns.First(c => string.Equals(c.Name, "Col1", StringComparison.Ordinal));
        col.DefaultValue.ShouldBeNull();
    }

    // --- Index Generation ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateCommandsIncludesIndexCommandsWhenOptionEnabled()
    {
        var prop = CreateProperty("Name", metadata: new Dictionary<string, object>
        {
            ["ClrType"] = "System.String",
            ["MaxLength"] = 200
        });

        var indexDef = new Mock<IIndexDefinition<IPropertyDefinition>>();
        indexDef.Setup(i => i.Name).Returns("IX_TestTable_Name");
        indexDef.Setup(i => i.Members).Returns([new IndexMember(0, "Name")]);
        indexDef.Setup(i => i.IsUnique).Returns(false);
        indexDef.Setup(i => i.IsClustered).Returns(false);
        indexDef.Setup(i => i.IncludeColumns).Returns((IReadOnlyList<string>?)null);
        indexDef.Setup(i => i.FilterPredicate).Returns((string?)null);

        var schema = CreateSchema("TestTable", [prop.Object], indexes: [indexDef.Object]);
        var options = new DdlGenerationOptions { IncludeIndexes = true };

        var result = _sut.GenerateCommands(schema.Object, options);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(2); // CreateTable + CreateIndex
        result.Value[1].ShouldBeOfType<CreateIndexCommand>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateCommandsExcludesIndexesWhenOptionDisabled()
    {
        var prop = CreateProperty("Name", metadata: new Dictionary<string, object>
        {
            ["ClrType"] = "System.String",
            ["MaxLength"] = 200
        });

        var indexDef = new Mock<IIndexDefinition<IPropertyDefinition>>();
        indexDef.Setup(i => i.Name).Returns("IX_TestTable_Name");
        indexDef.Setup(i => i.Members).Returns([new IndexMember(0, "Name")]);
        indexDef.Setup(i => i.IsUnique).Returns(false);
        indexDef.Setup(i => i.IsClustered).Returns(false);

        var schema = CreateSchema("TestTable", [prop.Object], indexes: [indexDef.Object]);
        var options = new DdlGenerationOptions { IncludeIndexes = false };

        var result = _sut.GenerateCommands(schema.Object, options);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(1); // Only CreateTable
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateCommandsSkipsIndexesWhenSchemaHasNone()
    {
        var prop = CreateProperty("Col1", metadata: new Dictionary<string, object>
        {
            ["ClrType"] = "System.Int32"
        });

        var schema = CreateSchema("TestTable", [prop.Object], indexes: []);
        var options = new DdlGenerationOptions { IncludeIndexes = true };

        var result = _sut.GenerateCommands(schema.Object, options);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(1); // Only CreateTable
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateCommandsIndexPreservesUniqueAndClusteredFlags()
    {
        var prop = CreateProperty("Id", metadata: new Dictionary<string, object>
        {
            ["ClrType"] = "System.Int32"
        });

        var indexDef = new Mock<IIndexDefinition<IPropertyDefinition>>();
        indexDef.Setup(i => i.Name).Returns("UX_TestTable_Id");
        indexDef.Setup(i => i.Members).Returns([new IndexMember(0, "Id")]);
        indexDef.Setup(i => i.IsUnique).Returns(true);
        indexDef.Setup(i => i.IsClustered).Returns(true);
        indexDef.Setup(i => i.IncludeColumns).Returns(new List<string> { "Name" });
        indexDef.Setup(i => i.FilterPredicate).Returns("IsCurrent = 1");

        var schema = CreateSchema("TestTable", [prop.Object], indexes: [indexDef.Object]);

        var result = _sut.GenerateCommands(schema.Object);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        var indexCmd = result.Value[1].ShouldBeOfType<CreateIndexCommand>();
        indexCmd.Definition.IsUnique.ShouldBeTrue();
        indexCmd.Definition.IsClustered.ShouldBeTrue();
        indexCmd.Definition.IncludeColumns.ShouldNotBeNull();
        indexCmd.Definition.IncludeColumns.ShouldContain("Name");
        indexCmd.Definition.FilterPredicate.ShouldBe("IsCurrent = 1");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateCommandsIndexUsesSchemaNameFromOptions()
    {
        var prop = CreateProperty("Col1", metadata: new Dictionary<string, object>
        {
            ["ClrType"] = "System.Int32"
        });

        var indexDef = new Mock<IIndexDefinition<IPropertyDefinition>>();
        indexDef.Setup(i => i.Name).Returns("IX_TestTable_Col1");
        indexDef.Setup(i => i.Members).Returns([new IndexMember(0, "Col1")]);
        indexDef.Setup(i => i.IsUnique).Returns(false);
        indexDef.Setup(i => i.IsClustered).Returns(false);

        var schema = CreateSchema("TestTable", [prop.Object], indexes: [indexDef.Object]);
        var options = new DdlGenerationOptions { SchemaName = "etl" };

        var result = _sut.GenerateCommands(schema.Object, options);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        var indexCmd = result.Value[1].ShouldBeOfType<CreateIndexCommand>();
        indexCmd.SchemaName.ShouldBe("etl");
        indexCmd.TableName.ShouldBe("TestTable");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateCommandsMultipleIndexesGenerateMultipleCommands()
    {
        var prop = CreateProperty("Col1", metadata: new Dictionary<string, object>
        {
            ["ClrType"] = "System.Int32"
        });

        var index1 = new Mock<IIndexDefinition<IPropertyDefinition>>();
        index1.Setup(i => i.Name).Returns("IX_1");
        index1.Setup(i => i.Members).Returns([new IndexMember(0, "Col1")]);
        index1.Setup(i => i.IsUnique).Returns(false);
        index1.Setup(i => i.IsClustered).Returns(false);

        var index2 = new Mock<IIndexDefinition<IPropertyDefinition>>();
        index2.Setup(i => i.Name).Returns("IX_2");
        index2.Setup(i => i.Members).Returns([new IndexMember(0, "Col1")]);
        index2.Setup(i => i.IsUnique).Returns(true);
        index2.Setup(i => i.IsClustered).Returns(false);

        var schema = CreateSchema("TestTable", [prop.Object], indexes: [index1.Object, index2.Object]);

        var result = _sut.GenerateCommands(schema.Object);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(3); // CreateTable + 2 CreateIndex
    }

    // --- Multiple Properties ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateCommandsMultiplePropertiesCreateMultipleColumns()
    {
        var props = new[]
        {
            CreateProperty("Id", isRequired: true, roleName: "SurrogateKey", metadata: new Dictionary<string, object>
            {
                ["ClrType"] = "System.Guid",
                ["DefaultValue"] = "NEWSEQUENTIALID()"
            }).Object,
            CreateProperty("Name", isRequired: true, metadata: new Dictionary<string, object>
            {
                ["ClrType"] = "System.String",
                ["MaxLength"] = 200
            }).Object,
            CreateProperty("IsActive", isRequired: true, metadata: new Dictionary<string, object>
            {
                ["ClrType"] = "System.Boolean",
                ["DefaultValue"] = "1"
            }).Object,
            CreateProperty("Description", isRequired: false, metadata: new Dictionary<string, object>
            {
                ["ClrType"] = "System.String"
            }).Object
        };

        var surrogateKey = new Mock<IKeyDefinition<IPropertyDefinition>>();
        surrogateKey.Setup(k => k.Members).Returns([new KeyMember(0, "Id")]);

        var schema = CreateSchema("Connection", props, surrogateKey: surrogateKey.Object);

        var result = _sut.GenerateCommands(schema.Object);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        var createTable = result.Value[0].ShouldBeOfType<CreateTableCommand>();
        createTable.Columns.Count.ShouldBe(4);
        createTable.PrimaryKeyColumns.ShouldNotBeNull();
        createTable.PrimaryKeyColumns.ShouldContain("Id");
    }

    // --- GenerateSql Error Path ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateSqlUnsupportedCommandReturnsFailure()
    {
        var cmd = new InsertDataCommand
        {
            TableName = "Test",
            Columns = ["Id"],
            Values = [new object?[] { 1 }]
        };

        var result = _sut.GenerateSql(cmd);

        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
    }

    // --- End-to-End: GenerateCommands then GenerateSql ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateCommandsThenGenerateSqlProducesValidSql()
    {
        var props = new[]
        {
            CreateProperty("Id", isRequired: true, roleName: "SurrogateKey", metadata: new Dictionary<string, object>
            {
                ["ClrType"] = "System.Int32"
            }).Object,
            CreateProperty("Name", isRequired: true, metadata: new Dictionary<string, object>
            {
                ["ClrType"] = "System.String",
                ["MaxLength"] = 100
            }).Object
        };

        var surrogateKey = new Mock<IKeyDefinition<IPropertyDefinition>>();
        surrogateKey.Setup(k => k.Members).Returns([new KeyMember(0, "Id")]);

        var schema = CreateSchema("TestTable", props, surrogateKey: surrogateKey.Object);
        var options = new DdlGenerationOptions { SchemaName = "cfg" };

        var commandsResult = _sut.GenerateCommands(schema.Object, options);
        commandsResult.IsSuccess.ShouldBeTrue();
        commandsResult.Value.ShouldNotBeNull();

        var scriptResult = _sut.GenerateScript(commandsResult.Value);
        scriptResult.IsSuccess.ShouldBeTrue();
        scriptResult.Value.ShouldNotBeNull();
        scriptResult.Value.ShouldContain("CREATE TABLE cfg.TestTable");
        scriptResult.Value.ShouldContain("PRIMARY KEY (Id)");
        scriptResult.Value.ShouldContain("GO");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateCommandsThenGenerateSqlWithIndexProducesValidSql()
    {
        var props = new[]
        {
            CreateProperty("Id", isRequired: true, metadata: new Dictionary<string, object>
            {
                ["ClrType"] = "System.Int32"
            }).Object,
            CreateProperty("Name", isRequired: true, metadata: new Dictionary<string, object>
            {
                ["ClrType"] = "System.String",
                ["MaxLength"] = 200
            }).Object
        };

        var indexDef = new Mock<IIndexDefinition<IPropertyDefinition>>();
        indexDef.Setup(i => i.Name).Returns("IX_TestTable_Name");
        indexDef.Setup(i => i.Members).Returns([new IndexMember(0, "Name")]);
        indexDef.Setup(i => i.IsUnique).Returns(false);
        indexDef.Setup(i => i.IsClustered).Returns(false);
        indexDef.Setup(i => i.IncludeColumns).Returns((IReadOnlyList<string>?)null);
        indexDef.Setup(i => i.FilterPredicate).Returns((string?)null);

        var schema = CreateSchema("TestTable", props, indexes: [indexDef.Object]);
        var options = new DdlGenerationOptions { SchemaName = "dbo", IncludeIndexes = true };

        var commandsResult = _sut.GenerateCommands(schema.Object, options);
        commandsResult.IsSuccess.ShouldBeTrue();
        commandsResult.Value.ShouldNotBeNull();

        var scriptResult = _sut.GenerateScript(commandsResult.Value);
        scriptResult.IsSuccess.ShouldBeTrue();
        scriptResult.Value.ShouldNotBeNull();
        scriptResult.Value.ShouldContain("CREATE TABLE dbo.TestTable");
        scriptResult.Value.ShouldContain("CREATE NONCLUSTERED INDEX IX_TestTable_Name ON dbo.TestTable (Name)");
    }
}
