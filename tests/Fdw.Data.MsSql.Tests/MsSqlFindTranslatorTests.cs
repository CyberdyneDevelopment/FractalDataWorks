using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;
using Fdw.Schema.Properties;
using Moq;

namespace Fdw.Data.MsSql.Tests;

public sealed class MsSqlFindTranslatorTests
{
    private readonly MsSqlFindTranslator _translator = new();

    private static IStorageContainer CreateMockContainer(
        string schema = "dbo",
        string tableName = "Customers",
        IReadOnlyList<IField>? fields = null)
    {
        var dbPath = new DatabasePath(null, schema, tableName);

        var mockSchema = new Mock<IContainerSchema>();
        mockSchema.Setup(s => s.Fields).Returns(fields ?? Array.Empty<IField>());

        var mockContainer = new Mock<IStorageContainer>();
        mockContainer.Setup(c => c.Name).Returns(tableName);
        mockContainer.Setup(c => c.Path).Returns(dbPath);
        mockContainer.Setup(c => c.Schema).Returns(mockSchema.Object);

        return mockContainer.Object;
    }

    private static IField CreateMockField(string name, Type clrType)
    {
        var mockFieldType = new Mock<IFieldType>();
        mockFieldType.Setup(ft => ft.ClrType).Returns(clrType);

        var mockField = new Mock<IField>();
        mockField.Setup(f => f.Name).Returns(name);
        mockField.Setup(f => f.FieldType).Returns(mockFieldType.Object);

        return mockField.Object;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateGeneratesLikeWithWildcards()
    {
        // Arrange
        var container = CreateMockContainer(fields: new[]
        {
            CreateMockField("Name", typeof(string)),
            CreateMockField("Id", typeof(int))
        });
        var command = new FindCommand<object>
        {
            SearchTerm = "acme"
        };

        // Act
        var result = await _translator.Translate(command, container, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var sql = result.Value!.CommandText;
        sql.ShouldContain("LIKE @searchTerm");
        result.Value.Parameters["@searchTerm"]!.Value.ShouldBe("%acme%");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateEscapesPercentAndUnderscoreInSearchTerm()
    {
        // Arrange
        var container = CreateMockContainer(fields: new[]
        {
            CreateMockField("Name", typeof(string))
        });
        var command = new FindCommand<object>
        {
            SearchTerm = "50%_off"
        };

        // Act
        var result = await _translator.Translate(command, container, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var paramValue = (string)result.Value!.Parameters["@searchTerm"]!.Value!;
        paramValue.ShouldContain("[%]");
        paramValue.ShouldContain("[_]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateIncludesOnlySpecifiedFieldsWhenInFieldsSet()
    {
        // Arrange
        var container = CreateMockContainer(fields: new[]
        {
            CreateMockField("Name", typeof(string)),
            CreateMockField("Email", typeof(string)),
            CreateMockField("Phone", typeof(string))
        });
        var command = new FindCommand<object>
        {
            SearchTerm = "test",
            FieldNames = new[] { "Name", "Email" }
        };

        // Act
        var result = await _translator.Translate(command, container, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var sql = result.Value!.CommandText;
        // WHERE clause should only search in specified fields
        var whereClause = sql.Substring(sql.IndexOf("WHERE", StringComparison.Ordinal));
        whereClause.ShouldContain("[Name]");
        whereClause.ShouldContain("[Email]");
        whereClause.ShouldNotContain("[Phone]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateGeneratesCollateClauseForCaseSensitiveSearch()
    {
        // Arrange
        var container = CreateMockContainer(fields: new[]
        {
            CreateMockField("Name", typeof(string))
        });
        var command = new FindCommand<object>
        {
            SearchTerm = "Acme",
            CaseSensitive = true
        };

        // Act
        var result = await _translator.Translate(command, container, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("COLLATE Latin1_General_CS_AS");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateOmitsCollateForCaseInsensitiveSearch()
    {
        // Arrange
        var container = CreateMockContainer(fields: new[]
        {
            CreateMockField("Name", typeof(string))
        });
        var command = new FindCommand<object>
        {
            SearchTerm = "acme",
            CaseSensitive = false
        };

        // Act
        var result = await _translator.Translate(command, container, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldNotContain("COLLATE");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateAddsTopClauseForMaxResults()
    {
        // Arrange
        var container = CreateMockContainer(fields: new[]
        {
            CreateMockField("Name", typeof(string))
        });
        var command = new FindCommand<object>
        {
            SearchTerm = "acme",
            MaxResults = 25
        };

        // Act
        var result = await _translator.Translate(command, container, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("SELECT TOP (25)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateOmitsTopClauseWhenMaxResultsNotSet()
    {
        // Arrange
        var container = CreateMockContainer(fields: new[]
        {
            CreateMockField("Name", typeof(string))
        });
        var command = new FindCommand<object>
        {
            SearchTerm = "acme"
        };

        // Act
        var result = await _translator.Translate(command, container, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldNotContain("TOP");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateGeneratesOrJoinedWhereForMultipleFields()
    {
        // Arrange
        var container = CreateMockContainer(fields: new[]
        {
            CreateMockField("Name", typeof(string)),
            CreateMockField("Description", typeof(string)),
            CreateMockField("Email", typeof(string))
        });
        var command = new FindCommand<object>
        {
            SearchTerm = "test"
        };

        // Act
        var result = await _translator.Translate(command, container, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var sql = result.Value!.CommandText;
        sql.ShouldContain("[Name]");
        sql.ShouldContain("[Description]");
        sql.ShouldContain("[Email]");
        sql.ShouldContain(" OR ");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateDefaultsToStringFieldsWhenNoFieldNamesSpecified()
    {
        // Arrange
        var container = CreateMockContainer(fields: new[]
        {
            CreateMockField("Name", typeof(string)),
            CreateMockField("Age", typeof(int)),
            CreateMockField("Email", typeof(string))
        });
        var command = new FindCommand<object>
        {
            SearchTerm = "test"
        };

        // Act
        var result = await _translator.Translate(command, container, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var sql = result.Value!.CommandText;
        sql.ShouldContain("[Name]");
        sql.ShouldContain("[Email]");
        // Age is int, should not be in LIKE search
        var whereClause = sql.Substring(sql.IndexOf("WHERE", StringComparison.Ordinal));
        whereClause.ShouldNotContain("[Age]");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateReturnsNoRowsWhenNoSearchableColumns()
    {
        // Arrange - container with only non-string fields, no explicit FieldNames
        var container = CreateMockContainer(fields: new[]
        {
            CreateMockField("Id", typeof(int)),
            CreateMockField("Amount", typeof(decimal))
        });
        var command = new FindCommand<object>
        {
            SearchTerm = "test"
        };

        // Act
        var result = await _translator.Translate(command, container, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("WHERE 1 = 0");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateReturnsFailureForEmptySearchTerm()
    {
        // Arrange
        var container = CreateMockContainer(fields: new[]
        {
            CreateMockField("Name", typeof(string))
        });
        var command = new FindCommand<object>
        {
            SearchTerm = ""
        };

        // Act
        var result = await _translator.Translate(command, container, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateUsesQuotedIdentifierFromDatabasePath()
    {
        // Arrange
        var container = CreateMockContainer(schema: "sales", tableName: "Orders", fields: new[]
        {
            CreateMockField("Name", typeof(string))
        });
        var command = new FindCommand<object>
        {
            SearchTerm = "test"
        };

        // Act
        var result = await _translator.Translate(command, container, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("[sales].[Orders]");
    }
}
