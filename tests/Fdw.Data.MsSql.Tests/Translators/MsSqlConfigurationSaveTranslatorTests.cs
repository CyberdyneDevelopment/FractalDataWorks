using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;
using Fdw.Results;
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

    /// <summary>
    /// The owner graph exactly as the store builder produces it: the foreign key references a BARE node
    /// that carries no keys, and the keys live on a separate final node reachable only back through the
    /// store by path and name. A translator that reads Keys off the reference sees nothing and fails;
    /// only one that resolves the final node finds the Logical key.
    /// </summary>
    private static IDataContainer WireOwner(string pathName, string containerName, params IContainerKey[] keys)
    {
        var store = new Mock<IDataStore>();
        var path = new Mock<IDataNodePath>();
        path.Setup(p => p.Name).Returns(pathName);
        path.Setup(p => p.Store).Returns(store.Object);

        var final = new Mock<IDataContainer>();
        final.Setup(c => c.Name).Returns(containerName);
        final.Setup(c => c.Parent).Returns(path.Object);
        final.Setup(c => c.Keys).Returns(new List<IContainerKey>(keys));

        var bare = new Mock<IDataContainer>();
        bare.Setup(c => c.Name).Returns(containerName);
        bare.Setup(c => c.Parent).Returns(path.Object);
        bare.Setup(c => c.Keys).Returns(new List<IContainerKey>());   // what the builder gives a reference

        path.Setup(p => p.Container(containerName)).Returns(GenericResult<IDataContainer>.Success(final.Object));
        store.Setup(s => s.Path(pathName)).Returns(GenericResult<IDataNodePath>.Success(path.Object));
        return bare.Object;
    }

    private static IContainerKey CreateKey(string keyName, string keyType, string fieldName)
    {
        var key = new Mock<IContainerKey>();
        key.Setup(k => k.KeyName).Returns(keyName);
        key.Setup(k => k.KeyType).Returns((KeyTypeBase)KeyTypes.ByName(keyType));
        key.Setup(k => k.KeyFields).Returns(new List<IContainerKeyField> { CreateKeyFieldEntry(fieldName) });
        return key.Object;
    }

    private static IContainerKeyField CreateKeyFieldEntry(string localFieldName)
    {
        var keyField = new Mock<IContainerKeyField>();
        keyField.Setup(k => k.LocalField).Returns(CreateKeyField(localFieldName));
        return keyField.Object;
    }

    /// <summary>
    /// A KVP child container (conn.MsSqlConnectionAuthentication shape): a physical logical-FK column
    /// (MsSqlConnectionConfigurationId), a physical RowId-FK column (MsSqlConnectionConfigurationRowId, declared via a Foreign
    /// key referencing the parent), Name/Value columns, and a PropertyCollection key declaring
    /// MsSqlConnectionConfigurationId as the owner-FK natural-key field (drives the FDW-547 UPDATE predicate fix).
    /// </summary>
    private static Mock<IDataContainer> CreateKvpChildContainer()
    {
        var dbPath = new DatabasePath("", "conn", "MsSqlConnectionAuthentication");
        var fields = new[]
        {
            CreateField("MsSqlConnectionConfigurationId").Object,
            CreateField("MsSqlConnectionConfigurationRowId").Object,
            CreateField("Name").Object,
            CreateField("Value", isNullable: true).Object,
        };
        var containerSchema = new Mock<IContainerSchema>();
        containerSchema.Setup(s => s.Fields).Returns(fields);
        containerSchema.Setup(s => s.GetReturnedFields(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>())).Returns(fields);

        var parentContainer = WireOwner("conn", "MsSqlConnection",
            CreateKey("PK_MsSqlConnection", "Physical", "RowId"),
            CreateKey("PK_MsSqlConnection_Logical", "Logical", "Id"));

        var fkKey = new Mock<IContainerKey>();
        fkKey.Setup(k => k.KeyType).Returns((KeyTypeBase)KeyTypes.ByName("Foreign"));
        fkKey.Setup(k => k.ReferencedContainer).Returns(parentContainer);
        fkKey.Setup(k => k.KeyFields).Returns(new List<IContainerKeyField> { CreateKeyFieldEntry("MsSqlConnectionConfigurationRowId") });

        var pcKey = new Mock<IContainerKey>();
        pcKey.Setup(k => k.KeyType).Returns((KeyTypeBase)KeyTypes.ByName("PropertyCollection"));
        pcKey.Setup(k => k.KeyFields).Returns(new List<IContainerKeyField> { CreateKeyFieldEntry("MsSqlConnectionConfigurationId") });

        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns("MsSqlConnectionAuthentication");
        container.Setup(c => c.Path).Returns(dbPath);
        container.Setup(c => c.Schema).Returns(containerSchema.Object);
        container.Setup(c => c.Keys).Returns(new List<IContainerKey> { fkKey.Object, pcKey.Object });
        container.Setup(c => c.ReferencingKeys).Returns(GenericResult<IReadOnlyList<ReferencingKeyBinding>>.Success([]));

        return container;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateWritesTheOwnerForeignKeyValueItIsGiven()
    {
        var container = CreateKvpChildContainer();
        var foreignKey = new Dictionary<string, object?>(StringComparer.Ordinal) { ["MsSqlConnectionConfigurationRowId"] = 42 };
        var command = new ConfigurationSaveCommand<TestKvpRow>(new TestKvpRow { Name = "Timeout", Value = "30" }, foreignKey, replacesCurrent: false);

        var result = await _sut.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var sql = result.Value!.CommandText;
        sql.ShouldContain("@MsSqlConnectionConfigurationRowId");
        // The owner key's value comes with the command; nothing is looked up from a durable Id.
        sql.ShouldNotContain("SELECT [RowId]");
        sql.ShouldNotContain("UPDATE");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateRetiresTheCurrentRowUnderTheSameOwner()
    {
        var container = CreateKvpChildContainer();
        var foreignKey = new Dictionary<string, object?>(StringComparer.Ordinal) { ["MsSqlConnectionConfigurationRowId"] = 42 };
        var command = new ConfigurationSaveCommand<TestKvpRow>(new TestKvpRow { Name = "Timeout", Value = "30" }, foreignKey, replacesCurrent: true);

        var result = await _sut.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var sql = result.Value!.CommandText;
        sql.ShouldContain("UPDATE");
        sql.ShouldContain("[MsSqlConnectionConfigurationRowId] = @MsSqlConnectionConfigurationRowId");
    }
}
