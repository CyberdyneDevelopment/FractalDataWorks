using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.PostgreSql.Discovery;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.PostgreSql.Tests.Discovery;

/// <summary>
/// A PostgreSQL discovery gives what it found the connection type's OWN container type options, PostgreSqlTable and PostgreSqlView
/// (FDW-6309, FDW-6387). It used to look up Table and View, which only the SQL Server assembly declares, so in a PostgreSQL-only process
/// every discovered container had the NotFound placeholder as its type.
/// </summary>
[Collection(nameof(PostgreSqlTestCollection))]
public sealed class PostgreSqlContainerTypeTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheWordTableIsThePostgreSqlTableOption()
    {
        var resolved = PostgreSqlSchemaConversion.GetContainerType(NullLogger.Instance, "Table");

        resolved.IsSuccess.ShouldBeTrue(resolved.CurrentMessage);
        resolved.Value.ShouldBeOfType<PostgreSqlTableContainerType>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheWordViewIsThePostgreSqlViewOption()
    {
        var resolved = PostgreSqlSchemaConversion.GetContainerType(NullLogger.Instance, "View");

        resolved.IsSuccess.ShouldBeTrue(resolved.CurrentMessage);
        resolved.Value.ShouldBeOfType<PostgreSqlViewContainerType>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ATableStatesAllFourOperationsAndAViewOnlyQuery()
    {
        ContainerTypes.ByName("PostgreSqlTable").SupportedOperations.ShouldBe(new[] { "Query", "Insert", "Update", "Delete" });
        ContainerTypes.ByName("PostgreSqlView").SupportedOperations.ShouldBe(new[] { "Query" });
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("StoredProcedure")]
    [InlineData("MaterializedView")]
    public void AWordThePostgreSqlDiscovererDoesNotWriteIsRefusedNotLookedUpByName(string word)
    {
        var resolved = PostgreSqlSchemaConversion.GetContainerType(NullLogger.Instance, word);

        resolved.IsSuccess.ShouldBeFalse();
        resolved.CurrentMessage.ShouldNotBeNull().ShouldContain(word);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ATableContainerIsBuiltWithTheRegisteredPostgreSqlTableOption()
    {
        var created = PostgreSqlTableContainer.Create(
            "orders",
            description: null,
            new Mock<IDataNodePath>().Object,
            [],
            [],
            GenericResult<IReadOnlyList<ReferencingKeyBinding>>.Success([]),
            new PostgreSqlDatabasePath(null, "public", "orders"),
            FormatTypes.ByName("Json"),
            new Dictionary<string, object>());

        created.IsSuccess.ShouldBeTrue(created.CurrentMessage);
        created.Value!.ContainerType.ShouldBeOfType<PostgreSqlTableContainerType>();
    }
}
