namespace Fdw.Data.Abstractions.Tests.Containers;

/// <summary>
/// A container whose rows hang from another container's rows is read from the keys it declares: a declared owner key, or a key that
/// references the other container's Physical key, holds it; a Foreign key to the other container's Logical key that is not declared the
/// owner only cites that record. Reads that tell a configuration from its parts use this rule (FDW-4644).
/// </summary>
public sealed class DataContainerOwnershipTests
{
    private static IDataField Field(string name)
    {
        var field = new Mock<IDataField>();
        field.Setup(f => f.Name).Returns(name);
        return field.Object;
    }

    private static IContainerKey Key(KeyTypeBase type, string localField, IDataContainer? referenced = null, string? referencedField = null, bool isOwner = false)
    {
        var keyField = new Mock<IContainerKeyField>();
        keyField.Setup(k => k.LocalField).Returns(Field(localField));
        keyField.Setup(k => k.ReferencedField).Returns(referencedField is null ? null : Field(referencedField));
        var key = new Mock<IContainerKey>();
        key.Setup(k => k.KeyType).Returns(type);
        key.Setup(k => k.KeyFields).Returns([keyField.Object]);
        key.Setup(k => k.ReferencedContainer).Returns(referenced);
        key.Setup(k => k.IsOwner).Returns(isOwner);
        return key.Object;
    }

    private static IDataContainer Container(params IContainerKey[] keys)
    {
        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Keys).Returns(keys);
        return container.Object;
    }

    // MsSqlConnectionConfiguration: RowId is its Physical key, Id its Logical key.
    private static IDataContainer Parent() => Container(Key(new PhysicalKeyType(), "RowId"), Key(new LogicalKeyType(), "Id"));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AKeyValueChildThatReferencesItsParentsPhysicalKeyIsOwned()
    {
        // MsSqlConnectionProperty: RowId, MsSqlConnectionConfigurationRowId -> PK_MsSqlConnectionConfiguration, Name, Value.
        var property = Container(
            Key(new PhysicalKeyType(), "RowId"),
            Key(new ForeignKeyType(), "MsSqlConnectionConfigurationRowId", Parent(), "RowId"));

        property.IsOwnedByAnotherContainer().ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AKeyDeclaredTheOwnerHoldsTheContainerWhicheverKeyItReferences()
    {
        var member = Container(
            Key(new PhysicalKeyType(), "RowId"),
            Key(new ForeignKeyType(), "WorkspaceId", Parent(), "Id", isOwner: true));

        member.IsOwnedByAnotherContainer().ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AForeignKeyToALogicalKeyThatIsNotDeclaredTheOwnerOnlyCitesTheRecord()
    {
        // ConnectionTag cites its Connection and its Tag by Id; neither holds it.
        var tag = Container(
            Key(new PhysicalKeyType(), "RowId"),
            Key(new LogicalKeyType(), "Id"),
            Key(new ForeignKeyType(), "ConnectionId", Parent(), "Id"));

        tag.IsOwnedByAnotherContainer().ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AContainerThatReferencesNoOtherContainerIsNotOwned()
    {
        Parent().IsOwnedByAnotherContainer().ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheWritePathsParentRelativeFormAppliesTheSameRule()
    {
        // The write path walks a parent's referencing keys, which need not carry the referenced container; it asks the rule of that parent.
        var physicalReference = Key(new ForeignKeyType(), "MsSqlConnectionConfigurationRowId", referenced: null, referencedField: "RowId");
        var logicalCitation = Key(new ForeignKeyType(), "ConnectionId", referenced: null, referencedField: "Id");

        physicalReference.HoldsOwnerUnder(Parent()).ShouldBeTrue();
        logicalCitation.HoldsOwnerUnder(Parent()).ShouldBeFalse();
        Key(new ForeignKeyType(), "WorkspaceId", referenced: null, referencedField: "Id", isOwner: true).HoldsOwnerUnder(Parent()).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void APhysicalForeignKeyIsNotTheContainersOwnPhysicalKey()
    {
        var child = Container(Key(new PhysicalKeyType(), "ParentRowId", Parent(), "RowId"), Key(new PhysicalKeyType(), "RowId"));

        child.PhysicalKeyField().ShouldBe("RowId");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AContainerWithNoPhysicalKeyOfItsOwnAnswersNone()
    {
        Container(Key(new LogicalKeyType(), "Id")).PhysicalKeyField().ShouldBeNull();
    }
}
