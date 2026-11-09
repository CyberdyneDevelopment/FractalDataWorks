using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Configuration;

/// <summary>
/// Tests for <c>ConfigurationWriteScope.OwnerBinding</c> (FDW-4240): a child container's owner key is the key it DECLARES as owner, whichever key of the
/// parent it references; a container that declares none keeps the earlier rule (the key to the parent's Physical key); a key to the Logical key that is
/// not declared the owner cites a record and holds nothing.
/// </summary>
public sealed class OwnerBindingTests
{
    private const string Child = "WorkspaceMember";

    private static IDataField Field(string name)
    {
        var field = new Mock<IDataField>();
        field.Setup(f => f.Name).Returns(name);
        return field.Object;
    }

    private static IContainerKeyField KeyField(string localColumn, string? referencedColumn)
    {
        var keyField = new Mock<IContainerKeyField>();
        keyField.Setup(k => k.LocalField).Returns(Field(localColumn));
        keyField.Setup(k => k.ReferencedField).Returns(referencedColumn is null ? null : Field(referencedColumn));
        return keyField.Object;
    }

    private static IContainerKey Key(string name, KeyTypeBase keyType, bool isOwner, string localColumn, string? referencedColumn)
    {
        var key = new Mock<IContainerKey>();
        key.Setup(k => k.KeyName).Returns(name);
        key.Setup(k => k.KeyType).Returns(keyType);
        key.Setup(k => k.IsOwner).Returns(isOwner);
        key.Setup(k => k.KeyFields).Returns(new List<IContainerKeyField> { KeyField(localColumn, referencedColumn) });
        return key.Object;
    }

    private static IDataContainer Container(string name, IReadOnlyList<IContainerKey>? keys = null, IReadOnlyList<ReferencingKeyBinding>? referencing = null)
    {
        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns(name);
        container.Setup(c => c.Keys).Returns(keys ?? []);
        container.Setup(c => c.ReferencingKeys).Returns(GenericResult<IReadOnlyList<ReferencingKeyBinding>>.Success(referencing ?? []));
        return container.Object;
    }

    private static IDataContainer Parent(params IContainerKey[] childKeys)
    {
        var child = Container(Child);
        var bindings = new List<ReferencingKeyBinding>();
        foreach (var key in childKeys)
        {
            bindings.Add(new ReferencingKeyBinding(key, child));
        }

        return Container(
            "WorkspaceConfiguration",
            [Key("PK_WorkspaceConfiguration", KeyTypes.Physical, false, "RowId", null), Key("PK_WorkspaceConfiguration_Logical", KeyTypes.Logical, false, "Id", null)],
            bindings);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void ADeclaredOwnerKeyToTheLogicalKeyIsTheBinding()
    {
        var owner = Key("FK_WorkspaceMember_WorkspaceConfiguration", KeyTypes.Foreign, true, "WorkspaceConfigurationId", "Id");

        var binding = ConfigurationWriteScope.OwnerBinding(NullLogger.Instance, Parent(owner), Child);

        binding.IsSuccess.ShouldBeTrue();
        binding.Value!.Key.ShouldBeSameAs(owner);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void AContainerThatDeclaresNoOwnerKeepsTheKeyToThePhysicalKey()
    {
        var physical = Key("FK_WorkspaceMember_WorkspaceConfiguration", KeyTypes.Foreign, false, "WorkspaceConfigurationRowId", "RowId");

        var binding = ConfigurationWriteScope.OwnerBinding(NullLogger.Instance, Parent(physical), Child);

        binding.IsSuccess.ShouldBeTrue();
        binding.Value!.Key.ShouldBeSameAs(physical);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void AKeyToTheLogicalKeyThatIsNotDeclaredTheOwnerHoldsNothing()
    {
        var citation = Key("FK_WorkspaceMember_Citation", KeyTypes.Foreign, false, "CitedWorkspaceId", "Id");

        var binding = ConfigurationWriteScope.OwnerBinding(NullLogger.Instance, Parent(citation), Child);

        binding.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void TheDeclaredOwnerKeyWinsOverAKeyToThePhysicalKey()
    {
        var owner = Key("FK_WorkspaceMember_Owner", KeyTypes.Foreign, true, "WorkspaceConfigurationId", "Id");
        var physical = Key("FK_WorkspaceMember_Version", KeyTypes.Foreign, false, "WorkspaceConfigurationRowId", "RowId");

        var binding = ConfigurationWriteScope.OwnerBinding(NullLogger.Instance, Parent(physical, owner), Child);

        binding.IsSuccess.ShouldBeTrue();
        binding.Value!.Key.ShouldBeSameAs(owner);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void TwoDeclaredOwnerKeysAreAmbiguousAndNothingChoosesBetweenThem()
    {
        var first = Key("FK_WorkspaceMember_A", KeyTypes.Foreign, true, "AId", "Id");
        var second = Key("FK_WorkspaceMember_B", KeyTypes.Foreign, true, "BId", "Id");

        var binding = ConfigurationWriteScope.OwnerBinding(NullLogger.Instance, Parent(first, second), Child);

        binding.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void AnOwnerKeyToTheLogicalFieldHoldsTheDurableId()
    {
        var owner = Key("FK_WorkspaceMember_WorkspaceConfiguration", KeyTypes.Foreign, true, "WorkspaceConfigurationId", "Id");

        ConfigurationWriteScope.HoldsDurableId(Parent(owner), owner).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void AnOwnerKeyToThePhysicalFieldDoesNotHoldTheDurableId()
    {
        var physical = Key("FK_WorkspaceMember_WorkspaceConfiguration", KeyTypes.Foreign, false, "WorkspaceConfigurationRowId", "RowId");

        ConfigurationWriteScope.HoldsDurableId(Parent(physical), physical).ShouldBeFalse();
    }
}
