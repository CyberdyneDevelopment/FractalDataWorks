using Fdw.Services.Users.Models;

namespace Fdw.Services.Users.Tests.Models;

/// <summary>
/// Tests for UserInfo class.
/// </summary>
public sealed class UserInfoTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void DefaultConstructorCreatesInstance()
    {
        // Act
        var user = new UserInfo();

        // Assert
        user.ShouldNotBeNull();
        user.Id.ShouldBe(Guid.Empty);
        user.Username.ShouldBe(string.Empty);
        user.Email.ShouldBeNull();
        user.IsActive.ShouldBeTrue();
        user.LastLoginAt.ShouldBeNull();
        user.CreatedAt.ShouldBe(default(DateTimeOffset));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void CanSetAndGetId()
    {
        // Arrange
        var user = new UserInfo();
        var id = Guid.NewGuid();

        // Act
        user.Id = id;

        // Assert
        user.Id.ShouldBe(id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void CanSetAndGetUsername()
    {
        // Arrange
        var user = new UserInfo();
        var username = "testuser";

        // Act
        user.Username = username;

        // Assert
        user.Username.ShouldBe(username);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void CanSetAndGetEmail()
    {
        // Arrange
        var user = new UserInfo();
        var email = "test@example.com";

        // Act
        user.Email = email;

        // Assert
        user.Email.ShouldBe(email);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void CanSetEmailToNull()
    {
        // Arrange
        var user = new UserInfo { Email = "test@example.com" };

        // Act
        user.Email = null;

        // Assert
        user.Email.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void CanSetAndGetIsActive()
    {
        // Arrange
        var user = new UserInfo();

        // Act
        user.IsActive = false;

        // Assert
        user.IsActive.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IsActiveDefaultsToTrue()
    {
        // Arrange & Act
        var user = new UserInfo();

        // Assert
        user.IsActive.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void CanSetAndGetLastLoginAt()
    {
        // Arrange
        var user = new UserInfo();
        var loginTime = DateTimeOffset.UtcNow;

        // Act
        user.LastLoginAt = loginTime;

        // Assert
        user.LastLoginAt.ShouldBe(loginTime);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void CanSetLastLoginAtToNull()
    {
        // Arrange
        var user = new UserInfo { LastLoginAt = DateTimeOffset.UtcNow };

        // Act
        user.LastLoginAt = null;

        // Assert
        user.LastLoginAt.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void CanSetAndGetCreatedAt()
    {
        // Arrange
        var user = new UserInfo();
        var createdTime = DateTimeOffset.UtcNow;

        // Act
        user.CreatedAt = createdTime;

        // Assert
        user.CreatedAt.ShouldBe(createdTime);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void CanCreateUserWithObjectInitializer()
    {
        // Arrange
        var id = Guid.NewGuid();
        var username = "testuser";
        var email = "test@example.com";
        var isActive = false;
        var lastLogin = DateTimeOffset.UtcNow.AddDays(-1);
        var created = DateTimeOffset.UtcNow.AddDays(-30);

        // Act
        var user = new UserInfo
        {
            Id = id,
            Username = username,
            Email = email,
            IsActive = isActive,
            LastLoginAt = lastLogin,
            CreatedAt = created
        };

        // Assert
        user.Id.ShouldBe(id);
        user.Username.ShouldBe(username);
        user.Email.ShouldBe(email);
        user.IsActive.ShouldBe(isActive);
        user.LastLoginAt.ShouldBe(lastLogin);
        user.CreatedAt.ShouldBe(created);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ImplementsIUserInterface()
    {
        // Arrange & Act
        var user = new UserInfo();

        // Assert
        user.ShouldBeAssignableTo<IUser>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void UsernameCanBeEmptyString()
    {
        // Arrange
        var user = new UserInfo();

        // Act
        user.Username = string.Empty;

        // Assert
        user.Username.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void EmailCanBeEmptyString()
    {
        // Arrange
        var user = new UserInfo();

        // Act
        user.Email = string.Empty;

        // Assert
        user.Email.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IdCanBeEmptyGuid()
    {
        // Arrange
        var user = new UserInfo();

        // Act
        user.Id = Guid.Empty;

        // Assert
        user.Id.ShouldBe(Guid.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void CreatedAtCanBeMinValue()
    {
        // Arrange
        var user = new UserInfo();

        // Act
        user.CreatedAt = DateTimeOffset.MinValue;

        // Assert
        user.CreatedAt.ShouldBe(DateTimeOffset.MinValue);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void CreatedAtCanBeMaxValue()
    {
        // Arrange
        var user = new UserInfo();

        // Act
        user.CreatedAt = DateTimeOffset.MaxValue;

        // Assert
        user.CreatedAt.ShouldBe(DateTimeOffset.MaxValue);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void LastLoginAtCanBeMinValue()
    {
        // Arrange
        var user = new UserInfo();

        // Act
        user.LastLoginAt = DateTimeOffset.MinValue;

        // Assert
        user.LastLoginAt.ShouldBe(DateTimeOffset.MinValue);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void LastLoginAtCanBeMaxValue()
    {
        // Arrange
        var user = new UserInfo();

        // Act
        user.LastLoginAt = DateTimeOffset.MaxValue;

        // Assert
        user.LastLoginAt.ShouldBe(DateTimeOffset.MaxValue);
    }
}
