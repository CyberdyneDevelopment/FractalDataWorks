using Fdw.Services.Users.Models;

namespace Fdw.Services.Users.Tests.Models;

/// <summary>
/// Tests for IUser interface.
/// </summary>
public sealed class IUserTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IUserIsPublicInterface()
    {
        // Arrange
        var type = typeof(IUser);

        // Assert
        type.IsInterface.ShouldBeTrue();
        type.IsPublic.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IdPropertyExists()
    {
        // Arrange
        var type = typeof(IUser);

        // Act
        var property = type.GetProperty("Id");

        // Assert
        property.ShouldNotBeNull();
        property.PropertyType.ShouldBe(typeof(Guid));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void UsernamePropertyExists()
    {
        // Arrange
        var type = typeof(IUser);

        // Act
        var property = type.GetProperty("Username");

        // Assert
        property.ShouldNotBeNull();
        property.PropertyType.ShouldBe(typeof(string));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void EmailPropertyExists()
    {
        // Arrange
        var type = typeof(IUser);

        // Act
        var property = type.GetProperty("Email");

        // Assert
        property.ShouldNotBeNull();
        property.PropertyType.ShouldBe(typeof(string));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IsActivePropertyExists()
    {
        // Arrange
        var type = typeof(IUser);

        // Act
        var property = type.GetProperty("IsActive");

        // Assert
        property.ShouldNotBeNull();
        property.PropertyType.ShouldBe(typeof(bool));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void LastLoginAtPropertyExists()
    {
        // Arrange
        var type = typeof(IUser);

        // Act
        var property = type.GetProperty("LastLoginAt");

        // Assert
        property.ShouldNotBeNull();
        property.PropertyType.ShouldBe(typeof(DateTimeOffset?));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void CreatedAtPropertyExists()
    {
        // Arrange
        var type = typeof(IUser);

        // Act
        var property = type.GetProperty("CreatedAt");

        // Assert
        property.ShouldNotBeNull();
        property.PropertyType.ShouldBe(typeof(DateTimeOffset));
        property.CanRead.ShouldBeTrue();
    }
}
