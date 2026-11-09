using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections.Sql.Tests.Fakes;

namespace Fdw.Services.Connections.Sql.Tests;

/// <summary>
/// A key list with more values than the connection's setting goes to the dialect's table form: one JSON parameter carries it and the condition reads the form's
/// subquery. At or below the setting, with a null in the list or on a joined table's column, the values stay parameters; outside a connection's scope the
/// builder stops (FDW-4267).
/// </summary>
[Collection(nameof(SqlTranslatorTestCollection))]
public sealed class SqlTranslatorBaseKeyListSpillTests
{
    private static readonly FakeDialect Dialect = new();

    private static IFilterExpression In(string column, IEnumerable<object?> values, string op = "In")
        => new FilterExpression { Root = new FilterCondition { PropertyName = column, Operator = FilterOperators.ByName(op), Value = values.ToList() } };

    private static (string Sql, List<(string Name, object? Value)> Parameters) Build(IFilterExpression filter)
    {
        var captured = new List<(string, object?)>();
        var sql = SqlTranslatorProxy.ExposeWhereClause(filter, Dialect, (name, value) => captured.Add((name, value)));
        return (sql, captured);
    }

    private static IEnumerable<object?> Ints(int count) => Enumerable.Range(1, count).Select(i => (object?)i);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void OutsideAScopeTheBuilderStopsAndNeverChoosesASetting()
    {
        // The connection always opens the scope; a builder reached without one was called by something that skipped the connection (FDW-4267).
        Should.Throw<InvalidOperationException>(() => Build(In("id", Ints(200)))).Message.ShouldContain("outside a connection's scope");
        Should.Throw<InvalidOperationException>(() => Build(In("id", Ints(0))));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void AListAtTheSettingStaysParameters()
    {
        using var scope = KeyListSpillScope.Begin(new TranslationSettings(50), "[t]");

        var (_, parameters) = Build(In("id", Ints(50)));

        parameters.Count.ShouldBe(50);
        scope.HasStatements.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void AListOverTheSettingIsOneJsonParameterJoinedThroughTheDialectsSubquery()
    {
        using var scope = KeyListSpillScope.Begin(new TranslationSettings(50), "[t]");

        var (sql, parameters) = Build(In("id", Ints(51)));

        parameters.Count.ShouldBe(1);
        parameters[0].Name.ShouldBe("kl0");
        parameters[0].Value.ShouldBeOfType<string>().ShouldStartWith("[1,2,3");
        sql.ShouldStartWith("\"id\" IN (SELECT v FROM keys_");
        scope.Before.ShouldHaveSingleItem().ShouldContain("FROM [t] USING @kl0");
        scope.After.ShouldHaveSingleItem().ShouldContain("CLEAN");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void ANotInListKeepsItsOperatorWhenItSpills()
    {
        using var scope = KeyListSpillScope.Begin(new TranslationSettings(5), "[t]");

        var (sql, _) = Build(In("id", Ints(6), "NotIn"));

        sql.ShouldContain(" NOT IN (SELECT v FROM keys_");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void AListThatHoldsANullKeepsItsParametersSoItsMeaningDoesNotChange()
    {
        using var scope = KeyListSpillScope.Begin(new TranslationSettings(5), "[t]");

        var (_, parameters) = Build(In("id", Ints(10).Append(null)));

        parameters.Count.ShouldBe(11);
        scope.HasStatements.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public void AColumnOfAJoinedTableKeepsItsParameters()
    {
        using var scope = KeyListSpillScope.Begin(new TranslationSettings(5), "[t]");

        var (_, parameters) = Build(In("Other.id", Ints(10)));

        parameters.Count.ShouldBe(10);
        scope.HasStatements.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public void AContainerThatIsNotADatabaseTableKeepsItsParameters()
    {
        using var scope = KeyListSpillScope.Begin(new TranslationSettings(5), null);

        var (_, parameters) = Build(In("id", Ints(10)));

        parameters.Count.ShouldBe(10);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void TheScopeEndsWithTheTranslationAndLeavesNoSettingBehind()
    {
        using (KeyListSpillScope.Begin(new TranslationSettings(5), "[t]"))
        {
            KeyListSpillScope.Current.ShouldNotBeNull();
        }

        KeyListSpillScope.Current.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task ABoundTranslatorRunsTheTranslatorInAScopeWithTheConnectionsSetting()
    {
        var inner = new ScopeRecordingTranslator();
        var bound = new SettingsBoundTranslator<object>(inner, new TranslationSettings(37));

        await bound.Translate(new Moq.Mock<IDataCommand>().Object, new Moq.Mock<IStorageContainer>().Object, TestContext.Current.CancellationToken);

        inner.Seen.ShouldBe(new TranslationSettings(37));
        KeyListSpillScope.Current.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task ATranslationMadeDirectlyRunsInNoScope()
    {
        var inner = new ScopeRecordingTranslator();

        await inner.Translate(new Moq.Mock<IDataCommand>().Object, new Moq.Mock<IStorageContainer>().Object, TestContext.Current.CancellationToken);

        inner.RanInAScope.ShouldBeFalse();
    }
}
