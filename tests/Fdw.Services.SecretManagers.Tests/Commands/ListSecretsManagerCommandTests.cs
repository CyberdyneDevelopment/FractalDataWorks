using System;
using System.Collections.Generic;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using Shouldly;
using Xunit;

namespace Fdw.Services.SecretManagers.Tests.Commands;

public class ListSecretsManagerCommandTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ConstructorSetsProperties()
    {
        var cmd = new ListSecretsManagerCommand("vault");

        cmd.Container.ShouldBe("vault");
        cmd.SecretKey.ShouldBeNull();
        cmd.CommandType.ShouldBe("ListSecrets");
        cmd.IsSecretModifying.ShouldBeFalse();
        cmd.ExpectedResultType.ShouldBe(typeof(IReadOnlyList<ISecretMetadata>));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AllCreatesCommandWithoutFilter()
    {
        var cmd = ListSecretsManagerCommand.All("vault");

        cmd.Filter.ShouldBeNull();
        cmd.MaxResults.ShouldBeNull();
        cmd.IncludeDeleted.ShouldBeFalse();
        cmd.ContinuationToken.ShouldBeNull();
        cmd.IncludeVersions.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void WithFilterSetsFilter()
    {
        var cmd = ListSecretsManagerCommand.WithFilter("vault", "prod-*");

        cmd.Filter.ShouldBe("prod-*");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void WithPaginationSetsMaxResults()
    {
        var cmd = ListSecretsManagerCommand.WithPagination("vault", 25);

        cmd.MaxResults.ShouldBe(25);
        cmd.ContinuationToken.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void WithPaginationSetsContinuationToken()
    {
        var cmd = ListSecretsManagerCommand.WithPagination("vault", 25, "token123");

        cmd.MaxResults.ShouldBe(25);
        cmd.ContinuationToken.ShouldBe("token123");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void IncludingDeletedSetsFlag()
    {
        var cmd = ListSecretsManagerCommand.IncludingDeleted("vault");

        cmd.IncludeDeleted.ShouldBeTrue();
        cmd.IncludeVersions.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void IncludingDeletedWithVersionsSetsFlags()
    {
        var cmd = ListSecretsManagerCommand.IncludingDeleted("vault", includeVersions: true);

        cmd.IncludeDeleted.ShouldBeTrue();
        cmd.IncludeVersions.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateSucceedsWithoutSecretKey()
    {
        var cmd = ListSecretsManagerCommand.All("vault");

        var result = cmd.Validate();

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ContainerCanBeNull()
    {
        var cmd = ListSecretsManagerCommand.All(null);

        cmd.Container.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void WithParametersCreatesNewCommand()
    {
        var cmd = ListSecretsManagerCommand.All("vault");
        var newParams = new Dictionary<string, object?> { ["Filter"] = "test-*" };

        var updated = cmd.WithParameters(newParams);

        updated.ShouldNotBeSameAs(cmd);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void WithMetadataCreatesNewCommand()
    {
        var cmd = ListSecretsManagerCommand.All("vault");
        var newMeta = new Dictionary<string, object> { ["source"] = "test" };

        var updated = cmd.WithMetadata(newMeta);

        updated.ShouldNotBeSameAs(cmd);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void TimeoutIsPassedThrough()
    {
        var timeout = TimeSpan.FromSeconds(60);
        var cmd = ListSecretsManagerCommand.All("vault", timeout);

        cmd.Timeout.ShouldBe(timeout);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void TypedWithParametersCreatesNewCommand()
    {
        ISecretManagerCommand<IReadOnlyList<ISecretMetadata>> cmd = ListSecretsManagerCommand.All("vault");
        var newParams = new Dictionary<string, object?> { ["Filter"] = "test-*" };

        var updated = cmd.WithParameters(newParams);

        updated.ShouldNotBeSameAs(cmd);
        updated.ShouldBeOfType<ListSecretsManagerCommand>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void TypedWithMetadataCreatesNewCommand()
    {
        ISecretManagerCommand<IReadOnlyList<ISecretMetadata>> cmd = ListSecretsManagerCommand.All("vault");
        var newMeta = new Dictionary<string, object> { ["source"] = "test" };

        var updated = cmd.WithMetadata(newMeta);

        updated.ShouldNotBeSameAs(cmd);
        updated.ShouldBeOfType<ListSecretsManagerCommand>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CorrelationIdIsSet()
    {
        var cmd = ListSecretsManagerCommand.All("vault");

        cmd.CorrelationId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CommandIdIsNotEmpty()
    {
        var cmd = ListSecretsManagerCommand.All("vault");

        cmd.CommandId.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void MetadataIsEmptyByDefault()
    {
        var cmd = ListSecretsManagerCommand.All("vault");

        cmd.Metadata.ShouldNotBeNull();
        cmd.Metadata.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void MaxResultsReturnsNullWhenParameterIsNotInt()
    {
        var parameters = new Dictionary<string, object?> { ["MaxResults"] = "notAnInt" };
        var cmd = new ListSecretsManagerCommand("vault", parameters);

        cmd.MaxResults.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void IncludeDeletedReturnsFalseWhenParameterIsNotBool()
    {
        var parameters = new Dictionary<string, object?> { ["IncludeDeleted"] = "notABool" };
        var cmd = new ListSecretsManagerCommand("vault", parameters);

        cmd.IncludeDeleted.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void IncludeVersionsReturnsFalseWhenParameterIsNotBool()
    {
        var parameters = new Dictionary<string, object?> { ["IncludeVersions"] = "notABool" };
        var cmd = new ListSecretsManagerCommand("vault", parameters);

        cmd.IncludeVersions.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void FilterReturnsNullWhenNotSet()
    {
        var cmd = ListSecretsManagerCommand.All("vault");

        cmd.Filter.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ValidateFailsWhenContainerIsNullAndRequired()
    {
        var cmd = ListSecretsManagerCommand.All(null);

        var result = cmd.Validate();

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void SecretKeyIsNullForListCommand()
    {
        var cmd = ListSecretsManagerCommand.All("vault");

        cmd.SecretKey.ShouldBeNull();
    }
}
