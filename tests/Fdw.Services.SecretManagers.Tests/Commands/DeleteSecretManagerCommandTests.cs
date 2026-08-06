using System;
using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using Shouldly;
using Xunit;

namespace Fdw.Services.SecretManagers.Tests.Commands;

public class DeleteSecretManagerCommandTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorSetsProperties()
    {
        var cmd = new DeleteSecretManagerCommand("vault", "mySecret");

        cmd.Container.ShouldBe("vault");
        cmd.SecretKey.ShouldBe("mySecret");
        cmd.CommandType.ShouldBe("DeleteSecret");
        cmd.IsSecretModifying.ShouldBeTrue();
        cmd.ExpectedResultType.ShouldBe(typeof(IGenericResult));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorWithEmptySecretKeyThrows()
    {
        Should.Throw<ArgumentException>(() => new DeleteSecretManagerCommand("vault", ""));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void SoftDeleteCreatesSoftDeleteCommand()
    {
        var cmd = DeleteSecretManagerCommand.SoftDelete("vault", "key");

        cmd.PermanentDelete.ShouldBeFalse();
        cmd.SecretKey.ShouldBe("key");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void PermanentlyDeleteCreatesPermanentDeleteCommand()
    {
        var cmd = DeleteSecretManagerCommand.PermanentlyDelete("vault", "key");

        cmd.PermanentDelete.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void SoftDeleteWithRecoverySetsRecoveryWindow()
    {
        var window = TimeSpan.FromDays(7);
        var cmd = DeleteSecretManagerCommand.SoftDeleteWithRecovery("vault", "key", window);

        cmd.PermanentDelete.ShouldBeFalse();
        cmd.RecoveryWindow.ShouldBe(window);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void DefaultRecoveryWindowIsNull()
    {
        var cmd = DeleteSecretManagerCommand.SoftDelete("vault", "key");

        cmd.RecoveryWindow.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void WithParametersCreatesNewCommand()
    {
        var cmd = DeleteSecretManagerCommand.SoftDelete("vault", "key");
        var newParams = new Dictionary<string, object?> { ["custom"] = "value" };

        var updated = cmd.WithParameters(newParams);

        updated.ShouldNotBeSameAs(cmd);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void WithMetadataCreatesNewCommand()
    {
        var cmd = DeleteSecretManagerCommand.SoftDelete("vault", "key");
        var newMeta = new Dictionary<string, object> { ["source"] = "test" };

        var updated = cmd.WithMetadata(newMeta);

        updated.ShouldNotBeSameAs(cmd);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateSucceedsForValidCommand()
    {
        var cmd = new DeleteSecretManagerCommand("vault", "key");

        var result = cmd.Validate();

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void TypedWithParametersCreatesNewCommand()
    {
        ISecretManagerCommand<IGenericResult> cmd = DeleteSecretManagerCommand.SoftDelete("vault", "key");
        var newParams = new Dictionary<string, object?> { ["custom"] = "value" };

        var updated = cmd.WithParameters(newParams);

        updated.ShouldNotBeSameAs(cmd);
        updated.ShouldBeOfType<DeleteSecretManagerCommand>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void TypedWithMetadataCreatesNewCommand()
    {
        ISecretManagerCommand<IGenericResult> cmd = DeleteSecretManagerCommand.SoftDelete("vault", "key");
        var newMeta = new Dictionary<string, object> { ["source"] = "test" };

        var updated = cmd.WithMetadata(newMeta);

        updated.ShouldNotBeSameAs(cmd);
        updated.ShouldBeOfType<DeleteSecretManagerCommand>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CorrelationIdIsSet()
    {
        var cmd = new DeleteSecretManagerCommand("vault", "key");

        cmd.CorrelationId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CommandIdIsNotEmpty()
    {
        var cmd = new DeleteSecretManagerCommand("vault", "key");

        cmd.CommandId.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void MetadataIsEmptyByDefault()
    {
        var cmd = new DeleteSecretManagerCommand("vault", "key");

        cmd.Metadata.ShouldNotBeNull();
        cmd.Metadata.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void PermanentDeleteReturnsFalseWhenParameterIsNotBool()
    {
        var parameters = new Dictionary<string, object?> { ["PermanentDelete"] = "notABool" };
        var cmd = new DeleteSecretManagerCommand("vault", "key", parameters);

        cmd.PermanentDelete.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void RecoveryWindowReturnsNullWhenParameterIsNotTimeSpan()
    {
        var parameters = new Dictionary<string, object?> { ["RecoveryWindow"] = "notATimeSpan" };
        var cmd = new DeleteSecretManagerCommand("vault", "key", parameters);

        cmd.RecoveryWindow.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorWithWhitespaceSecretKeyThrows()
    {
        Should.Throw<ArgumentException>(() => new DeleteSecretManagerCommand("vault", "   "));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void SoftDeleteWithTimeoutSetsTimeout()
    {
        var timeout = TimeSpan.FromSeconds(30);
        var cmd = DeleteSecretManagerCommand.SoftDelete("vault", "key", timeout);

        cmd.Timeout.ShouldBe(timeout);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void PermanentlyDeleteWithTimeoutSetsTimeout()
    {
        var timeout = TimeSpan.FromSeconds(15);
        var cmd = DeleteSecretManagerCommand.PermanentlyDelete("vault", "key", timeout);

        cmd.Timeout.ShouldBe(timeout);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateFailsWhenContainerIsNullAndRequired()
    {
        var cmd = new DeleteSecretManagerCommand(null, "key");

        var result = cmd.Validate();

        result.IsSuccess.ShouldBeFalse();
    }
}
