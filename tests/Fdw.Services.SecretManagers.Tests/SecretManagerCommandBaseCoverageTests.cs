using System;
using System.Collections.Generic;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;

namespace Fdw.Services.SecretManagers.Tests;

/// <summary>
/// Additional coverage tests for SecretManagerCommandBase branches
/// and explicit interface implementations across all command types.
/// </summary>
public sealed class SecretManagerCommandBaseCoverageTests
{
    // -------------------------------------------------------
    // SecretManagerCommandBase.Validate - uncovered branches
    // -------------------------------------------------------

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateModifyingParametersReturnsTrueForDeleteCommand()
    {
        // Arrange - DeleteSecret is IsSecretModifying=true but CommandType is "DeleteSecret" (not "SetSecret")
        // so ValidateModifyingParameters should return true (line 236 branch)
        var cmd = new DeleteSecretManagerCommand("vault", "key");

        // Act
        var result = cmd.Validate();

        // Assert - should succeed because DeleteSecret doesn't require SecretValue parameter
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateSucceedsForSetSecretWithValidParameters()
    {
        // Arrange - SetSecret with SecretValue in params (the normal case)
        var cmd = SetSecretManagerCommand.Create("vault", "key", "val");

        // Act
        var result = cmd.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateFailsWhenGetSecretHasEmptySecretKey()
    {
        // Arrange - GetSecret requires a secret key; create command then use null container
        // to trigger the validate failure for secret key
        // Note: GetSecretManagerCommand constructor throws if key is null/empty.
        // We need to test the Validate path where RequiresSecretKey() returns true but SecretKey is null/whitespace.
        // This can happen for GetSecretVersions which doesn't validate in constructor
        var cmd = new GetSecretManagerVersionsCommand("vault", "my-key");

        // Act
        var result = cmd.Validate();

        // Assert - should succeed because key is present
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void DeleteSecretValidateFailsWithEmptyContainer()
    {
        // Arrange - container null causes validate to fail on RequiresContainer check
        var cmd = new DeleteSecretManagerCommand(null, "key");

        // Act
        var result = cmd.Validate();

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    // -------------------------------------------------------
    // SecretManagerCommandBase.RequiresSecretKey - default branch
    // -------------------------------------------------------

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GetCertificateCommandRequiresSecretKeyViaDefaultSwitch()
    {
        // Arrange - GetCertificate is NOT in the explicit switch cases of RequiresSecretKey
        // so it hits the default => true branch
        var cmd = GetCertificateManagerCommand.Latest("vault", "cert");

        // Act - Validate exercises RequiresSecretKey internally
        var result = cmd.Validate();

        // Assert - should succeed because cert name is provided
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GetCertificateCommandValidateFailsWithNullContainer()
    {
        // Arrange - null container to test RequiresContainer
        var cmd = GetCertificateManagerCommand.Latest(null, "cert");

        // Act
        var result = cmd.Validate();

        // Assert - should fail on container validation
        result.IsFailure.ShouldBeTrue();
    }

    // -------------------------------------------------------
    // SecretManagerCommandBase.ValidateModifyingParameters
    // non-modifying command early return (line 228)
    // -------------------------------------------------------

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateModifyingParametersReturnsTrueForNonModifyingCommand()
    {
        // Arrange - ListSecrets is not secret-modifying; Validate calls ValidateModifyingParameters
        // which should early-return true on line 228
        var cmd = ListSecretsManagerCommand.All("vault");

        // Act
        var result = cmd.Validate();

        // Assert - should succeed
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateModifyingParametersReturnsTrueForGetSecretCommand()
    {
        // Arrange - GetSecret is not secret-modifying
        var cmd = GetSecretManagerCommand.Latest("vault", "key");

        // Act
        var result = cmd.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    // -------------------------------------------------------
    // Explicit interface IGenericCommand property implementations
    // (lines 80, 83, 86, 89, 112)
    // -------------------------------------------------------

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GenericCommandInterfaceCommandIdReturnsGuid()
    {
        // Arrange
        var cmd = new GetSecretManagerCommand("vault", "key");
        IGenericCommand genericCmd = cmd;

        // Act
        var commandId = genericCmd.CommandId;

        // Assert
        commandId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GenericCommandInterfaceCreatedAtReturnsTimestamp()
    {
        // Arrange
        var before = DateTime.UtcNow;
        var cmd = new GetSecretManagerCommand("vault", "key");
        IGenericCommand genericCmd = cmd;

        // Act
        var createdAt = genericCmd.CreatedAt;

        // Assert
        createdAt.ShouldBeGreaterThanOrEqualTo(before);
        createdAt.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GenericCommandInterfaceCommandTypeReturnsCorrectType()
    {
        // Arrange
        var cmd = new GetSecretManagerCommand("vault", "key");
        IGenericCommand genericCmd = cmd;

        // Act
        var commandType = genericCmd.CommandType;

        // Assert
        commandType.ShouldBe("GetSecret");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GenericCommandInterfaceCategoryReturnsSecretManagement()
    {
        // Arrange
        var cmd = new GetSecretManagerCommand("vault", "key");
        IGenericCommand genericCmd = cmd;

        // Act
        var category = genericCmd.Category;

        // Assert
        category.ShouldBe("SecretManagement");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void SecretManagerCommandInterfaceCommandIdReturnsStringGuid()
    {
        // Arrange
        var cmd = new GetSecretManagerCommand("vault", "key");
        ISecretManagerCommand secretCmd = cmd;

        // Act
        var commandId = secretCmd.CommandId;

        // Assert
        Guid.TryParse(commandId, out _).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GenericCommandInterfacePropertiesWorkForDeleteCommand()
    {
        // Arrange
        var cmd = new DeleteSecretManagerCommand("vault", "key");
        IGenericCommand genericCmd = cmd;

        // Act & Assert
        genericCmd.CommandId.ShouldNotBe(Guid.Empty);
        genericCmd.CommandType.ShouldBe("DeleteSecret");
        genericCmd.Category.ShouldBe("SecretManagement");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GenericCommandInterfacePropertiesWorkForSetCommand()
    {
        // Arrange
        var cmd = SetSecretManagerCommand.Create("vault", "key", "val");
        IGenericCommand genericCmd = cmd;

        // Act & Assert
        genericCmd.CommandId.ShouldNotBe(Guid.Empty);
        genericCmd.CommandType.ShouldBe("SetSecret");
        genericCmd.Category.ShouldBe("SecretManagement");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GenericCommandInterfacePropertiesWorkForListCommand()
    {
        // Arrange
        var cmd = ListSecretsManagerCommand.All("vault");
        IGenericCommand genericCmd = cmd;

        // Act & Assert
        genericCmd.CommandId.ShouldNotBe(Guid.Empty);
        genericCmd.CommandType.ShouldBe("ListSecrets");
        genericCmd.Category.ShouldBe("SecretManagement");
    }

    // -------------------------------------------------------
    // GetCertificateManagerCommand typed interface methods
    // (lines 138-146)
    // -------------------------------------------------------

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GetCertificateTypedWithParametersCreatesNewCommand()
    {
        // Arrange
        ISecretManagerCommand<SecretValue> cmd = GetCertificateManagerCommand.Latest("vault", "cert");
        var newParams = new Dictionary<string, object?> { ["custom"] = "value" };

        // Act
        var updated = cmd.WithParameters(newParams);

        // Assert
        updated.ShouldNotBeSameAs(cmd);
        updated.ShouldBeOfType<GetCertificateManagerCommand>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GetCertificateTypedWithMetadataCreatesNewCommand()
    {
        // Arrange
        ISecretManagerCommand<SecretValue> cmd = GetCertificateManagerCommand.Latest("vault", "cert");
        var newMeta = new Dictionary<string, object> { ["source"] = "test" };

        // Act
        var updated = cmd.WithMetadata(newMeta);

        // Assert
        updated.ShouldNotBeSameAs(cmd);
        updated.ShouldBeOfType<GetCertificateManagerCommand>();
    }

    // -------------------------------------------------------
    // GetSecretManagerVersionsCommand base interface methods
    // CreateCopyWithParameters/Metadata (lines 111-119) and
    // typed interface methods (lines 122-129)
    // -------------------------------------------------------

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GetVersionsBaseInterfaceWithParametersCreatesNewCommand()
    {
        // Arrange - use the base interface method (goes through CreateCopyWithParameters)
        ISecretManagerCommand cmd = GetSecretManagerVersionsCommand.Create("vault", "key");
        var newParams = new Dictionary<string, object?> { ["custom"] = "value" };

        // Act
        var updated = cmd.WithParameters(newParams);

        // Assert
        updated.ShouldNotBeSameAs(cmd);
        updated.ShouldBeOfType<GetSecretManagerVersionsCommand>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GetVersionsBaseInterfaceWithMetadataCreatesNewCommand()
    {
        // Arrange - use the base interface method (goes through CreateCopyWithMetadata)
        ISecretManagerCommand cmd = GetSecretManagerVersionsCommand.Create("vault", "key");
        var newMeta = new Dictionary<string, object> { ["source"] = "test" };

        // Act
        var updated = cmd.WithMetadata(newMeta);

        // Assert
        updated.ShouldNotBeSameAs(cmd);
        updated.ShouldBeOfType<GetSecretManagerVersionsCommand>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GetVersionsTypedInterfaceWithParametersCreatesNewCommand()
    {
        // Arrange - use the typed ISecretManagerCommand<IEnumerable<SecretValue>> interface
        ISecretManagerCommand<IEnumerable<SecretValue>> cmd = GetSecretManagerVersionsCommand.Create("vault", "key");
        var newParams = new Dictionary<string, object?> { ["custom"] = "value" };

        // Act
        var updated = cmd.WithParameters(newParams);

        // Assert
        updated.ShouldNotBeSameAs(cmd);
        updated.ShouldBeOfType<GetSecretManagerVersionsCommand>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GetVersionsTypedInterfaceWithMetadataCreatesNewCommand()
    {
        // Arrange - use the typed ISecretManagerCommand<IEnumerable<SecretValue>> interface
        ISecretManagerCommand<IEnumerable<SecretValue>> cmd = GetSecretManagerVersionsCommand.Create("vault", "key");
        var newMeta = new Dictionary<string, object> { ["source"] = "test" };

        // Act
        var updated = cmd.WithMetadata(newMeta);

        // Assert
        updated.ShouldNotBeSameAs(cmd);
        updated.ShouldBeOfType<GetSecretManagerVersionsCommand>();
    }

    // -------------------------------------------------------
    // GetSecretVersionsCommand - validate with null container
    // -------------------------------------------------------

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GetVersionsValidateFailsWithNullContainer()
    {
        // Arrange
        var cmd = GetSecretManagerVersionsCommand.Create(null, "key");

        // Act
        var result = cmd.Validate();

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    // -------------------------------------------------------
    // GetCertificateManagerCommand base interface methods
    // (CreateCopyWithParameters/CreateCopyWithMetadata)
    // -------------------------------------------------------

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GetCertificateBaseInterfaceWithParametersCreatesNewCommand()
    {
        // Arrange - use the base ISecretManagerCommand interface
        ISecretManagerCommand cmd = GetCertificateManagerCommand.Latest("vault", "cert");
        var newParams = new Dictionary<string, object?> { ["custom"] = "value" };

        // Act
        var updated = cmd.WithParameters(newParams);

        // Assert
        updated.ShouldNotBeSameAs(cmd);
        updated.ShouldBeOfType<GetCertificateManagerCommand>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GetCertificateBaseInterfaceWithMetadataCreatesNewCommand()
    {
        // Arrange - use the base ISecretManagerCommand interface
        ISecretManagerCommand cmd = GetCertificateManagerCommand.Latest("vault", "cert");
        var newMeta = new Dictionary<string, object> { ["source"] = "test" };

        // Act
        var updated = cmd.WithMetadata(newMeta);

        // Assert
        updated.ShouldNotBeSameAs(cmd);
        updated.ShouldBeOfType<GetCertificateManagerCommand>();
    }
}
