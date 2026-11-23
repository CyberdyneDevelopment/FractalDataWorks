using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.MsSql;
using Fdw.Data.MsSql.Tests.TestSupport;
using Shouldly;
using Xunit;

namespace Fdw.Data.MsSql.Tests.Translators;

/// <summary>
/// FDW-6682: every command type SQL Server translates states the operation or operations it performs, read from what its translator emits, so the shared rule
/// (DataOperationRule) can judge any command against the operations its container type declares, and no command is left out of the check. The second test walks
/// every translator registered on the collection, so one added later without stating its operations fails here (the base also makes the property abstract).
/// </summary>
[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlTranslatorOperationsTests
{
    public static TheoryData<string, string[]> Performed => new()
    {
        { "Query", new[] { "Query" } },
        { "Find", new[] { "Query" } },
        { "Insert", new[] { "Insert" } },
        { "Update", new[] { "Update" } },
        { "Delete", new[] { "Delete" } },
        { "BulkUpdate", new[] { "Update" } },
        { "BulkDelete", new[] { "Delete" } },
        { "Upsert", new[] { "Insert", "Update" } },
        { "BulkUpsert", new[] { "Insert", "Update" } },
        { "Execute", new[] { "Execute" } },
    };

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    [MemberData(nameof(Performed))]
    public void ACommandStatesTheOperationsItsTranslatorEmits(string command, string[] operations)
    {
        var translator = MsSqlDataCommandTranslators.ByName(command);

        translator.ShouldBeAssignableTo<IStatesOperations>().Operations.ShouldBe(operations, ignoreOrder: true);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void NoCommandTypeSqlServerTranslatesIsLeftWithoutItsOperations()
    {
        var unstated = MsSqlDataCommandTranslators.All()
            .Where(translator => !string.Equals(translator.Name, "NotFound", System.StringComparison.Ordinal))
            .Where(translator => translator is not IStatesOperations { Operations.Count: > 0 })
            .Select(translator => translator.Name);

        unstated.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void NoVectorizedCommandSqlServerTranslatesIsLeftWithoutItsOperations()
    {
        var unstated = MsSqlVectorizedDataCommandTranslators.All()
            .Where(translator => !string.Equals(translator.Name, "NotFound", System.StringComparison.Ordinal))
            .Where(translator => translator is not IStatesOperations { Operations.Count: > 0 })
            .Select(translator => translator.Name);

        unstated.ShouldBeEmpty();
        MsSqlVectorizedDataCommandTranslators.ByName("Nearest").ShouldBeAssignableTo<IStatesOperations>().Operations.ShouldBe(["Query"]);
    }
}
