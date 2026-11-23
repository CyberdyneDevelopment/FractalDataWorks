using System.Collections.Generic;
using System.Linq;

namespace Fdw.Data.Abstractions.Tests.Containers;

/// <summary>
/// The primary key a storage container declares is read from <see cref="IStorageContainer.Keys"/> and from nowhere else (FDW-6309).
/// A composite key is answered whole and never reduced to its first column: a write that matched on one column of it would write every
/// row that shares that value, and a page ordered by one column of it leaves rows that tie in no fixed order.
/// </summary>
public sealed class StorageContainerKeysTests
{
    private static IContainerKey Key(KeyTypeBase type, params (string Field, int Ordinal)[] fields)
    {
        var keyFields = fields.Select(entry =>
        {
            var local = new Mock<IDataField>();
            local.Setup(f => f.Name).Returns(entry.Field);
            var keyField = new Mock<IContainerKeyField>();
            keyField.Setup(k => k.LocalField).Returns(local.Object);
            keyField.Setup(k => k.Ordinal).Returns(entry.Ordinal);
            return keyField.Object;
        }).ToList();
        var key = new Mock<IContainerKey>();
        key.Setup(k => k.KeyType).Returns(type);
        key.Setup(k => k.KeyFields).Returns(keyFields);
        return key.Object;
    }

    private static IStorageContainer Container(IReadOnlyList<IContainerKey> keys, Dictionary<string, object>? metadata = null)
    {
        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Keys).Returns(keys);
        container.Setup(c => c.Metadata).Returns(metadata ?? new Dictionary<string, object>());
        return container.Object;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AOneColumnPrimaryKeyIsAnsweredByItsColumn()
    {
        var container = Container([Key(new PrimaryKeyType(), ("Id", 0))]);

        container.PrimaryKeyFieldNames().ShouldBe(new[] { "Id" });
        container.GetPrimaryKeyFieldName().ShouldBe("Id");
        container.DeclaredOrderFieldNames().ShouldBe(new[] { "Id" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ACompositePrimaryKeyIsAnsweredWholeInKeyOrder()
    {
        // The key's fields are listed out of order; the answer follows the key's own ordinals.
        var container = Container([Key(new PrimaryKeyType(), ("TenantId", 1), ("OrderId", 0))]);

        container.PrimaryKeyFieldNames().ShouldBe(new[] { "OrderId", "TenantId" });
        container.DeclaredOrderFieldNames().ShouldBe(new[] { "OrderId", "TenantId" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ACompositePrimaryKeyHasNoSingleColumnNameAndIsNeverReducedToItsFirstColumn()
    {
        var container = Container([Key(new PrimaryKeyType(), ("OrderId", 0), ("TenantId", 1))]);

        container.GetPrimaryKeyFieldName().ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AContainerThatDeclaresNoKeyAnswersNothing()
    {
        var container = Container([]);

        container.PrimaryKeyFieldNames().ShouldBeEmpty();
        container.GetPrimaryKeyFieldName().ShouldBeNull();
        container.DeclaredOrderFieldNames().ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AForeignKeyIsNotThePrimaryKey()
    {
        var container = Container([Key(new ForeignKeyType(), ("CustomerId", 0))]);

        container.PrimaryKeyFieldNames().ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheMetadataEntryNoBuilderEverWroteIsNoLongerRead()
    {
        // GetPrimaryKeyFieldName read Metadata["SurrogateKeyField"], which no builder wrote, so a built container never answered.
        // Keys is the one place a container states its key; the entry answers nothing.
        var container = Container([], new Dictionary<string, object> { ["SurrogateKeyField"] = "RowId" });

        container.GetPrimaryKeyFieldName().ShouldBeNull();
        container.DeclaredOrderFieldNames().ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheFirstPrimaryKeyThatNamesAFieldIsTheOneAnswered()
    {
        var container = Container(
        [
            Key(new PrimaryKeyType()),
            Key(new PrimaryKeyType(), ("Id", 0)),
            Key(new PrimaryKeyType(), ("Other", 0)),
        ]);

        container.PrimaryKeyFieldNames().ShouldBe(new[] { "Id" });
    }
}
