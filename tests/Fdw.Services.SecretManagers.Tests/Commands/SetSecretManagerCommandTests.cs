using System;
using System.Collections.Generic;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using Shouldly;
using Xunit;

namespace Fdw.Services.SecretManagers.Tests.Commands;

public class SetSecretManagerCommandTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorSetsProperties()
    {
        var cmd = new SetSecretManagerCommand("vault", "mySecret", "secretValue");

        cmd.Container.ShouldBe("vault");
        cmd.SecretKey.ShouldBe("mySecret");
        cmd.SecretValue.ShouldBe("secretValue");
        cmd.CommandType.ShouldBe("SetSecret");
        cmd.IsSecretModifying.ShouldBeTrue();
        cmd.ExpectedResultType.ShouldBe(typeof(SecretValue));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorWithEmptySecretKeyThrows()
    {
        Should.Throw<ArgumentException>(() => new SetSecretManagerCommand("vault", "", "value"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CreateFactoryMethodBuildsSimpleCommand()
    {
        var cmd = SetSecretManagerCommand.Create("vault", "key", "val");

        cmd.SecretKey.ShouldBe("key");
        cmd.SecretValue.ShouldBe("val");
        cmd.Description.ShouldBeNull();
        cmd.ExpirationDate.ShouldBeNull();
        cmd.Tags.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void WithDescriptionSetsDescription()
    {
        var cmd = SetSecretManagerCommand.WithDescription("vault", "key", "val", "A test secret");

        cmd.Description.ShouldBe("A test secret");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void WithExpirationSetsExpiration()
    {
        var expiry = DateTimeOffset.UtcNow.AddDays(90);
        var cmd = SetSecretManagerCommand.WithExpiration("vault", "key", "val", expiry);

        cmd.ExpirationDate.ShouldBe(expiry);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void WithTagsSetsTags()
    {
        var tags = new Dictionary<string, string> { ["env"] = "prod", ["team"] = "data" };
        var cmd = SetSecretManagerCommand.WithTags("vault", "key", "val", tags);

        cmd.Tags.ShouldContainKey("env");
        cmd.Tags["env"].ShouldBe("prod");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void WithParametersCreatesNewCommand()
    {
        var cmd = SetSecretManagerCommand.Create("vault", "key", "val");
        var newParams = new Dictionary<string, object?> { ["custom"] = "x" };

        var updated = cmd.WithParameters(newParams);

        updated.ShouldNotBeSameAs(cmd);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void WithMetadataCreatesNewCommand()
    {
        var cmd = SetSecretManagerCommand.Create("vault", "key", "val");
        var newMeta = new Dictionary<string, object> { ["source"] = "test" };

        var updated = cmd.WithMetadata(newMeta);

        updated.ShouldNotBeSameAs(cmd);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateSucceedsWithSecretValueInParameters()
    {
        var cmd = SetSecretManagerCommand.Create("vault", "key", "val");

        var result = cmd.Validate();

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ParametersContainSecretValueKey()
    {
        var cmd = SetSecretManagerCommand.Create("vault", "key", "val");

        cmd.Parameters.ShouldContainKey("SecretValue");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AdditionalParametersAreMergedWithSecretValue()
    {
        var additional = new Dictionary<string, object?> { ["Description"] = "desc" };
        var cmd = new SetSecretManagerCommand("vault", "key", "val", additional);

        cmd.Parameters.ShouldContainKey("SecretValue");
        cmd.Parameters.ShouldContainKey("Description");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void TypedWithParametersCreatesNewCommand()
    {
        ISecretManagerCommand<SecretValue> cmd = SetSecretManagerCommand.Create("vault", "key", "val");
        var newParams = new Dictionary<string, object?> { ["custom"] = "x" };

        var updated = cmd.WithParameters(newParams);

        updated.ShouldNotBeSameAs(cmd);
        updated.ShouldBeOfType<SetSecretManagerCommand>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void TypedWithMetadataCreatesNewCommand()
    {
        ISecretManagerCommand<SecretValue> cmd = SetSecretManagerCommand.Create("vault", "key", "val");
        var newMeta = new Dictionary<string, object> { ["source"] = "test" };

        var updated = cmd.WithMetadata(newMeta);

        updated.ShouldNotBeSameAs(cmd);
        updated.ShouldBeOfType<SetSecretManagerCommand>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void TagsReturnsEmptyDictionaryWhenNotSet()
    {
        var cmd = SetSecretManagerCommand.Create("vault", "key", "val");

        cmd.Tags.ShouldNotBeNull();
        cmd.Tags.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void TagsReturnsEmptyWhenParameterIsNotDictionary()
    {
        var parameters = new Dictionary<string, object?> { ["Tags"] = "notADictionary" };
        var cmd = new SetSecretManagerCommand("vault", "key", "val", parameters);

        cmd.Tags.ShouldNotBeNull();
        cmd.Tags.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ExpirationDateReturnsNullWhenNotDateTimeOffset()
    {
        var parameters = new Dictionary<string, object?> { ["ExpirationDate"] = "notADate" };
        var cmd = new SetSecretManagerCommand("vault", "key", "val", parameters);

        cmd.ExpirationDate.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void DescriptionReturnsNullWhenNotSet()
    {
        var cmd = SetSecretManagerCommand.Create("vault", "key", "val");

        cmd.Description.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CreateWithTimeoutSetsTimeout()
    {
        var timeout = TimeSpan.FromSeconds(15);
        var cmd = SetSecretManagerCommand.Create("vault", "key", "val", timeout);

        cmd.Timeout.ShouldBe(timeout);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateFailsWhenContainerIsNullAndRequired()
    {
        var cmd = SetSecretManagerCommand.Create(null, "key", "val");

        var result = cmd.Validate();

        result.IsSuccess.ShouldBeFalse();
    }
}
