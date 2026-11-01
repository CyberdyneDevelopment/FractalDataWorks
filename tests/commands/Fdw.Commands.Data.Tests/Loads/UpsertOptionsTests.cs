using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.Upserts;
using Fdw.Commands.Data.ValueSources;

namespace Fdw.Commands.Data.Tests.Loads;

public sealed class UpsertOptionsTests
{
    private static UpsertOptions Valid() => new(["Code"], ["Name"], ["Code", "Name"]);

    [Fact]
    public void ConsistentOptionsHaveNoProblem() => Valid().FirstProblem().ShouldBeNull();

    [Fact]
    public void EmptyCompareColumnsAreAllowed() =>
        new UpsertOptions(["Code"], [], ["Code", "Name"]).FirstProblem().ShouldBeNull();

    [Fact]
    public void KeyColumnsAreRequired() =>
        new UpsertOptions([], ["Name"], ["Name"]).FirstProblem().ShouldNotBeNull();

    [Fact]
    public void WriteColumnsAreRequired() =>
        new UpsertOptions(["Code"], [], []).FirstProblem().ShouldNotBeNull();

    [Fact]
    public void KeyAndCompareColumnsMustBeWriteColumns() =>
        new UpsertOptions(["Code"], ["Name"], ["Code"]).FirstProblem()!.ShouldContain("Name");

    [Fact]
    public void ADuplicateColumnIsRefused() =>
        new UpsertOptions(["Code", "code"], ["Name"], ["Code", "Name"]).FirstProblem().ShouldNotBeNull();

    [Fact]
    public void VersioningColumnsCannotAlsoBeWriteColumns()
    {
        var options = new UpsertOptions(["Code"], ["Name"], ["Code", "Name", "EffectiveFrom"])
        {
            Versioning = new VersioningOptions("EffectiveFrom", "EffectiveTo"),
        };

        options.FirstProblem()!.ShouldContain("versioning");
    }

    [Fact]
    public void VersioningNeedsDifferentFromAndToColumns()
    {
        var options = new UpsertOptions(["Code"], ["Name"], ["Code", "Name"])
        {
            Versioning = new VersioningOptions("Effective", "effective"),
        };

        options.FirstProblem().ShouldNotBeNull();
    }

    [Fact]
    public void InsertOnlyRuleNeedsAColumnAndAValue()
    {
        var options = new UpsertOptions(["Code"], ["Name"], ["Code", "Name"])
        {
            Versioning = new VersioningOptions("From", "To") { InsertOnlyRule = new InsertOnlyRule("", "x") },
        };

        options.FirstProblem()!.ShouldContain("insert-only");
    }

    [Fact]
    public void ValueSourceColumnCannotAlsoBeAWriteColumn()
    {
        var options = new UpsertOptions(["Code"], ["Name"], ["Code", "Name"])
        {
            ValueSources = [new ColumnValueBinding("Name", new ConstantValueSource("x"))],
        };

        options.FirstProblem().ShouldNotBeNull();
    }

    [Fact]
    public void ColumnValueSourceIsRefusedForAnUpsert()
    {
        var options = new UpsertOptions(["Code"], ["Name"], ["Code", "Name"])
        {
            ValueSources = [new ColumnValueBinding("Alias", new ColumnReferenceValueSource("Name"))],
        };

        options.FirstProblem()!.ShouldContain("Alias");
    }

    [Fact]
    public void UpsertCommandsCarryTheirOptionsAndTheVerbAsCommandType()
    {
        var rows = new List<Dictionary<string, object?>>();

        new UpsertCommand<Dictionary<string, object?>>(rows, Valid()).CommandType.ShouldBe("Upsert");
        new BulkUpsertCommand<Dictionary<string, object?>>(rows, Valid()).CommandType.ShouldBe("BulkUpsert");
    }
}
