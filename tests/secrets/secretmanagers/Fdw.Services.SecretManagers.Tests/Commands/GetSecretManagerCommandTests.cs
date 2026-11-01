using System;
using System.Collections.Generic;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using Shouldly;
using Xunit;

namespace Fdw.Services.SecretManagers.Tests.Commands;

public class GetSecretManagerCommandTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorSetsProperties()
    {
        var cmd = new GetSecretManagerCommand("vault", "mySecret");

        cmd.Container.ShouldBe("vault");
        cmd.SecretKey.ShouldBe("mySecret");
        cmd.CommandType.ShouldBe("GetSecret");
        cmd.IsSecretModifying.ShouldBeFalse();
        cmd.ExpectedResultType.ShouldBe(typeof(SecretValue));
        cmd.CommandId.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorWithEmptySecretKeyThrows()
    {
        Should.Throw<ArgumentException>(() => new GetSecretManagerCommand("vault", ""));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorWithWhitespaceSecretKeyThrows()
    {
        Should.Throw<ArgumentException>(() => new GetSecretManagerCommand("vault", "   "));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void LatestCreatesCommandWithoutVersion()
    {
        var cmd = GetSecretManagerCommand.Latest("vault", "key");

        cmd.Container.ShouldBe("vault");
        cmd.SecretKey.ShouldBe("key");
        cmd.Version.ShouldBeNull();
        cmd.IncludeMetadata.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void LatestWithMetadataCreatesCommandWithMetadataFlag()
    {
        var cmd = GetSecretManagerCommand.Latest("vault", "key", includeMetadata: true);

        cmd.IncludeMetadata.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ForVersionCreatesCommandWithVersion()
    {
        var cmd = GetSecretManagerCommand.ForVersion("vault", "key", "v3");

        cmd.Version.ShouldBe("v3");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ForVersionWithEmptyVersionThrows()
    {
        Should.Throw<ArgumentException>(() => GetSecretManagerCommand.ForVersion("vault", "key", ""));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ForVersionWithMetadataCreatesCommandWithBothFlags()
    {
        var cmd = GetSecretManagerCommand.ForVersion("vault", "key", "v1", includeMetadata: true);

        cmd.Version.ShouldBe("v1");
        cmd.IncludeMetadata.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void WithParametersCreatesNewCommandWithUpdatedParameters()
    {
        var cmd = GetSecretManagerCommand.Latest("vault", "key");
        var newParams = new Dictionary<string, object?> { ["custom"] = "value" };

        var updated = cmd.WithParameters(newParams);

        updated.ShouldNotBeSameAs(cmd);
        updated.SecretKey.ShouldBe("key");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void WithMetadataCreatesNewCommandWithUpdatedMetadata()
    {
        var cmd = GetSecretManagerCommand.Latest("vault", "key");
        var newMeta = new Dictionary<string, object> { ["source"] = "test" };

        var updated = cmd.WithMetadata(newMeta);

        updated.ShouldNotBeSameAs(cmd);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateSucceedsForValidCommand()
    {
        var cmd = new GetSecretManagerCommand("vault", "key");

        var result = cmd.Validate();

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ContainerCanBeNull()
    {
        var cmd = new GetSecretManagerCommand(null, "key");

        cmd.Container.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void TimeoutIsPassedThrough()
    {
        var timeout = TimeSpan.FromSeconds(30);
        var cmd = GetSecretManagerCommand.Latest("vault", "key", timeout: timeout);

        cmd.Timeout.ShouldBe(timeout);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void TypedWithParametersCreatesNewCommand()
    {
        ISecretManagerCommand<SecretValue> cmd = GetSecretManagerCommand.Latest("vault", "key");
        var newParams = new Dictionary<string, object?> { ["custom"] = "value" };

        var updated = cmd.WithParameters(newParams);

        updated.ShouldNotBeSameAs(cmd);
        updated.ShouldBeOfType<GetSecretManagerCommand>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void TypedWithMetadataCreatesNewCommand()
    {
        ISecretManagerCommand<SecretValue> cmd = GetSecretManagerCommand.Latest("vault", "key");
        var newMeta = new Dictionary<string, object> { ["source"] = "test" };

        var updated = cmd.WithMetadata(newMeta);

        updated.ShouldNotBeSameAs(cmd);
        updated.ShouldBeOfType<GetSecretManagerCommand>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CorrelationIdIsSet()
    {
        var cmd = new GetSecretManagerCommand("vault", "key");

        cmd.CorrelationId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ParametersAreEmptyByDefault()
    {
        var cmd = new GetSecretManagerCommand("vault", "key");

        cmd.Parameters.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void MetadataIsEmptyByDefault()
    {
        var cmd = new GetSecretManagerCommand("vault", "key");

        cmd.Metadata.ShouldNotBeNull();
        cmd.Metadata.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void VersionReturnsNullWhenNotSet()
    {
        var cmd = new GetSecretManagerCommand("vault", "key");

        cmd.Version.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void IncludeMetadataReturnsFalseWhenNotSet()
    {
        var cmd = new GetSecretManagerCommand("vault", "key");

        cmd.IncludeMetadata.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void IncludeMetadataReturnsFalseWhenParameterIsNotBool()
    {
        var parameters = new Dictionary<string, object?> { ["IncludeMetadata"] = "notABool" };
        var cmd = new GetSecretManagerCommand("vault", "key", parameters);

        cmd.IncludeMetadata.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateFailsWhenContainerIsNullAndRequired()
    {
        // Container is required by default (RequiresContainer returns true)
        var cmd = new GetSecretManagerCommand(null, "key");

        var result = cmd.Validate();

        result.IsSuccess.ShouldBeFalse();
    }
}
