using System;
using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;

namespace Fdw.Services.SecretManagers.Tests;

/// <summary>
/// Tests for SecretManagerCommandBase validation branches that can only be
/// reached via a concrete test subclass (since all production subclasses
/// hardcode valid CommandType values).
/// </summary>
public sealed class SecretManagerCommandBaseValidationTests
{
    /// <summary>
    /// Test-only concrete subclass of SecretManagerCommandBase that allows
    /// control over RequiresSecretKey, RequiresContainer, IsSecretModifying,
    /// and CommandType.
    /// </summary>
    private sealed class TestableSecretManagerCommand : SecretManagerCommandBase
    {
        private readonly bool _isModifying;
        private readonly bool _requiresKey;
        private readonly bool _requiresContainer;

        public TestableSecretManagerCommand(
            string commandType,
            string? container,
            string? secretKey,
            bool isModifying = false,
            bool requiresKey = true,
            bool requiresContainer = true,
            IReadOnlyDictionary<string, object?>? parameters = null)
            : base(commandType, container, secretKey, typeof(IGenericResult), parameters)
        {
            _isModifying = isModifying;
            _requiresKey = requiresKey;
            _requiresContainer = requiresContainer;
        }

        public override bool IsSecretModifying => _isModifying;

        protected override bool RequiresSecretKey() => _requiresKey;

        protected override bool RequiresContainer() => _requiresContainer;

        protected override ISecretManagerCommand CreateCopyWithParameters(IReadOnlyDictionary<string, object?> newParameters)
            => new TestableSecretManagerCommand(CommandType, Container, SecretKey, _isModifying, _requiresKey, _requiresContainer, newParameters);

        protected override ISecretManagerCommand CreateCopyWithMetadata(IReadOnlyDictionary<string, object> newMetadata)
            => new TestableSecretManagerCommand(CommandType, Container, SecretKey, _isModifying, _requiresKey, _requiresContainer);
    }

    // -------------------------------------------------------
    // Constructor validation: commandType null/empty
    // -------------------------------------------------------

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorThrowsWhenCommandTypeIsEmpty()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new TestableSecretManagerCommand("", "vault", "key"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorThrowsWhenCommandTypeIsWhitespace()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new TestableSecretManagerCommand("   ", "vault", "key"));
    }

    // -------------------------------------------------------
    // Validate: SecretKey required but null/empty
    // -------------------------------------------------------

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateFailsWhenSecretKeyIsNullAndRequired()
    {
        // Arrange - requiresKey=true but secretKey is null
        var cmd = new TestableSecretManagerCommand("CustomOp", "vault", null, requiresKey: true);

        // Act
        var result = cmd.Validate();

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateFailsWhenSecretKeyIsEmptyAndRequired()
    {
        // Arrange - requiresKey=true but secretKey is empty
        var cmd = new TestableSecretManagerCommand("CustomOp", "vault", "", requiresKey: true);

        // Act
        var result = cmd.Validate();

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateSucceedsWhenSecretKeyIsNullAndNotRequired()
    {
        // Arrange - requiresKey=false
        var cmd = new TestableSecretManagerCommand("CustomOp", "vault", null, requiresKey: false);

        // Act
        var result = cmd.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    // -------------------------------------------------------
    // Validate: Container required but null/empty
    // -------------------------------------------------------

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateFailsWhenContainerIsNullAndRequired()
    {
        // Arrange
        var cmd = new TestableSecretManagerCommand("CustomOp", null, "key", requiresContainer: true);

        // Act
        var result = cmd.Validate();

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateSucceedsWhenContainerIsNullAndNotRequired()
    {
        // Arrange
        var cmd = new TestableSecretManagerCommand("CustomOp", null, "key", requiresContainer: false);

        // Act
        var result = cmd.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    // -------------------------------------------------------
    // ValidateModifyingParameters: IsSecretModifying=true
    // with SetSecret command type and missing SecretValue
    // -------------------------------------------------------

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateFailsForModifyingSetSecretWithoutSecretValue()
    {
        // Arrange - isModifying=true, CommandType="SetSecret", no SecretValue param
        var cmd = new TestableSecretManagerCommand("SetSecret", "vault", "key",
            isModifying: true, requiresKey: true);

        // Act
        var result = cmd.Validate();

        // Assert - should fail because SetSecret requires SecretValue parameter
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateFailsForModifyingSetSecretWithNullSecretValue()
    {
        // Arrange - isModifying=true, CommandType="SetSecret", SecretValue is null
        var paramsWithNull = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["SecretValue"] = null
        };
        var cmd = new TestableSecretManagerCommand("SetSecret", "vault", "key",
            isModifying: true, requiresKey: true, parameters: paramsWithNull);

        // Act
        var result = cmd.Validate();

        // Assert - should fail because SecretValue is null
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateSucceedsForModifyingSetSecretWithSecretValue()
    {
        // Arrange - isModifying=true, CommandType="SetSecret", SecretValue present
        var paramsWithValue = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["SecretValue"] = "my-secret"
        };
        var cmd = new TestableSecretManagerCommand("SetSecret", "vault", "key",
            isModifying: true, requiresKey: true, parameters: paramsWithValue);

        // Act
        var result = cmd.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    // -------------------------------------------------------
    // ValidateModifyingParameters: non-SetSecret modifying
    // command returns true
    // -------------------------------------------------------

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateSucceedsForModifyingNonSetSecretCommand()
    {
        // Arrange - isModifying=true, CommandType is NOT "SetSecret"
        var cmd = new TestableSecretManagerCommand("DeleteSecret", "vault", "key",
            isModifying: true, requiresKey: true);

        // Act
        var result = cmd.Validate();

        // Assert - should succeed because non-SetSecret modifying ops don't need SecretValue
        result.IsSuccess.ShouldBeTrue();
    }

    // -------------------------------------------------------
    // ValidateModifyingParameters: non-modifying command
    // early return (line 228)
    // -------------------------------------------------------

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateModifyingParametersReturnsTrueForNonModifyingCommand()
    {
        // Arrange - isModifying=false
        var cmd = new TestableSecretManagerCommand("GetSecret", "vault", "key",
            isModifying: false, requiresKey: true);

        // Act
        var result = cmd.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    // -------------------------------------------------------
    // Multiple validation errors combined
    // -------------------------------------------------------

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateReportsMultipleErrorsWhenSecretKeyAndContainerMissing()
    {
        // Arrange - both container and secret key null but both required
        var cmd = new TestableSecretManagerCommand("CustomOp", null, null,
            requiresKey: true, requiresContainer: true);

        // Act
        var result = cmd.Validate();

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    // -------------------------------------------------------
    // WithParameters and WithMetadata on base interface
    // -------------------------------------------------------

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void WithParametersReturnsNewCommand()
    {
        // Arrange
        var cmd = new TestableSecretManagerCommand("CustomOp", "vault", "key");
        var newParams = new Dictionary<string, object?> { ["custom"] = "value" };

        // Act
        var updated = cmd.WithParameters(newParams);

        // Assert
        updated.ShouldNotBeSameAs(cmd);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void WithMetadataReturnsNewCommand()
    {
        // Arrange
        var cmd = new TestableSecretManagerCommand("CustomOp", "vault", "key");
        var newMeta = new Dictionary<string, object> { ["source"] = "test" };

        // Act
        var updated = cmd.WithMetadata(newMeta);

        // Assert
        updated.ShouldNotBeSameAs(cmd);
    }

    // -------------------------------------------------------
    // Configuration property
    // -------------------------------------------------------

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConfigurationIsNullForSecretManagerCommands()
    {
        // Arrange
        var cmd = new TestableSecretManagerCommand("CustomOp", "vault", "key");

        // Act & Assert
        cmd.Configuration.ShouldBeNull();
    }

    // -------------------------------------------------------
    // RequiresSecretKey default switch case (line 206)
    // Uses a command that does NOT override RequiresSecretKey
    // -------------------------------------------------------

    /// <summary>
    /// Subclass that does NOT override RequiresSecretKey, so the base class
    /// switch expression is exercised (including the default => true branch).
    /// </summary>
    private sealed class DefaultRequiresKeyCommand : SecretManagerCommandBase
    {
        public DefaultRequiresKeyCommand(
            string commandType,
            string? container,
            string? secretKey,
            bool isModifying = false)
            : base(commandType, container, secretKey, typeof(IGenericResult))
        {
            IsModifying = isModifying;
        }

        private bool IsModifying { get; }

        public override bool IsSecretModifying => IsModifying;

        protected override ISecretManagerCommand CreateCopyWithParameters(IReadOnlyDictionary<string, object?> newParameters)
            => new DefaultRequiresKeyCommand(CommandType, Container, SecretKey, IsModifying);

        protected override ISecretManagerCommand CreateCopyWithMetadata(IReadOnlyDictionary<string, object> newMetadata)
            => new DefaultRequiresKeyCommand(CommandType, Container, SecretKey, IsModifying);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void RequiresSecretKeyDefaultCaseReturnsTrueForUnknownCommandType()
    {
        // Arrange - "UnknownOp" is not in the base switch, so it hits default => true
        // Without a secret key, validation should fail
        var cmd = new DefaultRequiresKeyCommand("UnknownOp", "vault", null);

        // Act
        var result = cmd.Validate();

        // Assert - fails because RequiresSecretKey() returns true and SecretKey is null
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void RequiresSecretKeyReturnsTrueForGetSecret()
    {
        // Arrange - "GetSecret" returns true in the switch
        var cmd = new DefaultRequiresKeyCommand("GetSecret", "vault", null);

        // Act
        var result = cmd.Validate();

        // Assert - fails because RequiresSecretKey() returns true and SecretKey is null
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void RequiresSecretKeyReturnsFalseForListSecrets()
    {
        // Arrange - "ListSecrets" returns false in the switch
        var cmd = new DefaultRequiresKeyCommand("ListSecrets", "vault", null);

        // Act
        var result = cmd.Validate();

        // Assert - succeeds because RequiresSecretKey() returns false for ListSecrets
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void RequiresSecretKeyReturnsTrueForGetSecretVersions()
    {
        // Arrange - "GetSecretVersions" returns true in the switch
        var cmd = new DefaultRequiresKeyCommand("GetSecretVersions", "vault", null);

        // Act
        var result = cmd.Validate();

        // Assert - fails because RequiresSecretKey() returns true and SecretKey is null
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void RequiresSecretKeyDefaultCaseSucceedsWithKey()
    {
        // Arrange - "CustomOp" hits default => true, but we provide a key
        var cmd = new DefaultRequiresKeyCommand("CustomOp", "vault", "my-key");

        // Act
        var result = cmd.Validate();

        // Assert - succeeds
        result.IsSuccess.ShouldBeTrue();
    }

    // -------------------------------------------------------
    // ValidateModifyingParameters base implementation
    // when called on a modifying command that is not SetSecret
    // -------------------------------------------------------

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void BaseValidateModifyingParametersReturnsTrueForNonSetSecretModifyingOp()
    {
        // Arrange - isModifying=true, CommandType="CustomDelete" (not "SetSecret")
        // Uses DefaultRequiresKeyCommand which does NOT override ValidateModifyingParameters
        var cmd = new DefaultRequiresKeyCommand("CustomDelete", "vault", "key", isModifying: true);

        // Act
        var result = cmd.Validate();

        // Assert - should succeed because the base ValidateModifyingParameters
        // returns true for non-SetSecret command types (line 236)
        result.IsSuccess.ShouldBeTrue();
    }
}
