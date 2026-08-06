using Fdw.Services.Authorization.Abstractions;

namespace Fdw.Services.Authorization.Abstractions.Tests;

/// <summary>
/// Tests for PermissionActions TypeCollection.
/// </summary>
public class PermissionActionsTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AllReturnsAllActions()
    {
        // Act
        var all = PermissionActions.All();

        // Assert
        all.ShouldNotBeEmpty();
        all.Count.ShouldBeGreaterThanOrEqualTo(10);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ByIdReturnsCorrectAction()
    {
        // Act
        var result = PermissionActions.ById(1);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(1);
        result.Name.ShouldBe("Read");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ByIdReturnsNotFoundForUnknownId()
    {
        // Act
        var result = PermissionActions.ById(99999);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(PermissionActions.NotFound);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ByNameReturnsCorrectAction()
    {
        // Act
        var result = PermissionActions.ByName("Read");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Read");
        result.Id.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ByNameIsCaseSensitive()
    {
        // Act & Assert
        PermissionActions.ByName("Read").ShouldNotBeNull();
        PermissionActions.ByName("Read").Name.ShouldBe("Read");
        PermissionActions.ByName("read").ShouldBe(PermissionActions.NotFound);
        PermissionActions.ByName("READ").ShouldBe(PermissionActions.NotFound);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ByNameReturnsNotFoundForUnknownName()
    {
        // Act
        var result = PermissionActions.ByName("UnknownAction");

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(PermissionActions.NotFound);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void NotFoundReturnsEmptyInstance()
    {
        // Act
        var result = PermissionActions.NotFound;

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ReadActionHasCorrectProperties()
    {
        // Act
        var action = PermissionActions.ByName("Read");

        // Assert
        action.ShouldNotBeNull();
        action.Id.ShouldBe(1);
        action.Name.ShouldBe("Read");
        action.Icon.ShouldBe("visibility");
        action.Color.ShouldBe("Info");
        action.Description.ShouldBe("View resource data");
        action.IsWriteAction.ShouldBeFalse();
        action.IsDestructive.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void WriteActionHasCorrectProperties()
    {
        // Act
        var action = PermissionActions.ByName("Write");

        // Assert
        action.ShouldNotBeNull();
        action.Id.ShouldBe(2);
        action.Name.ShouldBe("Write");
        action.Icon.ShouldBe("edit");
        action.Color.ShouldBe("Success");
        action.Description.ShouldBe("Modify existing resources");
        action.IsWriteAction.ShouldBeTrue();
        action.IsDestructive.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CreateActionHasCorrectProperties()
    {
        // Act
        var action = PermissionActions.ByName("Create");

        // Assert
        action.ShouldNotBeNull();
        action.Id.ShouldBe(3);
        action.Icon.ShouldBe("add");
        action.IsWriteAction.ShouldBeTrue();
        action.IsDestructive.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void UpdateActionHasCorrectProperties()
    {
        // Act
        var action = PermissionActions.ByName("Update");

        // Assert
        action.ShouldNotBeNull();
        action.Id.ShouldBe(4);
        action.Icon.ShouldBe("edit");
        action.IsWriteAction.ShouldBeTrue();
        action.IsDestructive.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void DeleteActionHasCorrectProperties()
    {
        // Act
        var action = PermissionActions.ByName("Delete");

        // Assert
        action.ShouldNotBeNull();
        action.Id.ShouldBe(5);
        action.Icon.ShouldBe("delete");
        action.Color.ShouldBe("Error");
        action.IsWriteAction.ShouldBeTrue();
        action.IsDestructive.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ExecuteActionHasCorrectProperties()
    {
        // Act
        var action = PermissionActions.ByName("Execute");

        // Assert
        action.ShouldNotBeNull();
        action.Id.ShouldBe(6);
        action.Icon.ShouldBe("play_arrow");
        action.Color.ShouldBe("Warning");
        action.IsWriteAction.ShouldBeFalse();
        action.IsDestructive.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void BrowseActionHasCorrectProperties()
    {
        // Act
        var action = PermissionActions.ByName("Browse");

        // Assert
        action.ShouldNotBeNull();
        action.Id.ShouldBe(7);
        action.Icon.ShouldBe("folder_open");
        action.Color.ShouldBe("Info");
        action.IsWriteAction.ShouldBeFalse();
        action.IsDestructive.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AdminActionHasCorrectProperties()
    {
        // Act
        var action = PermissionActions.ByName("Admin");

        // Assert
        action.ShouldNotBeNull();
        action.Id.ShouldBe(8);
        action.Icon.ShouldBe("admin_panel_settings");
        action.Color.ShouldBe("Primary");
        action.IsWriteAction.ShouldBeTrue();
        action.IsDestructive.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ApproveActionHasCorrectProperties()
    {
        // Act
        var action = PermissionActions.ByName("Approve");

        // Assert
        action.ShouldNotBeNull();
        action.Id.ShouldBe(9);
        action.Icon.ShouldBe("check_circle");
        action.Color.ShouldBe("Tertiary");
        action.IsWriteAction.ShouldBeTrue();
        action.IsDestructive.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ManageActionHasCorrectProperties()
    {
        // Act
        var action = PermissionActions.ByName("Manage");

        // Assert
        action.ShouldNotBeNull();
        action.Id.ShouldBe(10);
        action.Icon.ShouldBe("settings");
        action.Color.ShouldBe("Primary");
        action.IsWriteAction.ShouldBeTrue();
        action.IsDestructive.ShouldBeFalse();
    }
}
