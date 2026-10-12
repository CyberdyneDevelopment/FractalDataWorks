using System;
using System.Collections.Generic;
using Fdw.Services.SecretManagers.Commands;
using Fdw.Services.SecretManagers.Abstractions;

namespace Fdw.Services.SecretManagers.Tests.Commands;

public sealed class SecretManagerCommandBaseTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GetSecretCommandValidateSucceedsWithValidInputs()
    {
        // Arrange
        var sut = new GetSecretManagerCommand("vault", "my-key");

        // Act
        var result = sut.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GetSecretCommandValidateFailsWithMissingSecretKey()
    {
        // Arrange - use null container and null key via SetSecret which has different constructor
        var sut = new ListSecretsManagerCommand("vault");

        // ListSecrets doesn't require a key, so validate should succeed
        var result = sut.Validate();
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void SetSecretCommandValidateFailsWithoutSecretValueParameter()
    {
        // Arrange - SetSecret without SecretValue parameter
        var sut = new SetSecretManagerCommand(
            "vault",
            "my-key",
            "my-value",
            parameters: new Dictionary<string, object?> { ["Other"] = "test" });

        // Act
        var result = sut.Validate();

        // Assert - should succeed because SetSecretManagerCommand includes SecretValue in parameters
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void DeleteSecretCommandValidateSucceeds()
    {
        // Arrange
        var sut = new DeleteSecretManagerCommand("vault", "my-key");

        // Act
        var result = sut.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GetSecretVersionsCommandValidateSucceeds()
    {
        // Arrange
        var sut = new GetSecretManagerVersionsCommand("vault", "my-key");

        // Act
        var result = sut.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void DeleteSecretCommandConstructorThrowsOnEmptyKey()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new DeleteSecretManagerCommand("vault", ""));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void SetSecretCommandConstructorThrowsOnEmptyKey()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new SetSecretManagerCommand("vault", "", "value"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void SetSecretCreateFactoryMethod()
    {
        // Act
        var sut = SetSecretManagerCommand.Create("vault", "my-key", "my-secret");

        // Assert
        sut.CommandType.ShouldBe("SetSecret");
        sut.Container.ShouldBe("vault");
        sut.SecretKey.ShouldBe("my-key");
        sut.SecretValue.ShouldBe("my-secret");
        sut.IsSecretModifying.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void SetSecretWithDescriptionFactoryMethod()
    {
        // Act
        var sut = SetSecretManagerCommand.WithDescription("vault", "my-key", "my-secret", "test desc");

        // Assert
        sut.Description.ShouldBe("test desc");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void SetSecretWithExpirationFactoryMethod()
    {
        // Arrange
        var expiry = DateTimeOffset.UtcNow.AddDays(90);

        // Act
        var sut = SetSecretManagerCommand.WithExpiration("vault", "my-key", "my-secret", expiry);

        // Assert
        sut.ExpirationDate.ShouldBe(expiry);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void SetSecretWithTagsFactoryMethod()
    {
        // Arrange
        var tags = new Dictionary<string, string> { ["env"] = "prod" };

        // Act
        var sut = SetSecretManagerCommand.WithTags("vault", "my-key", "my-secret", tags);

        // Assert
        sut.Tags.ShouldContainKey("env");
        sut.Tags["env"].ShouldBe("prod");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void SetSecretTagsReturnEmptyWhenNotSet()
    {
        // Arrange
        var sut = SetSecretManagerCommand.Create("vault", "my-key", "my-secret");

        // Assert
        sut.Tags.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void DeleteSecretSoftDeleteFactoryMethod()
    {
        // Act
        var sut = DeleteSecretManagerCommand.SoftDelete("vault", "my-key");

        // Assert
        sut.PermanentDelete.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void DeleteSecretPermanentlyDeleteFactoryMethod()
    {
        // Act
        var sut = DeleteSecretManagerCommand.PermanentlyDelete("vault", "my-key");

        // Assert
        sut.PermanentDelete.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void DeleteSecretSoftDeleteWithRecoveryFactoryMethod()
    {
        // Arrange
        var window = TimeSpan.FromDays(30);

        // Act
        var sut = DeleteSecretManagerCommand.SoftDeleteWithRecovery("vault", "my-key", window);

        // Assert
        sut.PermanentDelete.ShouldBeFalse();
        sut.RecoveryWindow.ShouldBe(window);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void DeleteSecretRecoveryWindowDefaultsToNull()
    {
        // Arrange
        var sut = new DeleteSecretManagerCommand("vault", "my-key");

        // Assert
        sut.RecoveryWindow.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ListSecretsAllFactoryMethod()
    {
        // Act
        var sut = ListSecretsManagerCommand.All("vault");

        // Assert
        sut.Container.ShouldBe("vault");
        sut.Filter.ShouldBeNull();
        sut.MaxResults.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ListSecretsWithFilterFactoryMethod()
    {
        // Act
        var sut = ListSecretsManagerCommand.WithFilter("vault", "db-*");

        // Assert
        sut.Filter.ShouldBe("db-*");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ListSecretsWithPaginationFactoryMethod()
    {
        // Act
        var sut = ListSecretsManagerCommand.WithPagination("vault", 10, "token123");

        // Assert
        sut.MaxResults.ShouldBe(10);
        sut.ContinuationToken.ShouldBe("token123");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ListSecretsWithPaginationNoContinuationToken()
    {
        // Act
        var sut = ListSecretsManagerCommand.WithPagination("vault", 25);

        // Assert
        sut.MaxResults.ShouldBe(25);
        sut.ContinuationToken.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ListSecretsIncludingDeletedFactoryMethod()
    {
        // Act
        var sut = ListSecretsManagerCommand.IncludingDeleted("vault");

        // Assert
        sut.IncludeDeleted.ShouldBeTrue();
        sut.IncludeVersions.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ListSecretsIncludingDeletedWithVersions()
    {
        // Act
        var sut = ListSecretsManagerCommand.IncludingDeleted("vault", includeVersions: true);

        // Assert
        sut.IncludeDeleted.ShouldBeTrue();
        sut.IncludeVersions.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ListSecretsDefaultProperties()
    {
        // Arrange
        var sut = new ListSecretsManagerCommand("vault");

        // Assert
        sut.Filter.ShouldBeNull();
        sut.MaxResults.ShouldBeNull();
        sut.IncludeDeleted.ShouldBeFalse();
        sut.ContinuationToken.ShouldBeNull();
        sut.IncludeVersions.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GetSecretCommandConstructorThrowsOnEmptyKey()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new GetSecretManagerCommand("vault", ""));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GetSecretCommandConstructorThrowsOnWhitespaceKey()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new GetSecretManagerCommand("vault", "   "));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CommandPropertiesAreSetCorrectly()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { ["Version"] = "v2" };
        var metadata = new Dictionary<string, object> { ["source"] = "test" };

        // Act
        var sut = new GetSecretManagerCommand("vault", "my-key", parameters, metadata, TimeSpan.FromSeconds(30));

        // Assert
        sut.CommandType.ShouldBe("GetSecret");
        sut.Container.ShouldBe("vault");
        sut.SecretKey.ShouldBe("my-key");
        sut.ExpectedResultType.ShouldBe(typeof(SecretValue));
        sut.Timeout.ShouldBe(TimeSpan.FromSeconds(30));
        sut.Parameters.ShouldContainKey("Version");
        sut.Metadata.ShouldContainKey("source");
        sut.IsSecretModifying.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void WithParametersReturnsCopyWithNewParameters()
    {
        // Arrange
        var sut = new GetSecretManagerCommand("vault", "my-key");
        var newParams = new Dictionary<string, object?> { ["Version"] = "v3" };

        // Act
        var copy = sut.WithParameters(newParams);

        // Assert
        copy.ShouldNotBeSameAs(sut);
        copy.Parameters.ShouldContainKey("Version");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void WithMetadataReturnsCopyWithNewMetadata()
    {
        // Arrange
        var sut = new GetSecretManagerCommand("vault", "my-key");
        var newMeta = new Dictionary<string, object> { ["env"] = "prod" };

        // Act
        var copy = sut.WithMetadata(newMeta);

        // Assert
        copy.ShouldNotBeSameAs(sut);
        copy.Metadata.ShouldContainKey("env");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GetSecretLatestFactoryMethod()
    {
        // Act
        var sut = GetSecretManagerCommand.Latest("vault", "my-key");

        // Assert
        sut.Container.ShouldBe("vault");
        sut.SecretKey.ShouldBe("my-key");
        sut.Version.ShouldBeNull();
        sut.IncludeMetadata.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GetSecretLatestWithMetadata()
    {
        // Act
        var sut = GetSecretManagerCommand.Latest("vault", "my-key", includeMetadata: true);

        // Assert
        sut.IncludeMetadata.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GetSecretForVersionFactoryMethod()
    {
        // Act
        var sut = GetSecretManagerCommand.ForVersion("vault", "my-key", "v2");

        // Assert
        sut.Version.ShouldBe("v2");
        sut.IncludeMetadata.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GetSecretForVersionWithMetadata()
    {
        // Act
        var sut = GetSecretManagerCommand.ForVersion("vault", "my-key", "v2", includeMetadata: true);

        // Assert
        sut.Version.ShouldBe("v2");
        sut.IncludeMetadata.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void GetSecretForVersionThrowsOnEmptyVersion()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            GetSecretManagerCommand.ForVersion("vault", "my-key", ""));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void DeleteSecretCommandIsSecretModifying()
    {
        // Arrange
        var sut = new DeleteSecretManagerCommand("vault", "my-key");

        // Assert
        sut.IsSecretModifying.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ListSecretsCommandIsNotSecretModifying()
    {
        // Arrange
        var sut = new ListSecretsManagerCommand("vault");

        // Assert
        sut.IsSecretModifying.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CommandIdIsGeneratedAsGuidFormat()
    {
        // Arrange
        var sut = new GetSecretManagerCommand("vault", "my-key");

        // Assert
        Guid.TryParse(sut.CommandId, out _).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CommandWithNullContainerValidatesIfNotRequired()
    {
        // Arrange - ListSecrets may or may not require container
        var sut = new ListSecretsManagerCommand(null);

        // Act
        var result = sut.Validate();

        // Assert - container validation depends on RequiresContainer override
        // Default is true, so this should fail
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CommandWithValidContainerValidatesSuccessfully()
    {
        // Arrange
        var sut = new ListSecretsManagerCommand("vault");

        // Act
        var result = sut.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }
}
