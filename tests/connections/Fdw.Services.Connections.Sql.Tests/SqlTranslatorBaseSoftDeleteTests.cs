using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data;
using Fdw.Commands.Data.SoftDeletes;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections.Sql.Tests.Fakes;

namespace Fdw.Services.Connections.Sql.Tests;

/// <summary>
/// Pins the UPDATE a soft delete runs in place of a DELETE on the SQL kinds that have no statement of their own:
/// the deleted flag, the optional deleted-at stamp, the rows taken from the filter and the marker, and rows already
/// flagged left alone.
/// </summary>
public sealed class SqlTranslatorBaseSoftDeleteTests
{
    private static readonly FakeDialect Dialect = new();

    private static Mock<IStorageContainer> Container(params (string Name, bool Identity, bool Computed)[] columns)
    {
        var fields = columns.Select(c =>
        {
            var field = new Mock<IField>();
            field.Setup(f => f.Name).Returns(c.Name);
            field.Setup(f => f.IsIdentity).Returns(c.Identity);
            field.Setup(f => f.IsComputed).Returns(c.Computed);
            return field.Object;
        }).ToList();

        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(fields);
        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns("Role");
        container.Setup(c => c.Schema).Returns(schema.Object);
        return container;
    }

    private static Mock<IStorageContainer> Standard() =>
        Container(("Id", false, false), ("Name", false, false), ("IsCurrent", false, false), ("IsDeleted", false, false), ("ModifyDate", false, false));

    private static (string Sql, Dictionary<string, object?> Parameters) Build(
        SoftDeleteOptions options, IStorageContainer container, string? filter = null)
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal);
        var sql = SqlTranslatorProxy.ExposeSoftDeleteUpdate("\"authz\".\"Role\"", Dialect, options, container, "now()", filter, (n, v) => parameters[n] = v);
        return (sql, parameters);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FilterMatchesFlagsTheFilteredRowsAndSkipsRowsAlreadyFlagged()
    {
        var options = new SoftDeleteOptions("IsDeleted", SoftDeleteModes.FilterMatches) { DeletedAtColumn = "ModifyDate" };

        var (sql, parameters) = Build(options, Standard().Object, "\"Id\" = @p0");

        sql.ShouldBe("UPDATE \"authz\".\"Role\" SET \"IsDeleted\" = @fdw_sd_flag, \"ModifyDate\" = now() "
            + "WHERE (\"Id\" = @p0) AND (\"IsDeleted\" IS NULL OR \"IsDeleted\" <> @fdw_sd_flag)");
        parameters["fdw_sd_flag"].ShouldBe(true);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ClosingAFlagSetsItToFalseAndSkipsRowsAlreadyClosed()
    {
        var options = new SoftDeleteOptions("IsCurrent", SoftDeleteModes.FilterMatches) { FlagValue = false, DeletedAtColumn = "ModifyDate" };

        var (sql, parameters) = Build(options, Standard().Object, "\"Id\" = @p0");

        sql.ShouldBe("UPDATE \"authz\".\"Role\" SET \"IsCurrent\" = @fdw_sd_flag, \"ModifyDate\" = now() "
            + "WHERE (\"Id\" = @p0) AND (\"IsCurrent\" IS NULL OR \"IsCurrent\" <> @fdw_sd_flag)");
        parameters["fdw_sd_flag"].ShouldBe(false);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void NoDeletedAtColumnStampsNothing()
    {
        var (sql, _) = Build(new SoftDeleteOptions("IsDeleted", SoftDeleteModes.FilterMatches), Standard().Object);

        sql.ShouldBe("UPDATE \"authz\".\"Role\" SET \"IsDeleted\" = @fdw_sd_flag "
            + "WHERE (\"IsDeleted\" IS NULL OR \"IsDeleted\" <> @fdw_sd_flag)");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MarkerMatchesAddsTheMarkerTestAndItsParameter()
    {
        var options = new SoftDeleteOptions("IsDeleted", SoftDeleteModes.MarkerMatches) { Marker = new SoftDeleteMarker("Name", "gone") };

        var (sql, parameters) = Build(options, Standard().Object);

        sql.ShouldContain("WHERE \"Name\" = @fdw_sd_match0 AND (\"IsDeleted\" IS NULL");
        parameters["fdw_sd_match0"].ShouldBe("gone");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AbsentFromSourceIsRefusedNotIgnored()
    {
        var options = new SoftDeleteOptions("IsDeleted", SoftDeleteModes.AbsentFromSource)
        {
            AbsentFromSource = new AbsentKeySet(["Id"], new List<IReadOnlyDictionary<string, object?>>()),
        };

        var thrown = Should.Throw<InvalidOperationException>(() => Build(options, Standard().Object));

        thrown.Message.ShouldContain("absent from a source set");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AColumnTheContainerDoesNotDeclareIsRefused()
    {
        var thrown = Should.Throw<InvalidOperationException>(
            () => Build(new SoftDeleteOptions("NoSuchFlag", SoftDeleteModes.FilterMatches), Standard().Object));

        thrown.Message.ShouldContain("NoSuchFlag");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AComputedDeletedAtColumnIsRefused()
    {
        var container = Container(("IsDeleted", false, false), ("ModifyDate", false, true));

        var thrown = Should.Throw<InvalidOperationException>(
            () => Build(new SoftDeleteOptions("IsDeleted", SoftDeleteModes.FilterMatches) { DeletedAtColumn = "ModifyDate" }, container.Object));

        thrown.Message.ShouldContain("computed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AnIdentityDeletedFlagColumnIsRefused()
    {
        var container = Container(("IsDeleted", true, false));

        var thrown = Should.Throw<InvalidOperationException>(
            () => Build(new SoftDeleteOptions("IsDeleted", SoftDeleteModes.FilterMatches), container.Object));

        thrown.Message.ShouldContain("identity");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnUpdateThatCarriesSoftDeleteOptionsIsRefused()
    {
        var update = new UpdateCommand<object>(new object()) { SoftDelete = new SoftDeleteOptions("IsDeleted", SoftDeleteModes.FilterMatches) };

        Should.Throw<InvalidOperationException>(() => SqlTranslatorProxy.ExposeRefuseSoftDeleteOnUpdate(update, Standard().Object));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AnUpdateWithoutSoftDeleteOptionsIsLeftAlone()
    {
        Should.NotThrow(() => SqlTranslatorProxy.ExposeRefuseSoftDeleteOnUpdate(new UpdateCommand<object>(new object()), Standard().Object));
    }

    // ── Returned columns ─────────────────────────────────────────────────────

    private static InsertCommand<object> Returning(params string[] columns) => new(new object()) { ReturnColumns = columns };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheDeclaredNamesOfTheColumnsAskedForAreAnsweredInTheOrderAsked()
    {
        SqlTranslatorProxy.ExposeReturnedColumns(Returning("name", "ID"), Standard().Object).ShouldBe(["Name", "Id"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void NoColumnsAskedForAnswersNone()
    {
        SqlTranslatorProxy.ExposeReturnedColumns(new InsertCommand<object>(new object()), Standard().Object).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AColumnTheContainerDoesNotDeclareIsRefusedByName()
    {
        var thrown = Should.Throw<InvalidOperationException>(() => SqlTranslatorProxy.ExposeReturnedColumns(Returning("Nope"), Standard().Object));

        thrown.Message.ShouldContain("Nope");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AColumnAskedForTwiceIsRefused()
    {
        Should.Throw<InvalidOperationException>(() => SqlTranslatorProxy.ExposeReturnedColumns(Returning("Id", "id"), Standard().Object));
    }
}
