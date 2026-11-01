using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;

namespace Fdw.Commands.Data.Tests;

/// <summary>
/// A find answers each row with the fields that matched in it (FDW-1361): the connection reads the rows and the matched
/// field names, and the command builds its own result from them.
/// </summary>
public sealed class FindCommandResultsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ResultsPairsEachRowWithItsMatchedFieldsInOrder()
    {
        // Arrange
        var command = new FindCommand<string> { SearchTerm = "a" };
        var rows = new List<object> { "first", "second" };
        var matched = new List<IReadOnlyList<string>> { new[] { "Name" }, new[] { "Name", "Email" } };

        // Act
        var results = ((IEnumerable<FindResult<string>>)command.Results(rows, matched)).ToList();

        // Assert
        results.Count.ShouldBe(2);
        results[0].Record.ShouldBe("first");
        results[0].MatchedFields.ShouldBe(new[] { "Name" });
        results[1].Record.ShouldBe("second");
        results[1].MatchedFields.ShouldBe(new[] { "Name", "Email" });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ARowIsReadAsTheCommandsRowType()
    {
        // Arrange
        var command = new FindCommand<string> { SearchTerm = "a" };

        // Act / Assert
        command.RowType.ShouldBe(typeof(string));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AMatchColumnNamesTheFieldItReportsOn()
    {
        // Act / Assert
        FindMatchColumn.FieldOf(FindMatchColumn.NameFor("Email")).ShouldBe("Email");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AColumnOfTheContainerIsNotAMatchColumn()
    {
        // Act / Assert
        FindMatchColumn.FieldOf("Email").ShouldBeNull();
    }
}
