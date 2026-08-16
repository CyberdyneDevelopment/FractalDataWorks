using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.MsSql.Tests.Translators;

/// <summary>
/// Pins the FDW-547 write-cascade fix in <see cref="MsSqlConfigurationSaveTranslator"/>: a KVP child
/// save carries the owner's logical FK via <see cref="IConfigurationSaveCommand.AdditionalColumnValues"/>,
/// and the version-on-write UPDATE predicate scopes to the (ownerFk, Name) natural key rather than the
/// owner FK alone — otherwise a second KVP entry for the same owner deactivates the first.
/// </summary>
[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlConfigurationSaveTranslatorTests
{
    private readonly MsSqlConfigurationSaveTranslator _sut = new();

    // Why: a real [GenerateMapper] POCO so PocoMapperCollection.ByName resolves a genuine mapper —
    // mirrors conn.MsSqlConnectionAuthentication's Name/Value shape. Public + nested so the generated
    // mapper (emitted as a top-level class in this file's namespace) can reference it.
    [GenerateMapper]
    public sealed class TestKvpRow
    {
        public string Name { get; set; } = string.Empty;
        public string? Value { get; set; }
    }

    private static Mock<IField> CreateField(string name, bool isNullable = false)
    {
        var field = new Mock<IField>();
        field.Setup(f => f.Name).Returns(name);
        field.Setup(f => f.IsIdentity).Returns(false);
        field.Setup(f => f.IsComputed).Returns(false);
        field.Setup(f => f.IsSystemProvided).Returns(false);
        field.Setup(f => f.IsNullable).Returns(isNullable);
        return field;
    }

    private static IDataField CreateKeyField(string name)
    {
        var field = new Mock<IDataField>();
        field.Setup(f => f.Name).Returns(name);
        return field.Object;
    }

    private static IContainerKeyField CreateKeyFieldEntry(string localFieldName)
    {
        var keyField = new Mock<IContainerKeyField>();
        keyField.Setup(k => k.LocalField).Returns(CreateKeyField(localFieldName));
        return keyField.Object;
    }

    /// <summary>
    /// A KVP child container (conn.MsSqlConnectionAuthentication shape): a physical logical-FK column
    /// (MsSqlConnectionId), a physical RowId-FK column (MsSqlConnectionRowId, declared via a Foreign
    /// key referencing the parent), Name/Value columns, and a PropertyCollection key declaring
    /// MsSqlConnectionId as the owner-FK natural-key field (drives the FDW-547 UPDATE predicate fix).
    /// </summary>
    private static Mock<IDataContainer> CreateKvpChildContainer()
    {
        var dbPath = new DatabasePath("", "conn", "MsSqlConnectionAuthentication");
        var fields = new[]
        {
            CreateField("MsSqlConnectionId").Object,
            CreateField("MsSqlConnectionRowId").Object,
            CreateField("Name").Object,
            CreateField("Value", isNullable: true).Object,
        };
        var containerSchema = new Mock<IContainerSchema>();
        containerSchema.Setup(s => s.Fields).Returns(fields);
        containerSchema.Setup(s => s.GetProjectableFields()).Returns(fields);

        var parentPath = new Mock<IDataPath>();
        parentPath.Setup(p => p.Name).Returns("conn");

        var parentContainer = new Mock<IDataContainer>();
        parentContainer.Setup(p => p.Name).Returns("MsSqlConnection");
        parentContainer.Setup(p => p.Parent).Returns(parentPath.Object);

        var fkKey = new Mock<IContainerKey>();
        fkKey.Setup(k => k.KeyType).Returns((KeyTypeBase)KeyTypes.ByName("Foreign"));
        fkKey.Setup(k => k.ReferencedContainer).Returns(parentContainer.Object);
        fkKey.Setup(k => k.KeyFields).Returns(new List<IContainerKeyField> { CreateKeyFieldEntry("MsSqlConnectionRowId") });

        var pcKey = new Mock<IContainerKey>();
        pcKey.Setup(k => k.KeyType).Returns((KeyTypeBase)KeyTypes.ByName("PropertyCollection"));
        pcKey.Setup(k => k.KeyFields).Returns(new List<IContainerKeyField> { CreateKeyFieldEntry("MsSqlConnectionId") });

        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns("MsSqlConnectionAuthentication");
        container.Setup(c => c.Path).Returns(dbPath);
        container.Setup(c => c.Schema).Returns(containerSchema.Object);
        container.Setup(c => c.Keys).Returns(new List<IContainerKey> { fkKey.Object, pcKey.Object });

        return container;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateInsertsOwnerColumnAndSubqueriesPhysicalRowIdFk()
    {
        var container = CreateKvpChildContainer();
        var ownerId = Guid.NewGuid();
        var extra = new Dictionary<string, object?>(StringComparer.Ordinal) { ["MsSqlConnectionId"] = ownerId };
        var command = new ConfigurationSaveCommand<TestKvpRow>(new TestKvpRow { Name = "Timeout", Value = "30" }, extra);

        var result = await _sut.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var sql = result.Value!.CommandText;
        sql.ShouldContain("@MsSqlConnectionId");
        sql.ShouldContain("(SELECT [RowId] FROM [conn].[MsSqlConnection] WHERE [Id] = @MsSqlConnectionId AND [IsCurrent] = 1)");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateScopesUpdatePredicateToOwnerAndNameNotOwnerAlone()
    {
        var container = CreateKvpChildContainer();
        var ownerId = Guid.NewGuid();
        var extra = new Dictionary<string, object?>(StringComparer.Ordinal) { ["MsSqlConnectionId"] = ownerId };
        var command = new ConfigurationSaveCommand<TestKvpRow>(new TestKvpRow { Name = "Timeout", Value = "30" }, extra);

        var result = await _sut.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var sql = result.Value!.CommandText;
        // Why: the regression this pins — scoping the predicate to the owner FK alone would deactivate
        // EVERY sibling KVP row for that owner on each new-entry insert (bag collapses to last entry).
        sql.ShouldContain("[MsSqlConnectionId] = @MsSqlConnectionId AND [Name] = @Name");
    }
}
