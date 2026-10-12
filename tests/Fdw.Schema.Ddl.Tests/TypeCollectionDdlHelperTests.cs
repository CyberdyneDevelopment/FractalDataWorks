using System.Collections.Generic;
using System.Linq;
using Fdw.Schema.Ddl.Commands;
using Fdw.Schema.Ddl.Helpers;
using Fdw.Types;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Schema.Ddl.Tests;

public sealed class TypeCollectionDdlHelperTests
{
    private static TypeCollectionMetadata CreateTestMetadata(
        IReadOnlyList<TypeOptionMetadata>? options = null)
    {
        var collectionKind = new Mock<ICollectionKind>();
        collectionKind.Setup(k => k.Name).Returns("TypeCollection");

        return new TypeCollectionMetadata
        {
            Id = 1,
            Name = "FilterOperators",
            FullName = "Fdw.Data.FilterOperators",
            CollectionKind = collectionKind.Object,
            ServiceCategory = "Data",
            AssemblyQualifiedName = "Fdw.Data.FilterOperators, Fdw.Data",
            Options = options ?? []
        };
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateDdlCommandsCreatesTypeCollectionInsert()
    {
        var metadata = CreateTestMetadata();

        var commands = TypeCollectionDdlHelper.GenerateDdlCommands(metadata);

        commands.Count.ShouldBeGreaterThan(0);
        var insertCommand = commands.OfType<InsertDataCommand>().First();
        insertCommand.SchemaName.ShouldBe("types");
        insertCommand.TableName.ShouldBe("TypeCollection");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateDdlCommandsIncludesTypeOptionInserts()
    {
        var options = new List<TypeOptionMetadata>
        {
            new TypeOptionMetadata
            {
                Id = 1,
                Name = "Equal",
                TypeCollectionId = 1,
                FullTypeName = "Fdw.Data.EqualOperator"
            },
            new TypeOptionMetadata
            {
                Id = 2,
                Name = "NotEqual",
                TypeCollectionId = 1,
                FullTypeName = "Fdw.Data.NotEqualOperator"
            }
        };

        var metadata = CreateTestMetadata(options);

        var commands = TypeCollectionDdlHelper.GenerateDdlCommands(metadata);

        var optionInsert = commands.OfType<InsertDataCommand>()
            .FirstOrDefault(c => string.Equals(c.TableName, "TypeOption", System.StringComparison.Ordinal));
        optionInsert.ShouldNotBeNull();
        optionInsert!.Values.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateDdlCommandsIncludesPropertyInserts()
    {
        var properties = new List<TypePropertyMetadata>
        {
            new TypePropertyMetadata
            {
                Name = "Operator",
                PropertyType = "System.String",
                PropertyRole = "Attribute",
                SqlType = "VARCHAR",
                MaxLength = 100,
                IsNullable = false,
                IsCollection = false
            }
        };

        var options = new List<TypeOptionMetadata>
        {
            new TypeOptionMetadata
            {
                Id = 1,
                Name = "Equal",
                TypeCollectionId = 1,
                FullTypeName = "Fdw.Data.EqualOperator",
                Properties = properties
            }
        };

        var metadata = CreateTestMetadata(options);

        var commands = TypeCollectionDdlHelper.GenerateDdlCommands(metadata);

        var propertyInsert = commands.OfType<InsertDataCommand>()
            .FirstOrDefault(c => string.Equals(c.TableName, "TypeProperty", System.StringComparison.Ordinal));
        propertyInsert.ShouldNotBeNull();
        propertyInsert!.Values.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateDdlCommandsSkipsPropertyInsertWhenNoProperties()
    {
        var options = new List<TypeOptionMetadata>
        {
            new TypeOptionMetadata
            {
                Id = 1,
                Name = "Equal",
                TypeCollectionId = 1,
                FullTypeName = "Fdw.Data.EqualOperator",
                Properties = []
            }
        };

        var metadata = CreateTestMetadata(options);

        var commands = TypeCollectionDdlHelper.GenerateDdlCommands(metadata);

        var propertyInsert = commands.OfType<InsertDataCommand>()
            .FirstOrDefault(c => string.Equals(c.TableName, "TypeProperty", System.StringComparison.Ordinal));
        propertyInsert.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateDdlCommandsSkipsOptionsWhenEmpty()
    {
        var metadata = CreateTestMetadata(options: []);

        var commands = TypeCollectionDdlHelper.GenerateDdlCommands(metadata);

        // Only the TypeCollection insert
        commands.Count.ShouldBe(1);
        commands.OfType<InsertDataCommand>().First().TableName.ShouldBe("TypeCollection");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateDdlCommandsPropertyCodeReturnsCodeSnippet()
    {
        var code = TypeCollectionDdlHelper.GenerateDdlCommandsPropertyCode(
            "Fdw.Data.FilterOperators",
            "FilterOperators");

        code.ShouldContain("DdlCommands");
        code.ShouldContain("BuildDdlCommands");
        code.ShouldContain("GetMetadata");
        code.ShouldContain("TypeCollectionDdlHelper");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateDdlCommandsSetsIdentityInsertFalse()
    {
        var metadata = CreateTestMetadata();

        var commands = TypeCollectionDdlHelper.GenerateDdlCommands(metadata);

        foreach (var insert in commands.OfType<InsertDataCommand>())
        {
            insert.IdentityInsert.ShouldBeFalse();
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateDdlCommandsIncludesCollectionMetadataValues()
    {
        var metadata = CreateTestMetadata();

        var commands = TypeCollectionDdlHelper.GenerateDdlCommands(metadata);

        var collectionInsert = commands.OfType<InsertDataCommand>().First();
        var firstRow = collectionInsert.Values[0];

        firstRow[0].ShouldBe(1); // Id
        firstRow[1].ShouldBe("FilterOperators"); // Name
        firstRow[2].ShouldBe("Fdw.Data.FilterOperators"); // FullName
        firstRow[3].ShouldBe("TypeCollection"); // CollectionKind.Name
    }
}
